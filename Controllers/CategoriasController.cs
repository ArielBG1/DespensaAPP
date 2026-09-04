using DespensaApp.Data;
using DespensaApp.Models.Entities;
using DespensaApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DespensaApp.Controllers;

[Authorize(Roles = "Admin")]
public class CategoriasController(DespensaDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var lista = await db.Categorias
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaListItemVm(
                c.Id, c.Nombre, c.Descripcion, c.Activo,
                c.Productos.Count(p => p.Activo)))
            .ToListAsync();

        ViewData["ActiveMenu"] = "Categorias";
        return View(lista);
    }

    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "Categorias";
        return View(new Categoria());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Categoria cat)
    {
        if (!ModelState.IsValid) { ViewData["ActiveMenu"] = "Categorias"; return View(cat); }

        if (await db.Categorias.AnyAsync(c => c.Nombre == cat.Nombre))
        {
            ModelState.AddModelError("Nombre", "Ya existe una categoría con ese nombre.");
            ViewData["ActiveMenu"] = "Categorias";
            return View(cat);
        }

        cat.CreatedAt = DateTime.UtcNow;
        db.Categorias.Add(cat);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Categoría '{cat.Nombre}' creada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var cat = await db.Categorias.FindAsync(id);
        if (cat is null) return NotFound();
        ViewData["ActiveMenu"] = "Categorias";
        return View(cat);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Categoria cat)
    {
        if (!ModelState.IsValid) { ViewData["ActiveMenu"] = "Categorias"; return View(cat); }

        if (await db.Categorias.AnyAsync(c => c.Nombre == cat.Nombre && c.Id != cat.Id))
        {
            ModelState.AddModelError("Nombre", "Ya existe una categoría con ese nombre.");
            ViewData["ActiveMenu"] = "Categorias";
            return View(cat);
        }

        db.Categorias.Update(cat);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Categoría '{cat.Nombre}' actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActivo(int id)
    {
        var cat = await db.Categorias.FindAsync(id);
        if (cat is null) return NotFound();
        cat.Activo = !cat.Activo;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Categoría '{cat.Nombre}' {(cat.Activo ? "activada" : "desactivada")}.";
        return RedirectToAction(nameof(Index));
    }
}
