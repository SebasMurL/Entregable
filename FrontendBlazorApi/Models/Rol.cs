using System.Text.Json.Serialization;

namespace FrontendBlazorApi.Models;

public class Rol
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}
