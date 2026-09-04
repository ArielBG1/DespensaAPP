namespace DespensaApp.Models.ViewModels;

public class UsuarioClaimsViewModel
{
    public int UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;

    public bool EsAdmin => Rol == "Admin";
    public bool EsCajero => Rol == "Cajero";
}
