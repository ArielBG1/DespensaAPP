using System.ComponentModel.DataAnnotations;

namespace DespensaApp.Models.ViewModels;

public record ProveedorListItemVm(
    int Id, string Nombre, string? Contacto, string? Telefono,
    string? Email, bool Activo, int NumCompras);

public class ProveedorFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(150)]
    [Display(Name = "Nombre / Razón social")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Contacto")]
    public string? Contacto { get; set; }

    [MaxLength(30)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(150)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [MaxLength(250)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}
