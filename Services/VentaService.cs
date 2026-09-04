using DespensaApp.Data;
using DespensaApp.Models.Entities;
using DespensaApp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DespensaApp.Services;

public class VentaService(DespensaDbContext db) : IVentaService
{
    // ─── Búsquedas de producto (AJAX) ─────────────────────────────────────────

    public async Task<ProductoBusquedaDto?> BuscarPorCodigoAsync(string codigo)
    {
        var codigoLimpio = codigo.Trim();

        var p = await db.Productos
            .Where(x => x.CodigoBarra == codigoLimpio && x.Activo)
            .FirstOrDefaultAsync();

        if (p is not null) return ToDto(p);

        // No matcheó el código principal: probar entre los alternativos del producto.
        var alterno = await db.CodigosBarraAlternativos
            .Include(c => c.Producto)
            .Where(c => c.Codigo == codigoLimpio && c.Producto!.Activo)
            .Select(c => c.Producto)
            .FirstOrDefaultAsync();

        return alterno is null ? null : ToDto(alterno);
    }

    public async Task<List<ProductoBusquedaDto>> BuscarPorNombreAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return [];

        var busqueda = q.ToLower().Trim();

        var lista = await db.Productos
            .Where(p => p.Activo && p.Nombre.ToLower().Contains(busqueda))
            .OrderBy(p => p.Nombre)
            .Take(8)
            .ToListAsync();

