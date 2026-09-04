using DespensaApp.Models.Entities;

namespace DespensaApp.Models.ViewModels;

public class MovimientoListItemViewModel
{
    public int Id { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal StockAntes { get; set; }
    public decimal StockDespues { get; set; }
    public string? Motivo { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }

    public string TipoClase => Tipo switch
    {
        TipoMovimiento.Entrada or TipoMovimiento.Compra or TipoMovimiento.Devolucion or TipoMovimiento.AjustePositivo
            => "success",
        TipoMovimiento.Salida or TipoMovimiento.Venta or TipoMovimiento.AjusteNegativo
            => "danger",
        _ => "secondary"
    };

    public string TipoIcono => Tipo switch
    {
        TipoMovimiento.Entrada => "fa-arrow-circle-down",
        TipoMovimiento.Salida => "fa-arrow-circle-up",
        TipoMovimiento.AjustePositivo => "fa-plus-circle",
        TipoMovimiento.AjusteNegativo => "fa-minus-circle",
        TipoMovimiento.Venta => "fa-cash-register",
        TipoMovimiento.Compra => "fa-truck",
        TipoMovimiento.Devolucion => "fa-undo",
        _ => "fa-exchange-alt"
    };

    public string TipoLabel => Tipo switch
    {
        TipoMovimiento.Entrada => "Entrada",
        TipoMovimiento.Salida => "Salida",
        TipoMovimiento.AjustePositivo => "Ajuste +",
        TipoMovimiento.AjusteNegativo => "Ajuste −",
        TipoMovimiento.Venta => "Venta",
        TipoMovimiento.Compra => "Compra",
        TipoMovimiento.Devolucion => "Devolución",
        _ => Tipo.ToString()
    };
}

public class MovimientoFiltroViewModel
{
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public string? CodigoBarra { get; set; }
    public decimal StockActual { get; set; }
    public TipoVenta TipoVentaProducto { get; set; }

    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<MovimientoListItemViewModel> Movimientos { get; set; } = [];
    public int TotalMovimientos { get; set; }

    public string UnidadStock => TipoVentaProducto switch
    {
        TipoVenta.Unidad => "u",
        TipoVenta.Kg => "kg",
        TipoVenta.Gramo => "g",
        TipoVenta.Cien_g => "×100g",
        _ => ""
    };
}
