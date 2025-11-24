using System;
using System.ComponentModel.DataAnnotations;

namespace FrontendBlazorApi.Models
{
    public class Meta_Proyecto
    {
        [Required(ErrorMessage = "El IdMeta es obligatorio")]
        public int IdMeta { get; set; }

        [Required(ErrorMessage = "El IdProyecto es obligatorio")]
        public int IdProyecto { get; set; }

        public DateTime? FechaAsociacion { get; set; }

        // Propiedades de navegación (opcional)
        public MetaEstrategica? MetaEstrategica { get; set; }
        public Proyecto? Proyecto { get; set; }
    }
}