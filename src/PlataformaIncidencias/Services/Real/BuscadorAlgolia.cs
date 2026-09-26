using System.Text;
using System.Text.Json;

namespace PlataformaIncidencias.Services.Real;

public sealed class BuscadorAlgolia(HttpClient http, IConfiguration configuracion, ILogger<BuscadorAlgolia> logger)
    : IBuscadorIncidencias
{
    private readonly string _indice = configuracion["Algolia:Indice"] ?? "incidencias";

    public async Task<IReadOnlyList<int>> BuscarIdsAsync(string texto, CancellationToken cancellationToken = default)
    {
        var termino = texto.Trim();
        if (termino.Length == 0)
        {
            return Array.Empty<int>();
        }

        var cuerpo = JsonSerializer.Serialize(new { query = termino, hitsPerPage = 100 });
        using var contenido = new StringContent(cuerpo, Encoding.UTF8, "application/json");
        using var respuesta = await http.PostAsync($"1/indexes/{_indice}/query", contenido, cancellationToken);

        respuesta.EnsureSuccessStatusCode();

        await using var flujo = await respuesta.Content.ReadAsStreamAsync(cancellationToken);
        using var documento = await JsonDocument.ParseAsync(flujo, cancellationToken: cancellationToken);

        var ids = new List<int>();
        foreach (var golpe in documento.RootElement.GetProperty("hits").EnumerateArray())
        {
            if (golpe.TryGetProperty("objectID", out var objectId) &&
                int.TryParse(objectId.GetString(), out var id))
            {
                ids.Add(id);
            }
        }

        logger.LogInformation("Algolia devolvió {Cantidad} coincidencias para \"{Texto}\"", ids.Count, termino);
        return ids;
    }
}
