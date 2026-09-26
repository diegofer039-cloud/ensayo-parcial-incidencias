using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;
using PlataformaIncidencias.Services;

namespace PlataformaIncidencias.Controllers;

[Authorize]
[Route("Operaciones/Incidencias")]
public class IncidenciasController(ApplicationDbContext db, INotificadorEnTiempoReal notificador, ILogger<IncidenciasController> registro)
    : Controller
{
    private async Task<IReadOnlyList<Incidencia>> ListarAbiertasAsync(CancellationToken cancellationToken) =>
        await db.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .OrderBy(i => i.Fecha)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var listado = await ListarAbiertasAsync(cancellationToken);
        return View(listado);
    }

    [HttpGet("datos")]
    public async Task<IActionResult> Datos(CancellationToken cancellationToken)
    {
        var listado = await ListarAbiertasAsync(cancellationToken);

        return Json(listado.Select(i => new
        {
            i.Id,
            i.Estacion,
            i.Descripcion,
            Prioridad = i.Prioridad.ToString(),
            Estado = i.Estado.ToString(),
            Fecha = i.Fecha
        }));
    }

    [HttpPost("cerrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id, CancellationToken cancellationToken)
    {
        var incidencia = await db.Incidencias.FindAsync([id], cancellationToken);
        if (incidencia is null)
        {
            return NotFound();
        }

        incidencia.Estado = EstadoIncidencia.Cerrada;
        incidencia.FechaCierre = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await notificador.PublicarAsync(incidencia.Id, incidencia.Estado, cancellationToken);
        }
        catch (Exception excepcion)
        {
            registro.LogError(
                excepcion,
                "No se pudo publicar IncidenciaActualizada (Id={Id}) tras persistir el cierre",
                id);
        }

        return RedirectToAction(nameof(Index));
    }
}
