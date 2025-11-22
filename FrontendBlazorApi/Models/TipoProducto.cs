using System.Text.Json.Serialization;

namespace FrontendBlazorApi.Models
{
    public class TipoProducto
    {
        public int Id { get; set; }                         // ID para Identity Server
        public string Nombre { get; set; } = string.Empty;   // Nombre del tipo de producto
        public string Descripcion { get; set; } = string.Empty; // Descripción del tipo de producto
    }

}