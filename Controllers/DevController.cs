using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BC = BCrypt.Net.BCrypt;

namespace DespensaApp.Controllers;

/// <summary>
/// Solo para desarrollo: genera hashes de contraseñas.
/// </summary>
[Authorize(Roles = "Admin")]
public class DevController : Controller
{
    [HttpGet]
    public IActionResult GenerarHash(string password = "")
    {
        if (string.IsNullOrWhiteSpace(password))
            return Content("Usa: /Dev/GenerarHash?password=TuPassword");

        var hash = BC.HashPassword(password, 12);
        return Content($"Password: {password}\nHash: {hash}");
    }

    [HttpGet]
    public IActionResult VerificarHash(string password = "", string hash = "")
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return Content("Usa: /Dev/VerificarHash?password=TuPassword&hash=TuHash");

        var valido = BC.Verify(password, hash);
        return Content($"Válido: {valido}");
    }
}
