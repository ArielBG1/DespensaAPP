using System.Text.RegularExpressions;
using DespensaApp.Data;
using DespensaApp.Models.Entities;
using DespensaApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace DespensaApp.Controllers;

[Authorize(Roles = "Admin")]
public class UsuariosController(DespensaDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var lista = await db.Usuarios
            .OrderBy(u => u.Nombre)
            .Select(u => new UsuarioListItemViewModel
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Email = u.Email,
                Rol = u.Rol,
                Activo = u.Activo,
                UltimoAcceso = u.UltimoAcceso,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        ViewData["ActiveMenu"] = "Usuarios";
        return View(lista);
    }

    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "Usuarios";
        return View(new UsuarioFormViewModel { Activo = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioFormViewModel vm)
    {
        if (!ValidarPassword(vm.Password, required: true))
            ModelState.AddModelError("Password", "La contraseña debe tener mínimo 8 caracteres, 1 mayúscula y 1 número.");

        if (!ModelState.IsValid)
        {
            ViewData["ActiveMenu"] = "Usuarios";
            return View(vm);
        }

        if (await db.Usuarios.AnyAsync(u => u.Email == vm.Email.ToLower().Trim()))
        {
            ModelState.AddModelError("Email", "Ya existe un usuario con ese email.");
            ViewData["ActiveMenu"] = "Usuarios";
            return View(vm);
        }

        var usuario = new Usuario
        {
            Nombre = vm.Nombre.Trim(),
            Email = vm.Email.ToLower().Trim(),
            PasswordHash = BC.HashPassword(vm.Password!, 12),
            Rol = vm.Rol,
            Activo = vm.Activo
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Usuario '{usuario.Nombre}' creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();

        var vm = new UsuarioFormViewModel
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Email = u.Email,
            Rol = u.Rol,
            Activo = u.Activo
        };
        ViewData["ActiveMenu"] = "Usuarios";
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UsuarioFormViewModel vm)
    {
        if (!string.IsNullOrEmpty(vm.Password) && !ValidarPassword(vm.Password, required: false))
            ModelState.AddModelError("Password", "La contraseña debe tener mínimo 8 caracteres, 1 mayúscula y 1 número.");

        if (!ModelState.IsValid)
        {
            ViewData["ActiveMenu"] = "Usuarios";
            return View(vm);
        }

        var u = await db.Usuarios.FindAsync(vm.Id);
        if (u is null) return NotFound();

        if (await db.Usuarios.AnyAsync(x => x.Email == vm.Email.ToLower().Trim() && x.Id != vm.Id))
        {
            ModelState.AddModelError("Email", "Ya existe un usuario con ese email.");
            ViewData["ActiveMenu"] = "Usuarios";
            return View(vm);
        }

        u.Nombre = vm.Nombre.Trim();
        u.Email = vm.Email.ToLower().Trim();
        u.Rol = vm.Rol;
        u.Activo = vm.Activo;
        u.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(vm.Password))
            u.PasswordHash = BC.HashPassword(vm.Password, 12);

        await db.SaveChangesAsync();
        TempData["Success"] = $"Usuario '{u.Nombre}' actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActivo(int id)
    {
        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();

        u.Activo = !u.Activo;
        u.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Usuario '{u.Nombre}' {(u.Activo ? "activado" : "desactivado")}.";
        return RedirectToAction(nameof(Index));
    }

    private static bool ValidarPassword(string? pwd, bool required)
    {
        if (string.IsNullOrEmpty(pwd)) return !required;
        return Regex.IsMatch(pwd, @"^(?=.*[A-Z])(?=.*\d).{8,}$");
    }
}
