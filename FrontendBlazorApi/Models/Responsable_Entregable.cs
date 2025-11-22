using System.ComponentModel.DataAnnotations;

namespace FrontendBlazorApi.Models
{
    public class Responsable_Entregable
    {
        [Required(ErrorMessage = "Debe seleccionar un Responsable.")]
        public int IdResponsable { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un Entregable.")]
        public int IdEntregable { get; set; }

        public DateTime? FechaAsociacion { get; set; }
    }
}
