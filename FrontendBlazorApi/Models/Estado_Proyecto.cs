using System;
using System.ComponentModel.DataAnnotations;

namespace FrontendBlazorApi.Models
{
public class Estado_Proyecto
{
[Key]
[Required(ErrorMessage = "El campo IdProyecto es obligatorio.")]
public int IdProyecto { get; set; }


    [Required(ErrorMessage = "El campo IdEstado es obligatorio.")]
    public int IdEstado { get; set; }

    // Relaciones opcionales (para mostrar nombres en tablas)
    public Proyecto? Proyecto { get; set; }
    public Estado? Estado { get; set; }
}


}
