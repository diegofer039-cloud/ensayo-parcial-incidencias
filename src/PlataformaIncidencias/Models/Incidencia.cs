using System.ComponentModel.DataAnnotations;

namespace PlataformaIncidencias.Models;

public enum EstadoIncidencia
{
    Abierta = 0,
    Cerrada = 1
}

public enum PrioridadIncidencia
{
    Baja = 0,
    Media = 1,
    Alta = 2,
    Critica = 3
}

public class Incidencia
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Estacion { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    public PrioridadIncidencia Prioridad { get; set; } = PrioridadIncidencia.Media;

    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;

    public DateTime Fecha { get; set; }

    public DateTime? FechaCierre { get; set; }
}
