using DespensaApp.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DespensaApp.Data;

public class DespensaDbContext(DbContextOptions<DespensaDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<CodigoBarraAlternativo> CodigosBarraAlternativos => Set<CodigoBarraAlternativo>();
    public DbSet<ProductoImagen> ProductoImagenes => Set<ProductoImagen>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<DetalleCompra> DetallesCompra => Set<DetalleCompra>();
    public DbSet<Configuracion> Configuraciones => Set<Configuracion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<RolUsuario>();
        modelBuilder.HasPostgresEnum<TipoVenta>();
        modelBuilder.HasPostgresEnum<MetodoPago>();
        modelBuilder.HasPostgresEnum<EstadoVenta>();
        modelBuilder.HasPostgresEnum<TipoMovimiento>();
        modelBuilder.HasPostgresEnum<EstadoCompra>();

        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Rol).HasConversion<string>();
        });

        modelBuilder.Entity<Producto>(e =>
        {
            e.HasIndex(p => p.CodigoBarra)
                .IsUnique()
                .HasFilter("codigo_barra IS NOT NULL");
            e.Property(p => p.TipoVenta).HasConversion<string>();
        });

        modelBuilder.Entity<CodigoBarraAlternativo>(e =>
        {
            e.HasIndex(c => c.Codigo).IsUnique();
            e.HasOne(c => c.Producto)
                .WithMany(p => p.CodigosAlternativos)
                .HasForeignKey(c => c.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductoImagen>(e =>
        {
            e.HasOne(i => i.Producto)
                .WithOne(p => p.Imagen)
                .HasForeignKey<ProductoImagen>(i => i.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Venta>(e =>
        {
            e.HasIndex(v => v.NumeroTicket).IsUnique();
            e.Property(v => v.MetodoPago).HasConversion<string>();
            e.Property(v => v.Estado).HasConversion<string>();
        });

        modelBuilder.Entity<MovimientoStock>(e =>
        {
            e.Property(m => m.Tipo).HasConversion<string>();
        });

        modelBuilder.Entity<Compra>(e =>
        {
            e.Property(c => c.Estado).HasConversion<string>();
        });

        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Configuracion>(e =>
        {
            e.HasData(
                new Configuracion { Clave = "NombreNegocio", Valor = "Despensa App", Descripcion = "Nombre del negocio", UpdatedAt = seedDate },
                new Configuracion { Clave = "Direccion", Valor = "", Descripcion = "Dirección del negocio", UpdatedAt = seedDate },
                new Configuracion { Clave = "Telefono", Valor = "", Descripcion = "Teléfono de contacto", UpdatedAt = seedDate },
                new Configuracion { Clave = "MonedaSimbolo", Valor = "Gs.", Descripcion = "Símbolo de moneda", UpdatedAt = seedDate },
                new Configuracion { Clave = "MonedaNombre", Valor = "Guaraní", Descripcion = "Nombre de la moneda", UpdatedAt = seedDate }
            );
        });
    }
}
