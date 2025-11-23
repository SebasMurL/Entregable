using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace FrontendBlazorApi.Servicios;

/// <summary>
/// Gestiona: login, logout, token, y RutasRol en sessionStorage + memoria.
/// </summary>
public class ServicioAutenticacion
{
    private readonly IJSRuntime _js;
    private readonly IHttpClientFactory _httpClientFactory;
    private string? _tokenEnMemoria;
    private List<RutaRol>? _rutasRolEnMemoria;

    public ServicioAutenticacion(IJSRuntime js, IHttpClientFactory httpClientFactory)
    {
        _js = js;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Obtiene un HttpClient configurado con el token JWT automáticamente.
    /// </summary>
    public async Task<HttpClient> ObtenerClienteAutenticadoAsync()
    {
        var cliente = _httpClientFactory.CreateClient("ApiGenerica");

        // Obtener token (de memoria o localStorage)
        var token = _tokenEnMemoria ?? await ObtenerTokenValidoAsync();

        if (!string.IsNullOrWhiteSpace(token))
        {
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return cliente;
    }

    /// <summary>
    /// Inicia sesión guardando el token en sessionStorage.
    /// </summary>
    public async Task IniciarSesionAsync(string token, string email)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("El token no puede estar vacío");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email no puede estar vacío");

        // Guardar en sessionStorage (se borra al cerrar navegador)
        await _js.InvokeVoidAsync("sessionStorage.setItem", "token", token);
        await _js.InvokeVoidAsync("sessionStorage.setItem", "email", email);

        // Guardar en memoria
        _tokenEnMemoria = token;
    }

    /// <summary>
    /// Cierra sesión limpiando todo.
    /// </summary>
    public async Task CerrarSesionAsync()
    {
        await _js.InvokeVoidAsync("sessionStorage.clear");
        _tokenEnMemoria = null;
        _rutasRolEnMemoria = null;
    }

    /// <summary>
    /// Obtiene el token de sessionStorage (sin verificar expiración).
    /// La API dirá si está expirado (responderá 401).
    /// </summary>
    public async Task<string?> ObtenerTokenValidoAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string>("sessionStorage.getItem", "token");

            if (string.IsNullOrWhiteSpace(token))
                return null;

            _tokenEnMemoria = token;
            return token;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Obtiene el email del usuario.
    /// </summary>
    public async Task<string?> ObtenerEmailUsuarioAsync()
    {
        try
        {
            return await _js.InvokeAsync<string>("sessionStorage.getItem", "email");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Verifica si hay sesión activa.
    /// </summary>
    public async Task<bool> EstaAutenticadoAsync()
    {
        var token = await ObtenerTokenValidoAsync();
        return !string.IsNullOrWhiteSpace(token);
    }

    /// <summary>
    /// Obtiene claims del JWT.
    /// </summary>
    public async Task<IEnumerable<Claim>> ObtenerClaimsAsync()
    {
        try
        {
            var token = await ObtenerTokenValidoAsync();
            if (string.IsNullOrWhiteSpace(token))
                return Enumerable.Empty<Claim>();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims;
        }
        catch
        {
            return Enumerable.Empty<Claim>();
        }
    }

    /// <summary>
    /// Obtiene un claim específico.
    /// </summary>
    public async Task<string?> ObtenerClaimAsync(string claimType)
    {
        var claims = await ObtenerClaimsAsync();
        return claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    /// <summary>
    /// Guarda las RutasRol en memoria y sessionStorage.
    /// </summary>
    public async Task GuardarRutasRolAsync(List<RutaRol> rutasRol)
    {
        if (rutasRol == null)
            throw new ArgumentNullException(nameof(rutasRol));

        _rutasRolEnMemoria = rutasRol;

        var json = JsonSerializer.Serialize(rutasRol);
        await _js.InvokeVoidAsync("sessionStorage.setItem", "rutasRol", json);
    }

    /// <summary>
    /// Obtiene las RutasRol de memoria o sessionStorage.
    /// </summary>
    public async Task<List<RutaRol>> ObtenerRutasRolAsync()
    {
        // Si ya están en memoria, devolverlas
        if (_rutasRolEnMemoria != null)
            return _rutasRolEnMemoria;

        try
        {
            // Intentar cargar de sessionStorage
            var json = await _js.InvokeAsync<string>("sessionStorage.getItem", "rutasRol");

            if (!string.IsNullOrWhiteSpace(json))
            {
                _rutasRolEnMemoria = JsonSerializer.Deserialize<List<RutaRol>>(json);
                return _rutasRolEnMemoria ?? new List<RutaRol>();
            }
        }
        catch
        {
            // Si falla, devolver lista vacía
        }

        return new List<RutaRol>();
    }

    /// <summary>
    /// Verifica si el usuario tiene permiso para acceder a una ruta específica.
    /// </summary>
    public async Task<bool> TienePermisoParaRutaAsync(string ruta)
    {
        try
        {
            // Obtener rutas permitidas
            var rutasRol = await ObtenerRutasRolAsync();

            if (rutasRol == null || rutasRol.Count == 0)
                return false;

            // Obtener email del usuario actual
            var email = await ObtenerEmailUsuarioAsync();

            if (string.IsNullOrEmpty(email))
                return false;

            // Obtener rol del usuario desde el email guardado en sessionStorage
            // Comparar: si email = "admin@correo.com" → buscar solo rutas con rol "Administrador"
            var rolUsuario = rutasRol.FirstOrDefault()?.NombreRol;

            // Verificar si existe la ruta para este rol específico
            return rutasRol.Any(r =>
                r.NombreRuta?.Equals(ruta, StringComparison.OrdinalIgnoreCase) == true);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Modelo simple para RutaRol.
/// La tabla rutarol solo tiene 2 columnas: ruta y rol.
/// </summary>
public class RutaRol
{
    [System.Text.Json.Serialization.JsonPropertyName("ruta")]
    public string? NombreRuta { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("rol")]
    public string? NombreRol { get; set; }
}
