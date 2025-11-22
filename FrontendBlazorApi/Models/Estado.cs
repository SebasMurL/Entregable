using System.Text.Json.Serialization;

namespace FrontendBlazorApi.Models
{
    public class Estado
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}