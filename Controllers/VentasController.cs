using DespensaApp.Models.ViewModels;
using DespensaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace DespensaApp.Controllers;

[Authorize]
public class VentasController(IVentaService ventaService, IConfiguracionService configService) : Controller
{
    // ─── PANTALLA DE CAJA ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Nueva()
    {
        ViewData["Title"] = "Nueva Venta";
        ViewData["ActiveMenu"] = "NuevaVenta";
        ViewData["NombreNegocio"] = await configService.ObtenerValorAsync("NombreNegocio", "Despensa App");
        ViewData["MonedaSimbolo"] = await configService.ObtenerValorAsync("MonedaSimbolo", "Gs.");
        return View();
    }

    // ─── AJAX: buscar producto por código de barras ───────────────────────────

    [HttpGet]
    public async Task<IActionResult> BuscarProducto(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return Json(new { encontrado = false });

        var prod = await ventaService.BuscarPorCodigoAsync(codigo);
        if (prod is null)
            return Json(new { encontrado = false, codigo });

        return Json(new { encontrado = true, producto = prod });
    }

    // ─── AJAX: búsqueda por nombre (autocomplete) ─────────────────────────────

    [HttpGet]
    public async Task<IActionResult> BuscarNombre(string q)
    {
        var lista = await ventaService.BuscarPorNombreAsync(q);
        return Json(lista);
    }

    // ─── PROCESAR VENTA (JSON) ────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Procesar([FromBody] ProcesarVentaDto dto)
    {
        if (dto is null || dto.Items.Count == 0)
            return Json(new { success = false, error = "Carrito vacío." });

        var (ok, error, ventaId, ticket) = await ventaService.ProcesarVentaAsync(dto, GetUserId());

        if (!ok)
            return Json(new { success = false, error });

        return Json(new { success = true, ventaId, numeroTicket = ticket });
    }

    // ─── TICKET IMPRIMIBLE ────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Ticket(int id)
    {
        var venta = await ventaService.GetByIdAsync(id);
        if (venta is null) return NotFound();

        var config = await configService.ObtenerTodasAsync();
        var vm = new VentaDetalleViewModel
        {
            Venta = venta,
            NombreNegocio = config.GetValueOrDefault("NombreNegocio", "Despensa App"),
            MonedaSimbolo = config.GetValueOrDefault("MonedaSimbolo", "Gs."),
            Direccion = config.GetValueOrDefault("Direccion", ""),
            Telefono = config.GetValueOrDefault("Telefono", "")
        };

        return View(vm);
    }

    // ─── DETALLE DE VENTA ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var venta = await ventaService.GetByIdAsync(id);
        if (venta is null) return NotFound();

        // Solo Admin o el propio cajero puede ver la venta
        if (!User.IsInRole("Admin") && venta.UsuarioId != GetUserId())
            return Forbid();

        var config = await configService.ObtenerTodasAsync();
        var vm = new VentaDetalleViewModel
        {
            Venta = venta,
            NombreNegocio = config.GetValueOrDefault("NombreNegocio", "Despensa App"),
            MonedaSimbolo = config.GetValueOrDefault("MonedaSimbolo", "Gs."),
            Direccion = config.GetValueOrDefault("Direccion", ""),
            Telefono = config.GetValueOrDefault("Telefono", "")
        };

        return View(vm);
    }

    // ─── HISTORIAL ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] VentaFiltroViewModel filtro)
    {
        var vm = await ventaService.GetListadoAsync(filtro, GetUserId(), User.IsInRole("Admin"));
        ViewData["Title"] = "Historial de Ventas";
        ViewData["ActiveMenu"] = "Ventas";
        return View(vm);
    }

    // ─── MIS VENTAS DEL DÍA (sidebar cajero) ─────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> MisVentasHoy()
    {
        var filtro = new VentaFiltroViewModel
        {
            Periodo = PeriodoFiltro.Hoy,
            UsuarioId = GetUserId()
        };
        var vm = await ventaService.GetListadoAsync(filtro, GetUserId(), isAdmin: false);
        ViewData["Title"] = "Mis Ventas del Día";
        ViewData["ActiveMenu"] = "MisVentas";
        vm.SoloMisVentas = true;
        return View("Index", vm);
    }

    // ─── ANULAR VENTA ─────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(int id)
    {
        var (ok, error) = await ventaService.AnularVentaAsync(id, GetUserId());
        if (!ok) TempData["Error"] = error;
        else TempData["Success"] = "Venta anulada. El stock ha sido restituido.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ─── EXPORTAR CSV ─────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportarCsv([FromQuery] VentaFiltroViewModel filtro)
    {
        var ventas = await ventaService.GetParaExportAsync(filtro, GetUserId(), isAdmin: true);

        var csv = new StringBuilder();
        csv.AppendLine("Ticket,Fecha,Cajero,Items,Subtotal,Descuento,Total,MetodoPago,Estado");

        foreach (var v in ventas)
        {
            csv.AppendLine(string.Join(',',
                $"\"{v.NumeroTicket}\"",
                v.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                $"\"{v.Cajero}\"",
                v.CantidadItems,
                v.Subtotal.ToString("F2"),
                v.Descuento.ToString("F2"),
                v.Total.ToString("F2"),
                v.MetodoPago,
                v.Estado));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv; charset=utf-8",
            $"ventas_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
