using System.Text;
using System.Text.Json;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services.Real;

public sealed class NotificadorPieHost(HttpClient http, IConfiguration configuracion, ILogger<NotificadorPieHost> logger)
    : INotificadorEnTiempoReal
{
    private readonly string _clave = configuracion["PieHost:Key"]
        ?? throw new InvalidOperationException("Falta la variable de entorno PieHost:Key.");

    private readonly string _secreto = configuracion["PieHost:Secret"]
        ?? throw new InvalidOperationException("Falta la variable de entorno PieHost:Secret.");

    private readonly string _canal = configuracion["PieHost:Channel"] ?? "incidencias";

    public async Task PublicarAsync(int id, EstadoIncidencia estado, CancellationToken cancellationToken = default)
    {
        var cuerpo = JsonSerializer.Serialize(new
        {
            key = _clave,
            secret = _secreto,
            roomId = _canal,
            message = new
            {
                @event = "IncidenciaActualizada",
                data = new { Id = id, Estado = estado.ToString() }
            }
        });

        using var contenido = new StringContent(cuerpo, Encoding.UTF8, "application/json");
        using var respuesta = await http.PostAsync("api/publish", contenido, cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "PieHost rechazó la publicación de IncidenciaActualizada (Id={Id}): {Status} {Detalle}",
                id,
                (int)respuesta.StatusCode,
                detalle);
            return;
        }

        logger.LogInformation(
            "Evento IncidenciaActualizada publicado en PieHost (Id={Id}, Estado={Estado}, Canal={Canal})",
            id,
            estado,
            _canal);
    }
}
