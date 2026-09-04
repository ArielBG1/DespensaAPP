using DespensaApp.Data;
using DespensaApp.Models.Entities;
using DespensaApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DespensaApp.Controllers;

[Authorize(Roles = "Admin")]
public class ProveedoresController(DespensaDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var lista = await db.Proveedores
            .OrderBy(p => p.Nombre)
            .Select(p => new ProveedorListItemVm(
                p.Id, p.Nombre, p.Contacto, p.Telefono,
                p.Email, p.Activo,
                p.Compras.Count))
            .ToListAsync();

        ViewData["ActiveMenu"] = "Proveedores";
        return View(lista);
    }

    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "Proveedores";
        return View(new ProveedorFormViewModel { Activo = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProveedorFormViewModel vm)
    {
        if (!ModelState.IsValid) { ViewData["ActiveMenu"] = "Proveedores"; return View(vm); }

        var p = new Proveedor
        {
            Nombre = vm.Nombre.Trim(),
            Contacto = vm.Contacto?.Trim(),
            Telefono = vm.Telefono?.Trim(),
            Email = vm.Email?.Trim().ToLower(),
            Direccion = vm.Direccion?.Trim(),
            Activo = vm.Activo
        };

        db.Proveedores.Add(p);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Proveedor '{p.Nombre}' creado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await db.Proveedores.FindAsync(id);
        if (p is null) return NotFound();

        var vm = new ProveedorFormViewModel
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Contacto = p.Contacto,
            Telefono = p.Telefono,
            Email = p.Email,
            Direccion = p.Direccion,
            Activo = p.Activo
        };
        ViewData["ActiveMenu"] = "Proveedores";
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProveedorFormViewModel vm)
    {
        if (!ModelState.IsValid) { ViewData["ActiveMenu"] = "Proveedores"; return View(vm); }

        var p = await db.Proveedores.FindAsync(vm.Id);
        if (p is null) return NotFound();

        p.Nombre = vm.Nombre.Trim();
        p.Contacto = vm.Contacto?.Trim();
        p.Telefono = vm.Telefono?.Trim();
        p.Email = vm.Email?.Trim().ToLower();
        p.Direccion = vm.Direccion?.Trim();
        p.Activo = vm.Activo;
        p.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        TempData["Success"] = $"Proveedor '{p.Nombre}' actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActivo(int id)
    {
        var p = await db.Proveedores.FindAsync(id);
        if (p is null) return NotFound();
        p.Activo = !p.Activo;
        p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Proveedor '{p.Nombre}' {(p.Activo ? "activado" : "desactivado")}.";
        return RedirectToAction(nameof(Index));
    }
}
