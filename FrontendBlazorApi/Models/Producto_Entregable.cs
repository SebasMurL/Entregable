using System.ComponentModel.DataAnnotations;

namespace FrontendBlazorApi.Models
{
    public class Producto_Entregable
    {
        [Required(ErrorMessage = "Debe seleccionar un Producto.")]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un Entregable.")]
        public int IdEntregable { get; set; }

        public DateTime? FechaAsociacion { get; set; }
    }
}
