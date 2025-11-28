using FrontendBlazorApi.Models;

namespace FrontendBlazorApi.Servicios
{
    public class ServicioAuditoria : IServicioAuditoria
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        private static readonly List<Auditoria> _auditoriasEnMemoria = new();
        private static long _contadorId = 1;
        private static readonly object _lockObject = new();

        public ServicioAuditoria(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RegistrarCreacionAsync<T>(T entidad, string usuarioId, string ipAddress, string userAgent) where T : class
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        var auditoria = new Auditoria
                        {
                            Id = _contadorId++,
                            TablaAfectada = typeof(T).Name,
                            Accion = "INSERT",
                            RegistroId = ObtenerId(entidad),
                            DatosNuevos = System.Text.Json.JsonSerializer.Serialize(entidad, 
                                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                            UsuarioId = usuarioId,
                            IpAddress = ipAddress,
                            UserAgent = userAgent,
                            FechaAuditoria = DateTime.Now
                        };

                        _auditoriasEnMemoria.Add(auditoria);
                        

                    }
                }
                catch (Exception ex)
                {
                }
            });
        }

        public async Task RegistrarActualizacionAsync<T>(T entidadAnterior, T entidadNueva, string usuarioId, string ipAddress, string userAgent) where T : class
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        var auditoria = new Auditoria
                        {
                            Id = _contadorId++,
                            TablaAfectada = typeof(T).Name,
                            Accion = "UPDATE",
                            RegistroId = ObtenerId(entidadNueva),
                            DatosAnteriores = System.Text.Json.JsonSerializer.Serialize(entidadAnterior, 
                                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                            DatosNuevos = System.Text.Json.JsonSerializer.Serialize(entidadNueva, 
                                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                            UsuarioId = usuarioId,
                            IpAddress = ipAddress,
                            UserAgent = userAgent,
                            FechaAuditoria = DateTime.Now
                        };

                        _auditoriasEnMemoria.Add(auditoria);
                        

                        
                        // Mostrar cambios específicos
                        MostrarCambios(entidadAnterior, entidadNueva);
                    }
                }
                catch (Exception ex)
                {
                }
            });
        }

        public async Task RegistrarEliminacionAsync<T>(T entidad, string usuarioId, string ipAddress, string userAgent) where T : class
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        var auditoria = new Auditoria
                        {
                            Id = _contadorId++,
                            TablaAfectada = typeof(T).Name,
                            Accion = "DELETE",
                            RegistroId = ObtenerId(entidad),
                            DatosAnteriores = System.Text.Json.JsonSerializer.Serialize(entidad, 
                                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                            UsuarioId = usuarioId,
                            IpAddress = ipAddress,
                            UserAgent = userAgent,
                            FechaAuditoria = DateTime.Now
                        };

                        _auditoriasEnMemoria.Add(auditoria);
                        

                    }
                }
                catch (Exception ex)
                {
                }
            });
        }

        public async Task<List<AuditoriaDto>> ObtenerAuditoriasAsync(string? tabla = null, DateTime? desde = null, DateTime? hasta = null)
        {
            return await Task.Run(() =>
            {
                try
                {

                    var query = _auditoriasEnMemoria.AsQueryable();

                    if (!string.IsNullOrEmpty(tabla))
                    {
                        query = query.Where(a => a.TablaAfectada == tabla);
                    }

                    if (desde.HasValue)
                    {
                        query = query.Where(a => a.FechaAuditoria >= desde.Value);
                    }

                    if (hasta.HasValue)
                    {
                        query = query.Where(a => a.FechaAuditoria <= hasta.Value);
                    }

                    var auditorias = query
                        .OrderByDescending(a => a.FechaAuditoria)
                        .Select(a => new AuditoriaDto
                        {
                            Id = a.Id,
                            TablaAfectada = a.TablaAfectada,
                            Accion = a.Accion,
                            RegistroId = a.RegistroId,
                            DatosAnteriores = a.DatosAnteriores,
                            DatosNuevos = a.DatosNuevos,
                            UsuarioId = a.UsuarioId,
                            FechaAuditoria = a.FechaAuditoria
                        })
                        .ToList();

                    return auditorias;
                }
                catch (Exception ex)
                {
                    return new List<AuditoriaDto>();
                }
            });
        }

        private int ObtenerId<T>(T entidad) where T : class
        {
            try
            {
                var propiedad = typeof(T).GetProperty("Id");
                if (propiedad != null)
                {
                    var valor = propiedad.GetValue(entidad);
                    var id = Convert.ToInt32(valor);
                    return id;
                }
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private void MostrarCambios<T>(T anterior, T nuevo) where T : class
        {
            try
            {
                var propiedades = typeof(T).GetProperties();
                var cambios = new List<string>();

                foreach (var prop in propiedades)
                {
                    var valorAnterior = prop.GetValue(anterior)?.ToString();
                    var valorNuevo = prop.GetValue(nuevo)?.ToString();

                    if (valorAnterior != valorNuevo)
                    {
                        cambios.Add($"{prop.Name}: '{valorAnterior}' → '{valorNuevo}'");
                    }
                }

                if (cambios.Any())
                {
                    foreach (var cambio in cambios)
                    {
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}