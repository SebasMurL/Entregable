using System.Text.Json.Serialization;

namespace FrontendBlazorApi.Models
{
    public class Archivo
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Ruta { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Tipo { get; set; }
        public DateTime? Fecha { get; set; }
    }
}