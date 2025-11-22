using System.ComponentModel.DataAnnotations;

namespace FrontendBlazorApi.Models
{
    public class DistribucionPresupuesto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El IdPresupuestoPadre es obligatorio")]
        public int IdPresupuestoPadre { get; set; }

        [Required(ErrorMessage = "El IdProyectoHijo es obligatorio")]
        public int IdProyectoHijo { get; set; }

        [Required(ErrorMessage = "El MontoAsignado es obligatorio")]
        [Range(0, double.MaxValue, ErrorMessage = "El MontoAsignado debe ser mayor o igual a 0")]
        public decimal MontoAsignado { get; set; }

        // Propiedades de navegación (opcional)
        public Presupuesto? PresupuestoPadre { get; set; }
        public Proyecto? ProyectoHijo { get; set; }
    }
}