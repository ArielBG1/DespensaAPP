using DespensaApp.Models.Entities;

namespace DespensaApp.Services;

public interface IAuthService
{
    Task<Usuario?> ValidarCredencialesAsync(string email, string password);
    Task RegistrarUltimoAccesoAsync(int usuarioId);
}
