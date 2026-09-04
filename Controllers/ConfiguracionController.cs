using DespensaApp.Models.ViewModels;
using DespensaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DespensaApp.Controllers;

[Authorize(Roles = "Admin")]
public class ConfiguracionController(IConfiguracionService configService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var all = await configService.ObtenerTodasAsync();
        var vm = new ConfiguracionViewModel
        {
            NombreNegocio   = all.GetValueOrDefault("NombreNegocio", "Despensa App"),
            Direccion       = all.GetValueOrDefault("Direccion", ""),
            Telefono        = all.GetValueOrDefault("Telefono", ""),
            MonedaSimbolo   = all.GetValueOrDefault("MonedaSimbolo", "Gs."),
            MonedaNombre    = all.GetValueOrDefault("MonedaNombre", "Guaraní"),
            IVAPorcentaje   = all.GetValueOrDefault("IVAPorcentaje", "0"),
            PieTicket       = all.GetValueOrDefault("PieTicket", "¡Gracias por su compra!"),
            SesionMinutos   = all.GetValueOrDefault("SesionMinutos", "480"),
            StockMinimoAlerta = all.GetValueOrDefault("StockMinimoAlerta", "5"),
        };
        ViewData["ActiveMenu"] = "Configuracion";
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ConfiguracionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ActiveMenu"] = "Configuracion";
            return View(vm);
        }

        await configService.SetVariosAsync(new Dictionary<string, string>
        {
            ["NombreNegocio"]     = vm.NombreNegocio.Trim(),
            ["Direccion"]         = vm.Direccion.Trim(),
            ["Telefono"]          = vm.Telefono.Trim(),
            ["MonedaSimbolo"]     = vm.MonedaSimbolo.Trim(),
            ["MonedaNombre"]      = vm.MonedaNombre.Trim(),
            ["IVAPorcentaje"]     = vm.IVAPorcentaje.Trim(),
            ["PieTicket"]         = vm.PieTicket.Trim(),
            ["SesionMinutos"]     = vm.SesionMinutos.Trim(),
            ["StockMinimoAlerta"] = vm.StockMinimoAlerta.Trim(),
        });

        TempData["Success"] = "Configuración guardada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
