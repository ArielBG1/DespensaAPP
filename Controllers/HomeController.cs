using DespensaApp.Data;
using DespensaApp.Models;
using DespensaApp.Models.Entities;
using DespensaApp.Models.ViewModels;
using DespensaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DespensaApp.Controllers;

[Authorize]
public class HomeController(DespensaDbContext db, IConfiguracionService configService) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!User.IsInRole("Admin"))
            return RedirectToAction("Nueva", "Ventas");

        var hoy = DateTime.UtcNow.Date;
        var manana = hoy.AddDays(1);

        var ventasHoy = await db.Ventas
            .Where(v => v.CreatedAt >= hoy && v.CreatedAt < manana && v.Estado == EstadoVenta.Completada)
            .ToListAsync();

        var totalHoy = ventasHoy.Sum(v => v.Total);
        var transacciones = ventasHoy.Count;
        var ticketPromedio = transacciones > 0 ? Math.Round(totalHoy / transacciones, 2) : 0m;
        var stockBajoCount = await db.Productos
            .CountAsync(p => p.Activo && p.StockActual <= p.StockMinimo);

        var limiteVencimiento = hoy.AddDays(ProductoListItemViewModel.DiasAlertaVencimiento + 1);
        var porVencerCount = await db.Productos
            .CountAsync(p => p.Activo && p.FechaVencimiento != null && p.FechaVencimiento < limiteVencimiento);

        var labels = new List<string>();
        var totales = new List<decimal>();
        for (int i = 6; i >= 0; i--)
        {
            var dia = hoy.AddDays(-i);
            labels.Add(dia.ToString("dd/MM"));
            var t = await db.Ventas
                .Where(v => v.CreatedAt >= dia && v.CreatedAt < dia.AddDays(1) && v.Estado == EstadoVenta.Completada)
                .SumAsync(v => (decimal?)v.Total) ?? 0m;
            totales.Add(t);
        }

        var alertas = await db.Productos
            .Where(p => p.Activo && p.StockActual <= p.StockMinimo)
            .OrderBy(p => p.StockActual - p.StockMinimo)
            .Take(10)
            .Select(p => new ProductoStockAlertaDto(p.Id, p.Nombre, p.CodigoBarra, p.StockActual, p.StockMinimo, p.TipoVenta.ToString()))
            .ToListAsync();

        var alertasVencimiento = await db.Productos
            .Where(p => p.Activo && p.FechaVencimiento != null && p.FechaVencimiento < limiteVencimiento)
            .OrderBy(p => p.FechaVencimiento)
            .Take(10)
            .Select(p => new ProductoVencimientoAlertaDto(p.Id, p.Nombre, p.FechaVencimiento!.Value))
            .ToListAsync();

        var ultimas = await db.Ventas
            .Include(v => v.Usuario)
            .Where(v => v.CreatedAt >= hoy && v.CreatedAt < manana && v.Estado == EstadoVenta.Completada)
            .OrderByDescending(v => v.CreatedAt)
            .Take(5)
            .Select(v => new VentaResumenDto(v.Id, v.NumeroTicket, v.Total, v.Usuario!.Nombre, v.CreatedAt))
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            TotalVendidoHoy = totalHoy,
            TransaccionesHoy = transacciones,
            TicketPromedio = ticketPromedio,
            ProductosStockBajo = stockBajoCount,
            ProductosPorVencer = porVencerCount,
            GraficoLabels = labels,
            GraficoTotales = totales,
            AlertasStock = alertas,
            AlertasVencimiento = alertasVencimiento,
            UltimasVentas = ultimas,
            MonedaSimbolo = await configService.ObtenerValorAsync("MonedaSimbolo", "Gs.")
        };

        ViewData["NombreNegocio"] = await configService.ObtenerValorAsync("NombreNegocio", "Despensa App");
        ViewData["ActiveMenu"] = "Dashboard";
        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult Error(int? statusCode)
    {
        var vm = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode ?? 500
        };
        return View(vm);
    }
}
