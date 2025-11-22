// Program.cs
// Archivo de arranque principal de la aplicación Blazor Web App (plantilla moderna unificada).
// Aquí se configuran los servicios y se define cómo se ejecuta la aplicación.

using FrontendBlazorApi.Components;          // Importa el espacio de nombres donde está App.razor
using Microsoft.AspNetCore.Components;       // Librerías base de Blazor
using Microsoft.AspNetCore.Components.Web;   // Funcionalidades adicionales de renderizado

var builder = WebApplication.CreateBuilder(args);

// -------------------------------
// Registro de servicios en el contenedor de dependencias
// -------------------------------

// Se registran los servicios de Razor Components.
// "AddInteractiveServerComponents" habilita el modo interactivo tipo Blazor Server (SignalR).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


 // Servicio HttpClient para consumir la API externa de productos.
 // Se activará más adelante cuando se implemente la conexión a la API.
 builder.Services.AddHttpClient("ApiUsuarios", cliente =>
 {
     // URL base de la API que expone /api/producto
     cliente.BaseAddress = new Uri("http://localhost:5031/");
     // Aquí se pueden agregar encabezados por defecto si se requiere.
 });

// Servicio HttpClient para consumir la API de tipos de producto
builder.Services.AddHttpClient("ApiTipoProductos", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiTipoProyectos", cliente =>
{
   cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiTipoResponsables", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiEntregables", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiEstados", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiVariableEstrategicas", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});
builder.Services.AddHttpClient("ApiProyectos", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});
builder.Services.AddHttpClient("ApiActividades", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});
builder.Services.AddHttpClient("ApiMetasEstrategicas", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});
builder.Services.AddHttpClient("ApiObjetivosEstrategicos", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});
builder.Services.AddHttpClient("ApiMeta_Proyectos", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiArchivos", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiResponsables", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiEstado_Proyectos", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiProductos", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiProducto_Entregables", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiResponsable_Entregables", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiPresupuestos", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

builder.Services.AddHttpClient("ApiEjecucionPresupuestos", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});



/*
 // Política CORS opcional. Útil solo si el navegador llamara
 // directamente a la API externa. Para Blazor Server no es necesario
 // si las llamadas se hacen con HttpClient en el servidor.
 const string nombrePoliticaCors = "PermitirTodo";
 builder.Services.AddCors(opciones =>
 {
     opciones.AddPolicy(nombrePoliticaCors, politica =>
         politica
             .AllowAnyOrigin()
             .AllowAnyMethod()
             .AllowAnyHeader());
 });
*/

var app = builder.Build();

// -------------------------------
// Configuración del pipeline HTTP
// -------------------------------
if (!app.Environment.IsDevelopment())
{
    // En producción, se activa un manejador de errores genérico.
    // El parámetro "createScopeForErrors" mejora el aislamiento de errores.
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // HSTS: seguridad extra para navegadores (fuerza HTTPS durante 30 días por defecto).
    app.UseHsts();
}

// Redirección automática a HTTPS si el usuario entra por HTTP.
app.UseHttpsRedirection();

// Servir archivos estáticos desde wwwroot (CSS, JS, imágenes, etc.).
app.UseStaticFiles();

// Middleware antifalsificación (protección contra CSRF).
app.UseAntiforgery();

/*
 // Activar CORS si se definió una política anteriormente.
 app.UseCors(nombrePoliticaCors);
*/

// -------------------------------
// Mapeo de componentes raíz
// -------------------------------
// Se indica que el componente principal de la aplicación es App.razor.
// Aquí arranca todo el enrutamiento y la estructura del sitio.
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// -------------------------------
// Inicio de la aplicación
// -------------------------------
app.Run();
