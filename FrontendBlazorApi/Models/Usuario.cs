using System.Text.Json.Serialization;
namespace FrontendBlazorApi.Models
{
    // Modelo que representa una Persona tal como lo devuelve la API
    public class Usuario
    {
        public int Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("contrasena")] 
        public string Contrasena { get; set; } = string.Empty;
        [JsonPropertyName("rutaavatar")] 
        public string RutaAvatar { get; set; } = string.Empty;
        [JsonPropertyName("activo")] 
        public bool Activo { get; set; }

    }

    public class RespuestaApi<T>
    {
        public T? Datos { get; set; }
    }
    public class UsuarioConRoles
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("roles")]
    public List<RolDto> Roles { get; set; } = new();
    
    [JsonPropertyName("rutaavatar")] 

    public string? RutaAvatar { get; set; }
    [JsonPropertyName("activo")] 
    public bool Activo { get; set; } = true;
}
    //a
public class RolDto
{
    [JsonPropertyName("idrol")]
    public int IdRol { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}
}
