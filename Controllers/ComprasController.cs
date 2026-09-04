using System.Security.Claims;
using DespensaApp.Data;
using DespensaApp.Models.Entities;
using DespensaApp.Models.ViewModels;
using DespensaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DespensaApp.Controllers;

[Authorize(Roles = "Admin")]
public class ComprasController(DespensaDbContext db, IConfiguracionService configService) : Controller
{
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── Historial ────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index([FromQuery] CompraFiltroViewModel filtro)
    {
        filtro.Pagina = Math.Max(1, filtro.Pagina);
        const int pageSize = 20;

        var q = db.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Usuario)
            .Include(c => c.Detalles)
            .AsQueryable();

        if (filtro.FechaDesde.HasValue)
            q = q.Where(c => c.FechaCompra >= filtro.FechaDesde.Value.ToUniversalTime());
        if (filtro.FechaHasta.HasValue)
            q = q.Where(c => c.FechaCompra < filtro.FechaHasta.Value.AddDays(1).ToUniversalTime());
        if (filtro.ProveedorId.HasValue)
            q = q.Where(c => c.ProveedorId == filtro.ProveedorId);

        var total = await q.CountAsync();
        var totalPeriodo = await q.SumAsync(c => (decimal?)c.Total) ?? 0m;

        var items = await q
            .OrderByDescending(c => c.FechaCompra)
            .Skip((filtro.Pagina - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CompraListItemViewModel
            {
                Id = c.Id,
                NumeroRemito = c.NumeroRemito,
                Proveedor = c.Proveedor!.Nombre,
                Responsable = c.Usuario!.Nombre,
                CantidadItems = c.Detalles.Count,
                Total = c.Total,
                Estado = c.Estado,
                FechaCompra = c.FechaCompra
            })
            .ToListAsync();

        var proveedores = await db.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new SelectListItem(p.Nombre, p.Id.ToString()))
            .ToListAsync();

        var vm = new CompraIndexViewModel
        {
            Compras = items,
            TotalItems = total,
            TotalPaginas = (int)Math.Ceiling((double)total / pageSize),
            Filtro = filtro,
            TotalPeriodo = totalPeriodo,
            MonedaSimbolo = await configService.ObtenerValorAsync("MonedaSimbolo", "Gs."),
            Proveedores = proveedores
        };

