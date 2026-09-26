using System.Text.Json;
using PlataformaIncidencias.Models;
using StackExchange.Redis;

namespace PlataformaIncidencias.Services.Real;

public sealed class CacheRedis(IConnectionMultiplexer redis, ILogger<CacheRedis> logger) : ICacheListado
{
    private const string ClaveListadoAbiertas = "incidencias:abiertas:v1";
    private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(60);

    public string NombreOrigen => "Redis";

    public async Task<IReadOnlyList<Incidencia>> ObtenerAbiertasAsync(
        Func<CancellationToken, Task<IReadOnlyList<Incidencia>>> fabrica,
        CancellationToken cancellationToken = default)
    {
        var baseDeDatos = redis.GetDatabase();
        var guardado = await baseDeDatos.StringGetAsync(ClaveListadoAbiertas);
        var texto = (string?)guardado;

        if (!string.IsNullOrEmpty(texto))
        {
            logger.LogInformation("Listado de incidencias abiertas leído desde {Origen}", NombreOrigen);
            return JsonSerializer.Deserialize<List<Incidencia>>(texto) ?? [];
        }

        var listado = await fabrica(cancellationToken);
        await baseDeDatos.StringSetAsync(ClaveListadoAbiertas, JsonSerializer.Serialize(listado), Duracion);
        logger.LogInformation("Listado de incidencias abiertas leído desde {Origen}", "Base de datos");
        return listado;
    }

    public async Task InvalidarAsync(CancellationToken cancellationToken = default)
    {
        await redis.GetDatabase().KeyDeleteAsync(ClaveListadoAbiertas);
        logger.LogInformation("Caché del listado invalidada ({Clave})", ClaveListadoAbiertas);
    }
}