        return lista.Select(ToDto).ToList();
    }

    private static ProductoBusquedaDto ToDto(Producto p) => new(
        p.Id,
        p.Nombre,
        p.CodigoBarra,
        p.PrecioVenta,
        p.StockActual,
        p.TipoVenta.ToString(),
        p.TipoVenta == TipoVenta.Kg || p.TipoVenta == TipoVenta.Gramo,
        p.TipoVenta switch
        {
            TipoVenta.Unidad => "u",
            TipoVenta.Kg => "kg",
            TipoVenta.Gramo => "g",
            TipoVenta.Cien_g => "×100g",
            _ => "u"
        }
    );

    // ─── Procesar venta ────────────────────────────────────────────────────────

    public async Task<(bool success, string? error, int ventaId, string numeroTicket)>
        ProcesarVentaAsync(ProcesarVentaDto dto, int usuarioId)
    {
        if (dto.Items.Count == 0)
            return (false, "El carrito está vacío.", 0, "");

        if (!Enum.TryParse<MetodoPago>(dto.MetodoPago, out var metodoPago))
            return (false, "Método de pago inválido.", 0, "");

        // Calcular totales
        var subtotal = dto.Items.Sum(i => i.PrecioUnitario * i.Cantidad - i.DescuentoItem);
        var descuento = Math.Max(0, dto.Descuento);
        var total = subtotal - descuento;
        var vuelto = metodoPago == MetodoPago.Efectivo
            ? Math.Max(0, dto.MontoRecibido - total)
            : 0;

        // El número de ticket se recalcula en cada intento: si dos ventas concurrentes
        // calculan el mismo número, la restricción única de "numero_ticket" hace fallar
        // a la segunda, que reintenta con el siguiente número ya actualizado.
        const int maxIntentosTicket = 10;
        for (var intento = 1; intento <= maxIntentosTicket; intento++)
        {
            var numeroTicket = await GenerarNumeroTicketAsync();

            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                // Crear venta
                var venta = new Venta
                {
                    NumeroTicket = numeroTicket,
                    UsuarioId = usuarioId,
                    Subtotal = subtotal,
                    Descuento = descuento,
                    Total = total,
                    MetodoPago = metodoPago,
                    MontoRecibido = dto.MontoRecibido,
                    Vuelto = vuelto,
                    Estado = EstadoVenta.Completada,
                    Observacion = dto.Observacion?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                db.Ventas.Add(venta);
                await db.SaveChangesAsync(); // obtiene venta.Id (puede chocar por ticket duplicado)

                // Crear detalles + descontar stock de forma atómica (UPDATE condicional:
                // si dos ventas concurrentes tocan el mismo producto, la segunda ve el
                // stock ya descontado por la primera y falla en vez de vender de más).
                foreach (var item in dto.Items)
                {
                    var itemSub = item.PrecioUnitario * item.Cantidad - item.DescuentoItem;

                    var (resultado, nuevoStock) = await StockOperations.AplicarDeltaAsync(
                        db, item.ProductoId, -item.Cantidad);

                    if (resultado == StockOperations.ResultadoStock.NoEncontrado)
                        throw new Exception($"Producto «{item.Nombre}» (id {item.ProductoId}) no encontrado.");
                    if (resultado == StockOperations.ResultadoStock.Insuficiente)
                        throw new Exception($"Stock insuficiente para «{item.Nombre}».");

                    db.DetallesVenta.Add(new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        DescuentoItem = item.DescuentoItem,
                        Subtotal = itemSub,
                        EsPorPeso = item.EsPorPeso,
                        NombreProducto = item.Nombre,
                        CodigoBarra = item.CodigoBarra
                    });

                    db.MovimientosStock.Add(new MovimientoStock
                    {
                        ProductoId = item.ProductoId,
                        Tipo = TipoMovimiento.Venta,
                        Cantidad = item.Cantidad,
                        StockAntes = nuevoStock + item.Cantidad,
                        StockDespues = nuevoStock,
                        Motivo = $"Venta {numeroTicket}",
                        ReferenciaId = venta.Id,
                        UsuarioId = usuarioId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return (true, null, venta.Id, numeroTicket);
            }
            catch (DbUpdateException ex) when (EsNumeroTicketDuplicado(ex) && intento < maxIntentosTicket)
            {
                await tx.RollbackAsync();
                // El rollback revierte la transacción en la base, pero no el change
                // tracker de EF: sin este Clear(), el "venta" fallido queda "Added" y
                // el próximo intento lo reinserta con el mismo ticket que ya chocó.
                db.ChangeTracker.Clear();
                // Backoff aleatorio: si varias ventas chocan al mismo tiempo, sin esto
                // los reintentos quedan sincronizados y vuelven a chocar entre sí.
                await Task.Delay(Random.Shared.Next(10, 40) * intento);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, ex.Message, 0, "");
            }
        }

        return (false, "No se pudo generar un número de ticket único. Intente nuevamente.", 0, "");
    }

    private static bool EsNumeroTicketDuplicado(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && pg.ConstraintName == "IX_ventas_numero_ticket";

    // ─── Anular venta ──────────────────────────────────────────────────────────

    public async Task<(bool success, string? error)> AnularVentaAsync(int ventaId, int usuarioId)
    {
        var venta = await db.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == ventaId);

        if (venta is null) return (false, "Venta no encontrada.");
        if (venta.Estado == EstadoVenta.Anulada) return (false, "La venta ya está anulada.");

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            venta.Estado = EstadoVenta.Anulada;

            foreach (var det in venta.Detalles)
            {
                var (resultado, nuevoStock) = await StockOperations.AplicarDeltaAsync(
                    db, det.ProductoId, det.Cantidad);
                if (resultado == StockOperations.ResultadoStock.NoEncontrado) continue;

                db.MovimientosStock.Add(new MovimientoStock
                {
                    ProductoId = det.ProductoId,
                    Tipo = TipoMovimiento.Devolucion,
                    Cantidad = det.Cantidad,
                    StockAntes = nuevoStock - det.Cantidad,
                    StockDespues = nuevoStock,
                    Motivo = $"Anulación {venta.NumeroTicket}",
                    ReferenciaId = ventaId,
                    UsuarioId = usuarioId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, ex.Message);
        }
    }

    // ─── Detalle ───────────────────────────────────────────────────────────────

    public async Task<Venta?> GetByIdAsync(int id)
    {
        return await db.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    // ─── Listado ───────────────────────────────────────────────────────────────

    public async Task<VentaIndexViewModel> GetListadoAsync(
        VentaFiltroViewModel filtro, int usuarioId, bool isAdmin)
    {
        var query = db.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.Detalles)
            .AsQueryable();

        // Acceso por rol
        if (!isAdmin)
            query = query.Where(v => v.UsuarioId == usuarioId);
        else if (filtro.UsuarioId.HasValue)
            query = query.Where(v => v.UsuarioId == filtro.UsuarioId);

        // Período
        var (desde, hasta) = ResolvePeriodo(filtro);
        query = query.Where(v => v.CreatedAt >= desde && v.CreatedAt <= hasta);

        // Filtros opcionales
        if (filtro.MetodoPago.HasValue)
            query = query.Where(v => v.MetodoPago == filtro.MetodoPago);

        if (filtro.Estado.HasValue)
            query = query.Where(v => v.Estado == filtro.Estado);

        var total = await query.CountAsync();

        var totalPeriodo = await query
            .Where(v => v.Estado == EstadoVenta.Completada)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;

        var pagina = Math.Max(1, filtro.Pagina);
        var totalPaginas = (int)Math.Ceiling((double)total / VentaFiltroViewModel.ItemsPorPagina);

        var ventas = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((pagina - 1) * VentaFiltroViewModel.ItemsPorPagina)
            .Take(VentaFiltroViewModel.ItemsPorPagina)
            .Select(v => new VentaListItemViewModel
            {
                Id = v.Id,
                NumeroTicket = v.NumeroTicket,
                FechaHora = v.CreatedAt,
                Cajero = v.Usuario != null ? v.Usuario.Nombre : "Sistema",
                Subtotal = v.Subtotal,
                Descuento = v.Descuento,
                Total = v.Total,
                MetodoPago = v.MetodoPago,
                Estado = v.Estado,
                CantidadItems = v.Detalles.Count
            })
            .ToListAsync();

        List<SelectListItem> cajeros = [];
        if (isAdmin)
        {
            cajeros = await db.Usuarios
                .Where(u => u.Activo)
                .OrderBy(u => u.Nombre)
                .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Nombre })
                .ToListAsync();
        }

        filtro.Pagina = pagina;

        return new VentaIndexViewModel
        {
            Ventas = ventas,
            Filtro = filtro,
            TotalPeriodo = totalPeriodo,
            CantidadVentas = total,
            TotalItems = total,
            TotalPaginas = totalPaginas,
            Cajeros = cajeros,
            SoloMisVentas = !isAdmin
        };
    }

    // ─── Exportar CSV ──────────────────────────────────────────────────────────

    public async Task<List<VentaListItemViewModel>> GetParaExportAsync(
        VentaFiltroViewModel filtro, int usuarioId, bool isAdmin)
    {
        var query = db.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.Detalles)
            .AsQueryable();

        if (!isAdmin) query = query.Where(v => v.UsuarioId == usuarioId);

        var (desde, hasta) = ResolvePeriodo(filtro);
        query = query.Where(v => v.CreatedAt >= desde && v.CreatedAt <= hasta);

        return await query
            .OrderByDescending(v => v.CreatedAt)
            .Take(10_000)
            .Select(v => new VentaListItemViewModel
            {
                Id = v.Id,
                NumeroTicket = v.NumeroTicket,
                FechaHora = v.CreatedAt,
                Cajero = v.Usuario != null ? v.Usuario.Nombre : "Sistema",
                Subtotal = v.Subtotal,
                Descuento = v.Descuento,
                Total = v.Total,
                MetodoPago = v.MetodoPago,
                Estado = v.Estado,
                CantidadItems = v.Detalles.Count
            })
            .ToListAsync();
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> GenerarNumeroTicketAsync()
    {
        var prefijo = "T-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-";
        var ultimo = await db.Ventas
            .Where(v => v.NumeroTicket.StartsWith(prefijo))
            .OrderByDescending(v => v.Id)
            .Select(v => v.NumeroTicket)
            .FirstOrDefaultAsync();

        int seq = 1;
        if (ultimo is not null)
        {
            var parts = ultimo.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[^1], out int n))
                seq = n + 1;
        }

        return prefijo + seq.ToString("D4");
    }

    private static (DateTime desde, DateTime hasta) ResolvePeriodo(VentaFiltroViewModel f)
    {
        var hoy = DateTime.UtcNow.Date;
        return f.Periodo switch
        {
            PeriodoFiltro.Hoy =>
                (hoy, hoy.AddDays(1).AddTicks(-1)),
            PeriodoFiltro.Semana =>
                (hoy.AddDays(-(int)hoy.DayOfWeek), hoy.AddDays(7 - (int)hoy.DayOfWeek).AddTicks(-1)),
            PeriodoFiltro.Mes =>
                (new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                 new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddTicks(-1)),
            PeriodoFiltro.Personalizado =>
                (f.FechaDesde.HasValue ? f.FechaDesde.Value.ToUniversalTime() : hoy.AddDays(-30),
                 f.FechaHasta.HasValue ? f.FechaHasta.Value.AddDays(1).ToUniversalTime() : hoy.AddDays(1).AddTicks(-1)),
            _ => (hoy, hoy.AddDays(1).AddTicks(-1))
        };
    }
}