        ViewData["ActiveMenu"] = "Compras";
        return View(vm);
    }

    // ─── Nueva compra ─────────────────────────────────────────────────────────

    public async Task<IActionResult> Nueva()
    {
        ViewData["Proveedores"] = await db.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new SelectListItem(p.Nombre, p.Id.ToString()))
            .ToListAsync();

        ViewData["MonedaSimbolo"] = await configService.ObtenerValorAsync("MonedaSimbolo", "Gs.");
        ViewData["ActiveMenu"] = "Compras";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Procesar([FromBody] CompraFormDto dto)
    {
        if (dto.ProveedorId <= 0)
            return BadRequest(new { error = "Seleccione un proveedor." });
        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest(new { error = "Agregue al menos un producto." });

        var ids = dto.Items.Select(i => i.ProductoId).Distinct().ToList();
        var productos = await db.Productos
            .Where(p => ids.Contains(p.Id) && p.Activo)
            .ToDictionaryAsync(p => p.Id);

        foreach (var item in dto.Items)
        {
            if (!productos.ContainsKey(item.ProductoId))
                return BadRequest(new { error = $"Producto ID {item.ProductoId} no encontrado." });
            if (item.Cantidad <= 0)
                return BadRequest(new { error = "Las cantidades deben ser mayores a 0." });
            if (item.PrecioUnitario < 0)
                return BadRequest(new { error = "El precio no puede ser negativo." });
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var compra = new Compra
            {
                ProveedorId = dto.ProveedorId,
                UsuarioId = GetUserId(),
                NumeroRemito = string.IsNullOrWhiteSpace(dto.NumeroRemito) ? null : dto.NumeroRemito.Trim(),
                Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim(),
                Estado = EstadoCompra.Recibida,
                FechaCompra = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            decimal totalCompra = 0;
            foreach (var item in dto.Items)
            {
                var subtotal = Math.Round(item.Cantidad * item.PrecioUnitario, 2);
                totalCompra += subtotal;

                compra.Detalles.Add(new DetalleCompra
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = subtotal
                });

                var (_, nuevoStock) = await StockOperations.AplicarDeltaAsync(db, item.ProductoId, item.Cantidad);

                db.MovimientosStock.Add(new MovimientoStock
                {
                    ProductoId = item.ProductoId,
                    UsuarioId = GetUserId(),
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = item.Cantidad,
                    StockAntes = nuevoStock - item.Cantidad,
                    StockDespues = nuevoStock,
                    Motivo = $"Compra #{compra.NumeroRemito ?? "s/n"}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            compra.Total = totalCompra;
            db.Compras.Add(compra);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { compraId = compra.Id });
        }
        catch
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { error = "Error al procesar la compra. Intente nuevamente." });
        }
    }

    // ─── Detalle ──────────────────────────────────────────────────────────────

    public async Task<IActionResult> Detail(int id)
    {
        var compra = await db.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Usuario)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra is null) return NotFound();

        var vm = new CompraDetalleViewModel
        {
            Compra = compra,
            MonedaSimbolo = await configService.ObtenerValorAsync("MonedaSimbolo", "Gs."),
            NombreNegocio = await configService.ObtenerValorAsync("NombreNegocio", "Despensa App")
        };

        ViewData["ActiveMenu"] = "Compras";
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(int id)
    {
        var compra = await db.Compras
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra is null) return NotFound();
        if (compra.Estado == EstadoCompra.Anulada)
        {
            TempData["Error"] = "La compra ya está anulada.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            compra.Estado = EstadoCompra.Anulada;

            foreach (var det in compra.Detalles)
            {
                var (resultado, nuevoStock) = await StockOperations.AplicarDeltaAsync(
                    db, det.ProductoId, -det.Cantidad);

                if (resultado == StockOperations.ResultadoStock.NoEncontrado) continue;
                if (resultado == StockOperations.ResultadoStock.Insuficiente)
                    throw new InvalidOperationException(
                        $"No se puede anular: el stock del producto (id {det.ProductoId}) ya fue " +
                        "consumido por ventas u otros movimientos posteriores.");

                db.MovimientosStock.Add(new MovimientoStock
                {
                    ProductoId = det.ProductoId,
                    UsuarioId = GetUserId(),
                    Tipo = TipoMovimiento.AjusteNegativo,
                    Cantidad = det.Cantidad,
                    StockAntes = nuevoStock + det.Cantidad,
                    StockDespues = nuevoStock,
                    Motivo = $"Anulación compra #{compra.NumeroRemito ?? compra.Id.ToString()}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["Success"] = "Compra anulada. Stock restituido.";
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["Error"] = ex is InvalidOperationException ? ex.Message : "Error al anular la compra.";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ─── AJAX: búsqueda de productos ─────────────────────────────────────────

    public async Task<IActionResult> BuscarProducto(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return NotFound();

        var p = await db.Productos
            .Where(x => x.CodigoBarra == codigo.Trim() && x.Activo)
            .FirstOrDefaultAsync();

        return p is null ? NotFound() : Ok(ToDto(p));
    }

    public async Task<IActionResult> BuscarNombre(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(Array.Empty<object>());

        var busqueda = q.ToLower().Trim();
        var lista = await db.Productos
            .Where(p => p.Activo && p.Nombre.ToLower().Contains(busqueda))
            .OrderBy(p => p.Nombre)
            .Take(8)
            .ToListAsync();

        return Ok(lista.Select(ToDto));
    }

    private static ProductoCompraBusquedaDto ToDto(Producto p) => new(
        p.Id, p.Nombre, p.CodigoBarra,
        p.PrecioCompra, p.PrecioVenta,
        p.StockActual,
        p.TipoVenta.ToString(),
        p.TipoVenta == TipoVenta.Kg || p.TipoVenta == TipoVenta.Gramo,
        p.TipoVenta switch
        {
            TipoVenta.Kg => "kg",
            TipoVenta.Gramo => "g",
            TipoVenta.Cien_g => "×100g",
            _ => "u"
        }
    );
}
