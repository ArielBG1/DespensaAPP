using DespensaApp.Data;
using DespensaApp.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DespensaApp.Services;

public class ConfiguracionService(DespensaDbContext db, IMemoryCache cache) : IConfiguracionService
{
    private const string CacheKey = "cfg_all";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private async Task<Dictionary<string, string>> GetCachedAsync()
    {
        if (!cache.TryGetValue(CacheKey, out Dictionary<string, string>? data))
        {
            data = await db.Configuraciones.ToDictionaryAsync(c => c.Clave, c => c.Valor);
            cache.Set(CacheKey, data, Ttl);
        }
        return data!;
    }

    public async Task<string> ObtenerValorAsync(string clave, string valorPorDefecto = "")
    {
        var all = await GetCachedAsync();
        return all.TryGetValue(clave, out var v) ? v : valorPorDefecto;
    }

    public async Task<Dictionary<string, string>> ObtenerTodasAsync()
        => await GetCachedAsync();

    public async Task SetValorAsync(string clave, string valor)
    {
        var c = await db.Configuraciones.FindAsync(clave);
        if (c is null)
            db.Configuraciones.Add(new Configuracion { Clave = clave, Valor = valor, UpdatedAt = DateTime.UtcNow });
        else
        {
            c.Valor = valor;
            c.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        cache.Remove(CacheKey);
    }

    public async Task SetVariosAsync(Dictionary<string, string> pares)
    {
        var claves = pares.Keys.ToList();
        var existentes = await db.Configuraciones
            .Where(c => claves.Contains(c.Clave))
            .ToListAsync();

        foreach (var (clave, valor) in pares)
        {
            var c = existentes.FirstOrDefault(x => x.Clave == clave);
            if (c is null)
                db.Configuraciones.Add(new Configuracion { Clave = clave, Valor = valor, UpdatedAt = DateTime.UtcNow });
            else
            {
                c.Valor = valor;
                c.UpdatedAt = DateTime.UtcNow;
            }
        }
        await db.SaveChangesAsync();
        cache.Remove(CacheKey);
    }
}
