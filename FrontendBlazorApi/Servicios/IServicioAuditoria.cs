using FrontendBlazorApi.Models;

namespace FrontendBlazorApi.Servicios
{
    public interface IServicioAuditoria
    {
        Task RegistrarCreacionAsync<T>(T entidad, string usuarioId, string ipAddress, string userAgent) where T : class;
        Task RegistrarActualizacionAsync<T>(T entidadAnterior, T entidadNueva, string usuarioId, string ipAddress, string userAgent) where T : class;
        Task RegistrarEliminacionAsync<T>(T entidad, string usuarioId, string ipAddress, string userAgent) where T : class;
        Task<List<AuditoriaDto>> ObtenerAuditoriasAsync(string? tabla = null, DateTime? desde = null, DateTime? hasta = null);
    }
}