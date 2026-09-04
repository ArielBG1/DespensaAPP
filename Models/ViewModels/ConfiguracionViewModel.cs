namespace DespensaApp.Models.ViewModels;

public class ConfiguracionViewModel
{
    public string NombreNegocio { get; set; } = "Despensa App";
    public string Direccion { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string MonedaSimbolo { get; set; } = "Gs.";
    public string MonedaNombre { get; set; } = "Guaraní";
    public string IVAPorcentaje { get; set; } = "0";
    public string PieTicket { get; set; } = "¡Gracias por su compra!";
    public string SesionMinutos { get; set; } = "480";
    public string StockMinimoAlerta { get; set; } = "5";
}
