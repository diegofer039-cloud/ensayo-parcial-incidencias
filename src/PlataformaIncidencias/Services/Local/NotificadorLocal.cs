using Microsoft.AspNetCore.SignalR;
using PlataformaIncidencias.Hubs;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Services.Local;

public sealed class NotificadorLocal(IHubContext<ActualizacionHub> hub, ILogger<NotificadorLocal> logger)
    : INotificadorEnTiempoReal
{
    public async Task PublicarAsync(int id, EstadoIncidencia estado, CancellationToken cancellationToken = default)
    {
        await hub.Clients.All.SendAsync(
            "IncidenciaActualizada",
            new { Id = id, Estado = estado.ToString() },
            cancellationToken);

        logger.LogInformation(
            "Evento IncidenciaActualizada publicado (Id={Id}, Estado={Estado}) por canal local",
            id,
            estado);
    }
}
