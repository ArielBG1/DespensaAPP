using DespensaApp.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DespensaApp.Models.ViewModels;

public enum PeriodoFiltro { Hoy, Semana, Mes, Personalizado }

public class VentaFiltroViewModel
{
    public PeriodoFiltro Periodo { get; set; } = PeriodoFiltro.Hoy;
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? UsuarioId { get; set; }
    public MetodoPago? MetodoPago { get; set; }
    public EstadoVenta? Estado { get; set; }
    public int Pagina { get; set; } = 1;
    public const int ItemsPorPagina = 25;
}

public class VentaListItemViewModel
{
    public int Id { get; set; }
    public string NumeroTicket { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string Cajero { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public EstadoVenta Estado { get; set; }
    public int CantidadItems { get; set; }

    public string EstadoClase => Estado switch
    {
        EstadoVenta.Completada => "success",
        EstadoVenta.Anulada => "danger",
        EstadoVenta.EnProceso => "warning",
        _ => "secondary"
    };

    public string MetodoPagoIcono => MetodoPago switch
    {
        MetodoPago.Efectivo => "fa-money-bill-wave",
        MetodoPago.Tarjeta => "fa-credit-card",
        MetodoPago.Transferencia => "fa-exchange-alt",
        MetodoPago.QR => "fa-qrcode",
        _ => "fa-question"
    };

    public string MetodoPagoLabel => MetodoPago switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.Tarjeta => "Tarjeta",
        MetodoPago.Transferencia => "Transf.",
        MetodoPago.QR => "QR",
        _ => MetodoPago.ToString()
    };
}

public class VentaIndexViewModel
{
    public List<VentaListItemViewModel> Ventas { get; set; } = [];
    public VentaFiltroViewModel Filtro { get; set; } = new();
    public decimal TotalPeriodo { get; set; }
    public int CantidadVentas { get; set; }
    public int TotalItems { get; set; }
    public int TotalPaginas { get; set; }
    public List<SelectListItem> Cajeros { get; set; } = [];
    public bool SoloMisVentas { get; set; }
}
