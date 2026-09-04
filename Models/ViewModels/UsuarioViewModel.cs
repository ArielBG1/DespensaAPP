using System.ComponentModel.DataAnnotations;
using DespensaApp.Models.Entities;

namespace DespensaApp.Models.ViewModels;

public class UsuarioFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(100)]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(150)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [MinLength(8, ErrorMessage = "Mínimo 8 caracteres")]
    [Display(Name = "Contraseña")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El rol es requerido")]
    [Display(Name = "Rol")]
    public RolUsuario Rol { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    public bool EsNuevo => Id == 0;
}

public class UsuarioListItemViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public bool Activo { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public DateTime CreatedAt { get; set; }
}
