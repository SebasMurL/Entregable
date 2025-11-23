using Microsoft.AspNetCore.Components;
using FrontendBlazorApi.Servicios;

namespace FrontendBlazorApi.Components;

/// <summary>
/// Clase base SIMPLE para páginas que requieren autenticación.
/// </summary>
public abstract class PaginaAutenticada : ComponentBase
{
    [Inject]
    protected ServicioAutenticacion ServicioAuth { get; set; } = default!;

    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    protected bool AutenticacionVerificada { get; private set; } = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await VerificarAutenticacion();
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task VerificarAutenticacion()
    {
        try
        {
            // 1. Verificar si hay token
            var autenticado = await ServicioAuth.EstaAutenticadoAsync();

            if (!autenticado)
            {
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }

            // 2. Verificar permisos de ruta
            var rutaActual = new Uri(Navigation.Uri).AbsolutePath;
            var rutasPublicas = new[] { "/", "/login", "/home" };

            if (!rutasPublicas.Contains(rutaActual, StringComparer.OrdinalIgnoreCase))
            {
                var tienePermiso = await ServicioAuth.TienePermisoParaRutaAsync(rutaActual);

                if (!tienePermiso)
                {
                    Navigation.NavigateTo("/home", forceLoad: true);
                    return;
                }
            }

            AutenticacionVerificada = true;
            await OnAutenticacionVerificada();
        }
        catch
        {
            Navigation.NavigateTo("/login", forceLoad: true);
        }
    }

    /// <summary>
    /// Método que las páginas hijas implementan para cargar datos.
    /// Se llama después de verificar autenticación.
    /// </summary>
    protected virtual Task OnAutenticacionVerificada()
    {
        return Task.CompletedTask;
    }
}
