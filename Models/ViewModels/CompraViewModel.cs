using DespensaApp.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DespensaApp.Models.ViewModels;

public class CompraFormDto
{
    public int ProveedorId { get; set; }
    public string? NumeroRemito { get; set; }
    public string? Observacion { get; set; }
    public List<CompraItemDto> Items { get; set; } = [];
}

public record CompraItemDto(int ProductoId, decimal Cantidad, decimal PrecioUnitario);

public record ProductoCompraBusquedaDto(
    int Id, string Nombre, string? CodigoBarra,
    decimal PrecioCompra, decimal PrecioVenta,
    decimal StockActual, string TipoVenta, bool EsPorPeso, string UnidadLabel);

public class CompraIndexViewModel
{
    public List<CompraListItemViewModel> Compras { get; set; } = [];
    public int TotalItems { get; set; }
    public int TotalPaginas { get; set; }
    public CompraFiltroViewModel Filtro { get; set; } = new();
    public decimal TotalPeriodo { get; set; }
    public string MonedaSimbolo { get; set; } = "Gs.";
    public List<SelectListItem> Proveedores { get; set; } = [];
}

public class CompraFiltroViewModel
{
    public int Pagina { get; set; } = 1;
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? ProveedorId { get; set; }
}

public class CompraListItemViewModel
{
    public int Id { get; set; }
    public string? NumeroRemito { get; set; }
    public string Proveedor { get; set; } = "";
    public string Responsable { get; set; } = "";
    public int CantidadItems { get; set; }
    public decimal Total { get; set; }
    public EstadoCompra Estado { get; set; }
    public DateTime FechaCompra { get; set; }
}

public class CompraDetalleViewModel
{
    public Compra Compra { get; set; } = null!;
    public string MonedaSimbolo { get; set; } = "Gs.";
    public string NombreNegocio { get; set; } = "";
}
