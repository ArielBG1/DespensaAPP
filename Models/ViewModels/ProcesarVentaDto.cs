namespace DespensaApp.Models.ViewModels;

// ─── DTOs para el endpoint /Ventas/Procesar (JSON) ──────────────────────────

public record CartItemDto(
    int ProductoId,
    string Nombre,
    string? CodigoBarra,
    decimal PrecioUnitario,
    decimal Cantidad,
    decimal DescuentoItem,
    bool EsPorPeso,
    string TipoVenta
);

public record ProcesarVentaDto(
    List<CartItemDto> Items,
    string MetodoPago,
    decimal Descuento,
    decimal MontoRecibido,
    string? Observacion
);

// ─── DTO para respuesta de búsqueda de producto (AJAX) ──────────────────────

public record ProductoBusquedaDto(
    int Id,
    string Nombre,
    string? CodigoBarra,
    decimal PrecioVenta,
    decimal StockActual,
    string TipoVenta,
    bool EsPorPeso,
    string Unidad
);
