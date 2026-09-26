using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Models;

namespace PlataformaIncidencias.Controllers;

[Authorize]
[Route("Operaciones/Incidencias")]
public class IncidenciasController(ApplicationDbContext db) : Controller
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

        return RedirectToAction(nameof(Index));
    }
}
