using System.Text.Json.Serialization;

namespace FrontendBlazorApi.Models
{
    public class Proyecto
{
    public int Id { get; set; }
    public int? IdProyectoPadre { get; set; }  // Nullable, ya que puede ser NULL
    public int IdResponsable { get; set; }
    public int IdTipoProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFinPrevista { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
    public string RutaLogo { get; set; } = string.Empty;
}
}