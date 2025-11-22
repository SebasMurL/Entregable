using System.Text.Json.Serialization;

namespace FrontendBlazorApi.Models
{
    public class VariableEstrategica
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}