using Microsoft.Extensions.Caching.Memory;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services.Local;

public sealed class CacheLocal(IMemoryCache cache, ILogger<CacheLocal> logger) : ICacheListado
{
    private const string ClaveListadoAbiertas = "incidencias:abiertas:v1";
    private static readonly TimeSpan Duracion = TimeSpan.FromSeconds(60);

    public string NombreOrigen => "Caché en memoria";

    public async Task<IReadOnlyList<Incidencia>> ObtenerAbiertasAsync(
        Func<CancellationToken, Task<IReadOnlyList<Incidencia>>> fabrica,
        CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(ClaveListadoAbiertas, out IReadOnlyList<Incidencia>? guardado) && guardado is not null)
        {
            logger.LogInformation("Listado de incidencias abiertas leído desde {Origen}", NombreOrigen);
            return guardado;
        }

        var listado = await fabrica(cancellationToken);
        cache.Set(ClaveListadoAbiertas, listado, Duracion);
        logger.LogInformation("Listado de incidencias abiertas leído desde {Origen}", "Base de datos");
        return listado;
    }

    public Task InvalidarAsync(CancellationToken cancellationToken = default)
    {
        cache.Remove(ClaveListadoAbiertas);
        logger.LogInformation("Caché del listado invalidada ({Clave})", ClaveListadoAbiertas);
        return Task.CompletedTask;
    }
}
