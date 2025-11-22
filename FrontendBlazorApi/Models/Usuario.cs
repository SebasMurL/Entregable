using System.Text.Json.Serialization;
namespace FrontendBlazorApi.Models
{
    // Modelo que representa una Persona tal como lo devuelve la API
    public class Usuario
    {
         public int Id { get; set; }
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("Contrasena")] 
        public string Contrasena { get; set; } = string.Empty;
        
        public string RutaAvatar { get; set; } = string.Empty;
        public bool Activo { get; set; }

    }

    public class RespuestaApi<T>
    {
        public T? Datos { get; set; }
    }
}
