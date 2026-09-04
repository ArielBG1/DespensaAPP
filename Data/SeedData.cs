using DespensaApp.Data;
using DespensaApp.Models.Entities;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace DespensaApp.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DespensaDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            // Crea la base y aplica las migraciones pendientes (necesario en el primer deploy).
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudieron aplicar las migraciones. Revise la cadena de conexión (DATABASE_URL).");
            return;
        }

        // En producción conviene definir estas contraseñas por variable de entorno.
        var adminPassword = config["ADMIN_PASSWORD"] ?? "Admin1234!";
        var cajeroPassword = config["CAJERO_PASSWORD"] ?? "Cajero1234!";

        if (!await db.Usuarios.AnyAsync())
        {
            db.Usuarios.AddRange(
                new Usuario
                {
                    Nombre = "Administrador",
                    Email = "admin@despensa.com",
                    PasswordHash = BC.HashPassword(adminPassword, 12),
                    Rol = RolUsuario.Admin,
                    Activo = true
                },
                new Usuario
                {
                    Nombre = "Cajero",
                    Email = "cajero@despensa.com",
                    PasswordHash = BC.HashPassword(cajeroPassword, 12),
                    Rol = RolUsuario.Cajero,
                    Activo = true
                }
            );
            logger.LogInformation("Usuarios iniciales creados (admin@despensa.com y cajero@despensa.com).");
        }

        if (!await db.Categorias.AnyAsync())
        {
            db.Categorias.AddRange(
                new Categoria { Nombre = "Lácteos", Descripcion = "Leche, yogurt, quesos" },
                new Categoria { Nombre = "Panificados", Descripcion = "Pan, galletas, cereales" },
                new Categoria { Nombre = "Bebidas", Descripcion = "Aguas, jugos, gaseosas" },
                new Categoria { Nombre = "Limpieza", Descripcion = "Productos de limpieza del hogar" },
                new Categoria { Nombre = "Almacén", Descripcion = "Abarrotes en general" },
                new Categoria { Nombre = "Carnes", Descripcion = "Carnes y embutidos" }
            );
            logger.LogInformation("Categorías iniciales creadas.");
        }

        var clavesFaltantes = new Dictionary<string, (string Valor, string Descripcion)>
        {
            ["MonedaSimbolo"]   = ("Gs.", "Símbolo de moneda"),
            ["MonedaNombre"]    = ("Guaraní", "Nombre de la moneda"),
            ["IVAPorcentaje"]   = ("0", "Porcentaje de IVA"),
            ["PieTicket"]       = ("¡Gracias por su compra!", "Texto al pie del ticket"),
            ["SesionMinutos"]   = ("480", "Duración de sesión en minutos"),
            ["StockMinimoAlerta"] = ("5", "Umbral de alerta de stock bajo"),
        };

        var clavesExistentes = await db.Configuraciones
            .Select(c => c.Clave)
            .ToListAsync();

        foreach (var (clave, (valor, desc)) in clavesFaltantes)
        {
            if (!clavesExistentes.Contains(clave))
            {
                db.Configuraciones.Add(new Configuracion
                {
                    Clave = clave,
                    Valor = valor,
                    Descripcion = desc,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
