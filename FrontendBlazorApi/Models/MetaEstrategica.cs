using System.ComponentModel.DataAnnotations;

namespace FrontendBlazorApi.Models
{
    public class MetaEstrategica
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El IdObjetivo es obligatorio")]
        public int IdObjetivo { get; set; }

        [Required(ErrorMessage = "El Titulo es obligatorio")]
        [StringLength(255, ErrorMessage = "El Titulo no puede exceder los 255 caracteres")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(int.MaxValue, ErrorMessage = "La Descripcion es demasiado larga")]
        public string? Descripcion { get; set; }

        // Propiedad de navegación (opcional)
        public ObjetivoEstrategico? ObjetivoEstrategico { get; set; }
    }
}