using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Data;

public static class Semilla
{
    public const string CorreoSupervisor = "supervisor@operaciones.local";
    public const string ClaveSupervisor = "Supervisor123!";

    private static readonly (string Estacion, string Descripcion, PrioridadIncidencia Prioridad, EstadoIncidencia Estado)[]
        Incidencias =
        [
            ("Plaza Norte", "Freno delantero desajustado en 4 bicicletas", PrioridadIncidencia.Alta, EstadoIncidencia.Abierta),
            ("Av. Central", "Rueda pinchada en la bicicleta 128", PrioridadIncidencia.Media, EstadoIncidencia.Abierta),
            ("Parque Sur", "Cadena saltada y con óxido", PrioridadIncidencia.Baja, EstadoIncidencia.Abierta),
            ("Terminal de Buses", "Luces traseras no encienden", PrioridadIncidencia.Media, EstadoIncidencia.Abierta),
            ("Estación Universidad", "Asiento roto en 2 unidades", PrioridadIncidencia.Baja, EstadoIncidencia.Abierta),
            ("Mercado Central", "Anclaje de la estación suelto", PrioridadIncidencia.Critica, EstadoIncidencia.Abierta),
            ("Barrio Histórico", "Pantalla del panel no responde", PrioridadIncidencia.Alta, EstadoIncidencia.Abierta),
            ("Aeropuerto", "Puerto de carga bloqueado", PrioridadIncidencia.Media, EstadoIncidencia.Cerrada),
            ("Puerto", "Candado digital no abre", PrioridadIncidencia.Alta, EstadoIncidencia.Abierta),
        ];

    public static async Task InicializarAsync(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();

        var db = alcance.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        if (!await db.Incidencias.AnyAsync())
        {
            var ahora = DateTime.UtcNow;
            db.Incidencias.AddRange(Incidencias.Select((dato, indice) => new Incidencia
            {
                Estacion = dato.Estacion,
                Descripcion = dato.Descripcion,
                Prioridad = dato.Prioridad,
                Estado = dato.Estado,
                Fecha = ahora.AddMinutes(-indice * 17),
                FechaCierre = dato.Estado == EstadoIncidencia.Cerrada ? ahora.AddMinutes(-5) : null
            }));
            await db.SaveChangesAsync();
        }

        var gestorUsuarios = alcance.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        if (await gestorUsuarios.FindByEmailAsync(CorreoSupervisor) is null)
        {
            await gestorUsuarios.CreateAsync(
                new IdentityUser { UserName = CorreoSupervisor, Email = CorreoSupervisor, EmailConfirmed = true },
                ClaveSupervisor);
        }
    }
}
