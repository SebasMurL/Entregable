using System.Text.Json.Serialization;

namespace FrontendBlazorApi.Models
{
    public class Actividad
    {
        public int Id { get; set; }
        public int IdEntregable { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinPrevista { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public int? Prioridad { get; set; }
        public int? PorcentajeAvance { get; set; }
    }
}