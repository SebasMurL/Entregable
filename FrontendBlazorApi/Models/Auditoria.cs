namespace FrontendBlazorApi.Models
{
    public class Auditoria
    {
        public long Id { get; set; }
        public string TablaAfectada { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public int RegistroId { get; set; }
        public string? DatosAnteriores { get; set; }
        public string? DatosNuevos { get; set; }
        public string? UsuarioId { get; set; }
        public DateTime FechaAuditoria { get; set; } = DateTime.Now;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class AuditoriaDto
    {
        public long Id { get; set; }
        public string TablaAfectada { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public int RegistroId { get; set; }
        public string? DatosAnteriores { get; set; }
        public string? DatosNuevos { get; set; }
        public string? UsuarioId { get; set; }
        public DateTime FechaAuditoria { get; set; }
        public string AccionColor => Accion switch
        {
            "INSERT" => "success",
            "UPDATE" => "warning", 
            "DELETE" => "danger",
            _ => "secondary"
        };
    }
}