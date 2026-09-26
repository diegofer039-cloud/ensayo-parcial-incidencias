using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;

namespace PlataformaIncidencias.Services.Local;

public sealed class BuscadorLocal(ApplicationDbContext db) : IBuscadorIncidencias
{
    public async Task<IReadOnlyList<int>> BuscarIdsAsync(string texto, CancellationToken cancellationToken = default)
    {
        var termino = texto.Trim();
        if (termino.Length == 0)
        {
            return Array.Empty<int>();
        }

        var ids = await db.Incidencias
            .Where(i => i.Estacion.Contains(termino) || i.Descripcion.Contains(termino))
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return ids;
    }
}
