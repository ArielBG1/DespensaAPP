using DespensaApp.Data;
using DespensaApp.Models.Entities;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace DespensaApp.Services;

public class AuthService(DespensaDbContext db) : IAuthService
{
    public async Task<Usuario?> ValidarCredencialesAsync(string email, string password)
    {
        var usuario = await db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email.ToLower().Trim() && u.Activo);

        if (usuario is null) return null;
        if (!BC.Verify(password, usuario.PasswordHash)) return null;

        return usuario;
    }

    public async Task RegistrarUltimoAccesoAsync(int usuarioId)
    {
        var usuario = await db.Usuarios.FindAsync(usuarioId);
        if (usuario is not null)
        {
            usuario.UltimoAcceso = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}
