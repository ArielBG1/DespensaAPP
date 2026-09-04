using DespensaApp.Models.Entities;

namespace DespensaApp.Models.ViewModels;

public class VentaDetalleViewModel
{
    public Venta Venta { get; set; } = null!;
    public string NombreNegocio { get; set; } = string.Empty;
    public string MonedaSimbolo { get; set; } = "Gs.";
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
}
