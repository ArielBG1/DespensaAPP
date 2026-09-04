using DespensaApp.Data;
using DespensaApp.Models.Entities;
using DespensaApp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DespensaApp.Services;

public class ProductoService(DespensaDbContext db) : IProductoService
{
    private static readonly string[] ImagenesPermitidas = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImagenBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<ProductoIndexViewModel> GetListadoAsync(ProductoFiltroViewModel filtro)
    {
        var query = db.Productos
            .Include(p => p.Categoria)
            .AsQueryable();

        if (filtro.CategoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == filtro.CategoriaId);

        if (filtro.TipoVenta.HasValue)
            query = query.Where(p => p.TipoVenta == filtro.TipoVenta);

        if (filtro.Activo.HasValue)
            query = query.Where(p => p.Activo == filtro.Activo);
        else
            query = query.Where(p => p.Activo); // por defecto solo activos

        if (filtro.SoloStockBajo)
            query = query.Where(p => p.StockActual <= p.StockMinimo);

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var busq = filtro.Busqueda.ToLower().Trim();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(busq) ||
                (p.CodigoBarra != null && p.CodigoBarra.Contains(busq)) ||
                p.CodigosAlternativos.Any(c => c.Codigo.Contains(busq)));
        }

        var total = await query.CountAsync();
        var pagina = Math.Max(1, filtro.Pagina);
        var totalPaginas = (int)Math.Ceiling((double)total / ProductoFiltroViewModel.ItemsPorPagina);

        var productos = await query
            .OrderBy(p => p.Nombre)
            .Skip((pagina - 1) * ProductoFiltroViewModel.ItemsPorPagina)
            .Take(ProductoFiltroViewModel.ItemsPorPagina)
            .Select(p => new ProductoListItemViewModel
            {
                Id = p.Id,
                CodigoBarra = p.CodigoBarra,
                Nombre = p.Nombre,
                Categoria = p.Categoria != null ? p.Categoria.Nombre : null,
                TipoVenta = p.TipoVenta,
                PrecioVenta = p.PrecioVenta,
                StockActual = p.StockActual,
                StockMinimo = p.StockMinimo,
                Activo = p.Activo,
                ImagenUrl = p.ImagenUrl,
                FechaVencimiento = p.FechaVencimiento
            })
            .ToListAsync();

        var categorias = await db.Categorias
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Nombre })
            .ToListAsync();

        filtro.Pagina = pagina;

        return new ProductoIndexViewModel
        {
            Productos = productos,
            Filtro = filtro,
            TotalItems = total,
            TotalPaginas = totalPaginas,
            Categorias = categorias
        };
    }

    public async Task<Producto?> GetByIdAsync(int id)
    {
        return await db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Proveedor)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // ── FORMULARIO (alta y edición comparten la misma carga) ───────

    public async Task<ProductoFormViewModel> GetFormViewModelAsync(int? id = null)
    {
        var vm = id.HasValue
            ? await CargarParaEdicionAsync(id.Value)
            : new ProductoFormViewModel { Activo = true };

        await PopulateDropdownsAsync(vm);
        return vm;
    }

    private async Task<ProductoFormViewModel> CargarParaEdicionAsync(int id)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null) return new ProductoFormViewModel();

        var codigosAlternativos = await db.CodigosBarraAlternativos
            .Where(c => c.ProductoId == id)
            .OrderBy(c => c.Id)
            .Select(c => c.Codigo)
            .ToListAsync();

        return new ProductoFormViewModel
        {
            Id = producto.Id,
            CodigoBarra = producto.CodigoBarra,
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            CategoriaId = producto.CategoriaId,
            ProveedorId = producto.ProveedorId,
            TipoVenta = producto.TipoVenta,
            PrecioCompra = producto.PrecioCompra,
            PrecioVenta = producto.PrecioVenta,
            StockActual = producto.StockActual,
            StockMinimo = producto.StockMinimo,
            ImagenUrl = producto.ImagenUrl,
            FechaVencimiento = producto.FechaVencimiento,
            Activo = producto.Activo,
            CodigosAlternativos = codigosAlternativos
        };
    }

    public async Task PopulateDropdownsAsync(ProductoFormViewModel vm)
    {
        vm.CategoriasItems = await db.Categorias
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Nombre })
            .ToListAsync();

        vm.ProveedoresItems = await db.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Nombre })
            .ToListAsync();
    }

    // ── ALTA DE PRODUCTO ─────────────────────────────────────────

    public async Task<(bool success, string? error)> CreateAsync(ProductoFormViewModel vm, int usuarioId)
    {
        var codigoBarra = string.IsNullOrWhiteSpace(vm.CodigoBarra) ? null : vm.CodigoBarra.Trim();

        if (codigoBarra is not null)
        {
            var yaExiste = await db.Productos.AnyAsync(p => p.CodigoBarra == codigoBarra);
            if (yaExiste) return (false, "Ya existe un producto con ese código de barras.");
        }

        var (codigosAlternativos, errorCodigos) = await ValidarCodigosAlternativosAsync(
            vm.CodigosAlternativos, codigoBarra, idActualProducto: 0);
        if (errorCodigos is not null) return (false, errorCodigos);

        ImagenSubida? imagenNueva = null;
        if (vm.ImagenFile is not null)
        {
            var (subida, errorImagen) = await LeerImagenAsync(vm.ImagenFile);
            if (errorImagen is not null) return (false, errorImagen);
            imagenNueva = subida;
        }

        var producto = new Producto
        {
            CodigoBarra = codigoBarra,
            Nombre = vm.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(vm.Descripcion) ? null : vm.Descripcion.Trim(),
            CategoriaId = vm.CategoriaId,
            ProveedorId = vm.ProveedorId,
            TipoVenta = vm.TipoVenta,
            PrecioCompra = vm.PrecioCompra,
            PrecioVenta = vm.PrecioVenta,
            StockActual = vm.StockActual,
            StockMinimo = vm.StockMinimo,
            FechaVencimiento = vm.FechaVencimiento,
            Activo = vm.Activo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            db.Productos.Add(producto);
            await db.SaveChangesAsync();

            // La imagen se guarda despues del primer SaveChanges porque necesita el Id del producto.
            if (imagenNueva is not null)
                producto.ImagenUrl = await GuardarImagenAsync(producto.Id, imagenNueva.Value);

            foreach (var codigo in codigosAlternativos)
            {
                db.CodigosBarraAlternativos.Add(new CodigoBarraAlternativo
                {
                    ProductoId = producto.Id,
                    Codigo = codigo,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (vm.StockActual > 0)
            {
                db.MovimientosStock.Add(new MovimientoStock
                {
                    ProductoId = producto.Id,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = vm.StockActual,
                    StockAntes = 0,
                    StockDespues = vm.StockActual,
                    Motivo = "Stock inicial",
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
            return (false, $"Error al guardar el producto: {ex.Message}");
        }
    }

    // ── EDICIÓN (fuera de alcance del pedido, se mantiene igual) ───

    public async Task<(bool success, string? error)> UpdateAsync(ProductoFormViewModel vm, int usuarioId)
    {
        var producto = await db.Productos.FindAsync(vm.Id);
        if (producto is null) return (false, "Producto no encontrado.");

        var codigoBarra = string.IsNullOrWhiteSpace(vm.CodigoBarra) ? null : vm.CodigoBarra.Trim();

        if (codigoBarra is not null)
        {
            var duplicado = await db.Productos.AnyAsync(p => p.CodigoBarra == codigoBarra && p.Id != vm.Id);
            if (duplicado) return (false, "Ya existe otro producto con ese código de barras.");
        }

        var (codigosAlternativos, errorCodigos) = await ValidarCodigosAlternativosAsync(
            vm.CodigosAlternativos, codigoBarra, idActualProducto: vm.Id);
        if (errorCodigos is not null) return (false, errorCodigos);

        if (vm.ImagenFile is not null)
        {
            var (subida, errorImagen) = await LeerImagenAsync(vm.ImagenFile);
            if (errorImagen is not null) return (false, errorImagen);

            producto.ImagenUrl = await GuardarImagenAsync(producto.Id, subida!.Value);
        }

        producto.CodigoBarra = codigoBarra;
        producto.Nombre = vm.Nombre.Trim();
        producto.Descripcion = string.IsNullOrWhiteSpace(vm.Descripcion) ? null : vm.Descripcion.Trim();
        producto.CategoriaId = vm.CategoriaId;
        producto.ProveedorId = vm.ProveedorId;
        producto.TipoVenta = vm.TipoVenta;
        producto.PrecioCompra = vm.PrecioCompra;
        producto.PrecioVenta = vm.PrecioVenta;
        producto.StockMinimo = vm.StockMinimo;
        producto.FechaVencimiento = vm.FechaVencimiento;
        producto.Activo = vm.Activo;
        producto.UpdatedAt = DateTime.UtcNow;

        try
        {
            var existentes = await db.CodigosBarraAlternativos
                .Where(c => c.ProductoId == vm.Id)
                .ToListAsync();
            db.CodigosBarraAlternativos.RemoveRange(existentes);

            foreach (var codigo in codigosAlternativos)
            {
                db.CodigosBarraAlternativos.Add(new CodigoBarraAlternativo
                {
                    ProductoId = vm.Id,
                    Codigo = codigo,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar: {ex.Message}");
        }
    }

    public async Task<bool> DeleteLogicalAsync(int id)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null) return false;

        producto.Activo = false;
        producto.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool success, string? error)> AjustarStockAsync(AjusteStockViewModel vm, int usuarioId)
    {
        decimal delta;
        switch (vm.Tipo)
        {
            case TipoMovimiento.Entrada:
            case TipoMovimiento.AjustePositivo:
                delta = vm.Cantidad;
                break;
            case TipoMovimiento.Salida:
            case TipoMovimiento.AjusteNegativo:
                delta = -vm.Cantidad;
                break;
            default:
                return (false, "Tipo de movimiento inválido.");
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var (resultado, nuevoStock) = await StockOperations.AplicarDeltaAsync(db, vm.ProductoId, delta);

            if (resultado == StockOperations.ResultadoStock.NoEncontrado)
            {
                await tx.RollbackAsync();
                return (false, "Producto no encontrado.");
            }
            if (resultado == StockOperations.ResultadoStock.Insuficiente)
            {
                var stockActual = await db.Productos
                    .Where(p => p.Id == vm.ProductoId)
                    .Select(p => p.StockActual)
                    .FirstOrDefaultAsync();
                await tx.RollbackAsync();
                return (false, $"Stock insuficiente. Stock actual: {stockActual}");
            }

            db.MovimientosStock.Add(new MovimientoStock
            {
                ProductoId = vm.ProductoId,
                Tipo = vm.Tipo,
                Cantidad = vm.Cantidad,
                StockAntes = nuevoStock - delta,
                StockDespues = nuevoStock,
                Motivo = vm.Motivo.Trim(),
                UsuarioId = usuarioId,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, $"Error al ajustar stock: {ex.Message}");
        }
    }

    public async Task<MovimientoFiltroViewModel?> GetMovimientosAsync(int productoId, DateTime? desde, DateTime? hasta)
    {
        var producto = await db.Productos.FindAsync(productoId);
        if (producto is null) return null;

        var query = db.MovimientosStock
            .Include(m => m.Usuario)
            .Where(m => m.ProductoId == productoId);

        if (desde.HasValue)
            query = query.Where(m => m.CreatedAt >= desde.Value.ToUniversalTime());
        if (hasta.HasValue)
            query = query.Where(m => m.CreatedAt <= hasta.Value.AddDays(1).ToUniversalTime());

        var total = await query.CountAsync();

        var movimientos = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(200)
            .Select(m => new MovimientoListItemViewModel
            {
                Id = m.Id,
                Tipo = m.Tipo,
                Cantidad = m.Cantidad,
                StockAntes = m.StockAntes,
                StockDespues = m.StockDespues,
                Motivo = m.Motivo,
                UsuarioNombre = m.Usuario != null ? m.Usuario.Nombre : "Sistema",
                FechaHora = m.CreatedAt
            })
            .ToListAsync();

        return new MovimientoFiltroViewModel
        {
            ProductoId = productoId,
            NombreProducto = producto.Nombre,
            CodigoBarra = producto.CodigoBarra,
            StockActual = producto.StockActual,
            TipoVentaProducto = producto.TipoVenta,
            FechaDesde = desde,
            FechaHasta = hasta,
            Movimientos = movimientos,
            TotalMovimientos = total
        };
    }

    public async Task<EtiquetasViewModel> GetProductosParaEtiquetasAsync(int[] ids)
    {
        var productos = await db.Productos
            .Where(p => ids.Contains(p.Id))
            .OrderBy(p => p.Nombre)
            .Select(p => new EtiquetaItemViewModel
            {
                Id = p.Id,
                Nombre = p.Nombre,
                CodigoBarra = p.CodigoBarra
            })
            .ToListAsync();

        return new EtiquetasViewModel { Productos = productos };
    }

    // ── CÓDIGOS DE BARRA: verificación, alternativos y generación ──

    public async Task<object> CheckCodigoBarraAsync(string codigo, int idActual)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return new { existe = false };

        var comoPrincipal = await db.Productos
            .Where(p => p.CodigoBarra == codigo && p.Id != idActual)
            .Select(p => new { p.Id, p.Nombre, p.Activo })
            .FirstOrDefaultAsync();

        if (comoPrincipal is not null)
            return new { existe = true, comoPrincipal.Id, comoPrincipal.Nombre, comoPrincipal.Activo };

        var comoAlternativo = await db.CodigosBarraAlternativos
            .Where(c => c.Codigo == codigo && c.ProductoId != idActual)
            .Select(c => new { c.Producto!.Id, c.Producto.Nombre, c.Producto.Activo })
            .FirstOrDefaultAsync();

        if (comoAlternativo is not null)
            return new { existe = true, comoAlternativo.Id, comoAlternativo.Nombre, comoAlternativo.Activo };

        return new { existe = false };
    }

    private async Task<(List<string> codigos, string? error)> ValidarCodigosAlternativosAsync(
        List<string> entrada, string? codigoPrincipal, int idActualProducto)
    {
        var normalizados = entrada
            .Select(c => c?.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!)
            .Distinct()
            .ToList();

        if (codigoPrincipal is not null && normalizados.Contains(codigoPrincipal))
            return ([], "Un código alternativo no puede ser igual al código de barras principal.");

        foreach (var codigo in normalizados)
        {
            var existeComoPrincipal = await db.Productos
                .AnyAsync(p => p.CodigoBarra == codigo && p.Id != idActualProducto);
            if (existeComoPrincipal)
                return ([], $"El código «{codigo}» ya es el código principal de otro producto.");

            var existeComoAlterno = await db.CodigosBarraAlternativos
                .AnyAsync(c => c.Codigo == codigo && c.ProductoId != idActualProducto);
            if (existeComoAlterno)
                return ([], $"El código «{codigo}» ya está asignado como alterno de otro producto.");
        }

        return (normalizados, null);
    }

    // Rango 200000000000–299999999999 (prefijo "20") está reservado por GS1 para uso
    // interno/restringido de cada comercio: nunca es asignado a un producto de fábrica,
    // por lo que un código generado acá jamás puede chocar con el de una Oreo, un atún, etc.
    private const string PrefijoInterno = "20";

    public async Task<string> GenerarCodigoBarraInternoAsync()
    {
        for (var intento = 0; intento < 20; intento++)
        {
            var cuerpo = string.Concat(Enumerable.Range(0, 10).Select(_ => Random.Shared.Next(0, 10)));
            var doceDigitos = PrefijoInterno + cuerpo;
            var candidato = doceDigitos + CalcularDigitoVerificadorEan13(doceDigitos);

            var existe = await db.Productos.AnyAsync(p => p.CodigoBarra == candidato)
                || await db.CodigosBarraAlternativos.AnyAsync(c => c.Codigo == candidato);
            if (!existe) return candidato;
        }

        throw new InvalidOperationException("No se pudo generar un código de barras único. Intente nuevamente.");
    }

    private static int CalcularDigitoVerificadorEan13(string doceDigitos)
    {
        var suma = 0;
        for (var i = 0; i < 12; i++)
        {
            var d = doceDigitos[i] - '0';
            suma += (i % 2 == 0) ? d : d * 3;
        }
        return (10 - (suma % 10)) % 10;
    }

    // ── IMAGEN ───────────────────────────────────────────────────

    private readonly record struct ImagenSubida(byte[] Datos, string ContentType);

    /// <summary>Valida el archivo subido y lo lee a memoria.</summary>
    private static async Task<(ImagenSubida? imagen, string? error)> LeerImagenAsync(IFormFile archivo)
    {
        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ImagenesPermitidas.Contains(ext))
            return (null, $"Formato no permitido. Use: {string.Join(", ", ImagenesPermitidas)}");
        if (archivo.Length > MaxImagenBytes)
            return (null, "La imagen no puede superar los 5 MB.");

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);

        return (new ImagenSubida(memoria.ToArray(), ContentTypePara(ext)), null);
    }

    /// <summary>
    /// Guarda o reemplaza la imagen del producto en la base y devuelve la URL para mostrarla.
    /// No hace SaveChanges: lo hace quien llama, dentro de su propia transacción.
    /// </summary>
    private async Task<string> GuardarImagenAsync(int productoId, ImagenSubida imagen)
    {
        var fila = await db.ProductoImagenes.FindAsync(productoId);
        if (fila is null)
        {
            fila = new ProductoImagen { ProductoId = productoId };
            db.ProductoImagenes.Add(fila);
        }

        fila.Datos = imagen.Datos;
        fila.ContentType = imagen.ContentType;
        fila.UpdatedAt = DateTime.UtcNow;

        // El sufijo de versión evita que el navegador siga mostrando la foto anterior.
        return $"/Productos/Imagen/{productoId}?v={fila.UpdatedAt.Ticks}";
    }

    public async Task<(byte[] datos, string contentType)?> GetImagenAsync(int productoId)
    {
        var imagen = await db.ProductoImagenes
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ProductoId == productoId);

        return imagen is null ? null : (imagen.Datos, imagen.ContentType);
    }

    private static string ContentTypePara(string extension) => extension switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };
}
