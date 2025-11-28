using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FrontendBlazorApi.Models
{
public class ArchivoEntregable
{
    [Required]
    public int IdArchivo { get; set; }   // FK hacia Archivo

    [Required]
    public int IdEntregable { get; set; } // FK hacia Entregable
}
}