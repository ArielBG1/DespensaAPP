namespace DespensaApp.Models.ViewModels;

public class DashboardViewModel
{
    public decimal TotalVendidoHoy { get; set; }
    public int TransaccionesHoy { get; set; }
    public decimal TicketPromedio { get; set; }
    public int ProductosStockBajo { get; set; }
    public int ProductosPorVencer { get; set; }
    public string MonedaSimbolo { get; set; } = "Gs.";

    public List<string> GraficoLabels { get; set; } = [];
    public List<decimal> GraficoTotales { get; set; } = [];

    public List<ProductoStockAlertaDto> AlertasStock { get; set; } = [];
    public List<ProductoVencimientoAlertaDto> AlertasVencimiento { get; set; } = [];
    public List<VentaResumenDto> UltimasVentas { get; set; } = [];
}

public record ProductoStockAlertaDto(
    int Id, string Nombre, string? CodigoBarra,
    decimal StockActual, decimal StockMinimo, string TipoVenta);

public record ProductoVencimientoAlertaDto(
    int Id, string Nombre, DateTime FechaVencimiento);

public record VentaResumenDto(
    int Id, string NumeroTicket, decimal Total, string Cajero, DateTime FechaHora);
