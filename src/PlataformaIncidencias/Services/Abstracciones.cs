using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services;

public interface IBuscadorIncidencias
{
    Task<IReadOnlyList<int>> BuscarIdsAsync(string texto, CancellationToken cancellationToken = default);
}

public interface ICacheListado
{
    string NombreOrigen { get; }

    Task<IReadOnlyList<Incidencia>> ObtenerAbiertasAsync(
        Func<CancellationToken, Task<IReadOnlyList<Incidencia>>> fabrica,
        CancellationToken cancellationToken = default);

    Task InvalidarAsync(CancellationToken cancellationToken = default);
}

public interface INotificadorEnTiempoReal
{
    Task PublicarAsync(int id, EstadoIncidencia estado, CancellationToken cancellationToken = default);
}
