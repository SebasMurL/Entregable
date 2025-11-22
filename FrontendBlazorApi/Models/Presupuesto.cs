using System.ComponentModel.DataAnnotations;

namespace FrontendBlazorApi.Models
{
    public class Presupuesto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El IdProyecto es obligatorio")]
        public int IdProyecto { get; set; }

        [Required(ErrorMessage = "El MontoSolicitado es obligatorio")]
        [Range(0, double.MaxValue, ErrorMessage = "El MontoSolicitado debe ser mayor o igual a 0")]
        public decimal MontoSolicitado { get; set; }

        [Required(ErrorMessage = "El Estado es obligatorio")]
        [StringLength(20, ErrorMessage = "El Estado no puede exceder los 20 caracteres")]
        [RegularExpression("^(Pendiente|Aprobado|Rechazado)$", ErrorMessage = "El Estado debe ser Pendiente, Aprobado o Rechazado")]
        public string Estado { get; set; } = "Pendiente";

        [Range(0, double.MaxValue, ErrorMessage = "El MontoAprobado debe ser mayor o igual a 0")]
        public decimal? MontoAprobado { get; set; }

        [Range(1900, 2100, ErrorMessage = "El PeriodoAnio debe estar entre 1900 y 2100")]
        public int? PeriodoAnio { get; set; }

        public DateTime? FechaSolicitud { get; set; }

        public DateTime? FechaAprobacion { get; set; }

        [StringLength(int.MaxValue, ErrorMessage = "Las Observaciones son demasiado largas")]
        public string? Observaciones { get; set; }

        // Propiedad de navegación (opcional)
        public Proyecto? Proyecto { get; set; }

        // Colecciones de relaciones (opcional)
        public ICollection<DistribucionPresupuesto>? DistribucionesPresupuesto { get; set; }
        public ICollection<EjecucionPresupuesto>? EjecucionesPresupuesto { get; set; }
    }
}