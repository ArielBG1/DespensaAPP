using DespensaApp.Models.ViewModels;
using DespensaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DespensaApp.Controllers;

[Authorize]
public class ProductosController(IProductoService service) : Controller
{
    // ── LISTADO ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ProductoFiltroViewModel filtro)
    {
        var vm = await service.GetListadoAsync(filtro);
        return View(vm);
    }

    // ── CREAR ────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        var vm = await service.GetFormViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductoFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await service.PopulateDropdownsAsync(vm);
            return View(vm);
        }

        var (creado, error) = await service.CreateAsync(vm, GetUserId());

        if (!creado)
        {
            ModelState.AddModelError(string.Empty, error!);
            await service.PopulateDropdownsAsync(vm);
            return View(vm);
        }

        TempData["Success"] = "Producto creado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // ── EDITAR ───────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await service.GetFormViewModelAsync(id);
        if (vm.Id == 0) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductoFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await service.PopulateDropdownsAsync(vm);
            return View(vm);
        }

        var (actualizado, error) = await service.UpdateAsync(vm, GetUserId());

        if (!actualizado)
        {
            ModelState.AddModelError(string.Empty, error!);
            await service.PopulateDropdownsAsync(vm);
            return View(vm);
        }

        TempData["Success"] = "Producto actualizado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // ── DETALLE ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var producto = await service.GetByIdAsync(id);
        return producto is null ? NotFound() : View(producto);
    }

    // ── ELIMINAR (lógico) ────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteLogicalAsync(id);
        TempData["Success"] = "Producto desactivado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ── AJUSTE DE STOCK ──────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AjustarStock(AjusteStockViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        var (ajustado, error) = await service.AjustarStockAsync(vm, GetUserId());

        if (!ajustado)
            TempData["Error"] = error;
        else
            TempData["Success"] = "Stock ajustado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // ── HISTORIAL DE MOVIMIENTOS ─────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Movimientos(int id,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        var vm = await service.GetMovimientosAsync(id, desde, hasta);
        return vm is null ? NotFound() : View(vm);
    }

    // ── AJAX: verificar código de barra (usado por Crear y Editar) ─
    [HttpGet]
    public async Task<IActionResult> CheckCodigoBarra(string codigo, int idActual = 0)
    {
        var resultado = await service.CheckCodigoBarraAsync(codigo, idActual);
        return Json(resultado);
    }

    // ── IMPRESIÓN DE ETIQUETAS EN LOTE ────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImprimirEtiquetas([FromQuery] int[] ids)
    {
        if (ids is null || ids.Length == 0) return RedirectToAction(nameof(Index));
        var vm = await service.GetProductosParaEtiquetasAsync(ids);
        return View(vm);
    }

    // ── AJAX: generar código de barras interno (producto sin código) ─
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerarCodigoBarra()
    {
        try
        {
            var codigo = await service.GenerarCodigoBarraInternoAsync();
            return Json(new { ok = true, codigo });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }

    // ── AJAX: creación rápida de categoría (usado por Crear y Editar) ─
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCategoriaRapida([FromBody] CrearCategoriaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Json(new { ok = false, error = "El nombre es obligatorio." });

        var ctx = HttpContext.RequestServices
            .GetRequiredService<DespensaApp.Data.DespensaDbContext>();

        var categoria = new DespensaApp.Models.Entities.Categoria
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            Activo = true,
            CreatedAt = DateTime.UtcNow
        };

        ctx.Categorias.Add(categoria);
        await ctx.SaveChangesAsync();
        return Json(new { ok = true, id = categoria.Id, nombre = categoria.Nombre });
    }

    // ────────────────────────────────────────────────────────────
    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public record CrearCategoriaDto(string Nombre, string? Descripcion);
