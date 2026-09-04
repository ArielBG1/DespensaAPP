using System.ComponentModel.DataAnnotations;
using DespensaApp.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DespensaApp.Models.ViewModels;

public class ProductoFormViewModel
{
    public int Id { get; set; }

    [MaxLength(50)]
    [Display(Name = "Código de barras")]
    public string? CodigoBarra { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(200)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Categoría")]
    public int? CategoriaId { get; set; }

    [Display(Name = "Proveedor")]
    public int? ProveedorId { get; set; }

    [Display(Name = "Tipo de venta")]
    public TipoVenta TipoVenta { get; set; } = TipoVenta.Unidad;

    [Range(0, 9999999.99, ErrorMessage = "El precio debe ser mayor o igual a 0")]
    [Display(Name = "Precio de compra")]
    public decimal PrecioCompra { get; set; }

    [Range(0.01, 9999999.99, ErrorMessage = "El precio de venta debe ser mayor a 0")]
    [Display(Name = "Precio de venta")]
    public decimal PrecioVenta { get; set; }

    [Range(0, 999999.999, ErrorMessage = "Stock inválido")]
    [Display(Name = "Stock actual")]
    public decimal StockActual { get; set; }

    [Range(0, 999999.999, ErrorMessage = "Stock mínimo inválido")]
    [Display(Name = "Stock mínimo")]
    public decimal StockMinimo { get; set; }

    [Display(Name = "Imagen")]
    public IFormFile? ImagenFile { get; set; }

    public string? ImagenUrl { get; set; }

    [Display(Name = "Fecha de vencimiento")]
    [DataType(DataType.Date)]
    public DateTime? FechaVencimiento { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [Display(Name = "Códigos de barra alternativos")]
    public List<string> CodigosAlternativos { get; set; } = [];

    // Dropdown data (not bound from form)
    public List<SelectListItem> CategoriasItems { get; set; } = [];
    public List<SelectListItem> ProveedoresItems { get; set; } = [];

    public bool EsNuevo => Id == 0;

    public string UnidadLabel => TipoVenta switch
    {
        TipoVenta.Unidad => "unidades",
        TipoVenta.Kg => "kg",
        TipoVenta.Gramo => "gramos",
        TipoVenta.Cien_g => "unidades de 100g",
        _ => ""
    };
}
