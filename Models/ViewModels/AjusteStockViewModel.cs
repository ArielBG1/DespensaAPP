using System.ComponentModel.DataAnnotations;
using DespensaApp.Models.Entities;

namespace DespensaApp.Models.ViewModels;

public class AjusteStockViewModel
{
    [Required]
    public int ProductoId { get; set; }

    // Display only (not posted)
    public string NombreProducto { get; set; } = string.Empty;
    public decimal StockActual { get; set; }
    public string UnidadStock { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione el tipo de movimiento")]
    [Display(Name = "Tipo de movimiento")]
    public TipoMovimiento Tipo { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria")]
    [Range(0.001, 999999.999, ErrorMessage = "La cantidad debe ser mayor a 0")]
    [Display(Name = "Cantidad")]
    public decimal Cantidad { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio")]
    [MaxLength(300)]
    [Display(Name = "Motivo / Observación")]
    public string Motivo { get; set; } = string.Empty;
}
