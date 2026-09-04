using DespensaApp.Data;
using Microsoft.EntityFrameworkCore;

namespace DespensaApp.Services;

/// <summary>
/// Aplica cambios de stock como UPDATE condicional atómico en la base de datos
/// (en vez de "leer stock -> calcular en memoria -> guardar"), para que dos
/// operaciones concurrentes sobre el mismo producto (ventas, compras, ajustes)
/// no puedan pisarse y dejar el stock mal calculado o vender de más.
/// </summary>
public static class StockOperations
{
    public enum ResultadoStock { Ok, NoEncontrado, Insuficiente }

    /// <summary>
    /// Suma <paramref name="delta"/> (puede ser negativo) al stock del producto.
    /// Si delta es negativo, el UPDATE solo aplica cuando el stock alcanza —el chequeo
    /// y la resta ocurren en la misma sentencia SQL, sin ventana para una carrera—.
    /// </summary>
    public static async Task<(ResultadoStock resultado, decimal stockResultante)> AplicarDeltaAsync(
        DespensaDbContext db, int productoId, decimal delta)
    {
        var ahora = DateTime.UtcNow;

        var query = db.Productos.Where(p => p.Id == productoId);
        if (delta < 0)
            query = query.Where(p => p.StockActual >= -delta);

        var filas = await query.ExecuteUpdateAsync(s => s
            .SetProperty(p => p.StockActual, p => p.StockActual + delta)
            .SetProperty(p => p.UpdatedAt, ahora));

        if (filas > 0)
        {
            var stock = await db.Productos
                .Where(p => p.Id == productoId)
                .Select(p => p.StockActual)
                .FirstAsync();
            return (ResultadoStock.Ok, stock);
        }

        // 0 filas afectadas: o el producto no existe, o (con delta negativo) no había stock suficiente.
        var existe = await db.Productos.AnyAsync(p => p.Id == productoId);
        return existe ? (ResultadoStock.Insuficiente, 0) : (ResultadoStock.NoEncontrado, 0);
    }
}
