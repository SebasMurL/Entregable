using System.ComponentModel.DataAnnotations;

namespace FrontendBlazorApi.Models
{
    public class EjecucionPresupuesto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El IdPresupuesto es obligatorio")]
        public int IdPresupuesto { get; set; }

        [Required(ErrorMessage = "El Anio es obligatorio")]
        [Range(1900, 2100, ErrorMessage = "El Anio debe estar entre 1900 y 2100")]
        public int Anio { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El MontoPlaneado debe ser mayor o igual a 0")]
        public decimal? MontoPlaneado { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El MontoEjecutado debe ser mayor o igual a 0")]
        public decimal? MontoEjecutado { get; set; }

        [StringLength(int.MaxValue, ErrorMessage = "Las Observaciones son demasiado largas")]
        public string? Observaciones { get; set; }

        // Propiedad de navegación (opcional)
        public Presupuesto? Presupuesto { get; set; }
    }
}