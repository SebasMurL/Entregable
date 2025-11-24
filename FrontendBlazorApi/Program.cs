// =============================================================================
// Program.cs
// =============================================================================
// DESCRIPCIÓN:
// Archivo de punto de entrada principal de la aplicación Blazor Server.
// Este archivo configura todos los servicios, middleware y pipeline HTTP
// necesarios para que la aplicación funcione correctamente.
//
// CONCEPTOS CLAVE PARA ESTUDIANTES:
//   1. Contenedor de inyección de dependencias: Donde se registran todos
//      los servicios que la aplicación necesita (HttpClient, servicios propios, etc.)
//   2. Pipeline HTTP: Cadena de middleware que procesa cada petición HTTP
//      en orden (autenticación, logging, manejo de errores, archivos estáticos, etc.)
//   3. Patrón Builder: Se usa WebApplicationBuilder para configurar la app
//      antes de ejecutarla
//   4. Configuración por entorno: Comportamiento diferente en Development
//      vs Production
// =============================================================================

// -----------------------------------------------------------------------------
// SECCIÓN 1: IMPORTACIÓN DE ESPACIOS DE NOMBRES (USING)
// -----------------------------------------------------------------------------
// Los "using" importan bibliotecas de .NET que proporcionan funcionalidades
// específicas. Son como los "import" en otros lenguajes de programación.

using FrontendBlazorApi.Components;          // Espacio de nombres del proyecto donde está App.razor
using Microsoft.AspNetCore.Components;       // Clases base de Blazor (ComponentBase, ParameterAttribute, etc.)
using Microsoft.AspNetCore.Components.Web;   // Funcionalidades adicionales de renderizado web para Blazor
using Microsoft.AspNetCore.Authentication.Cookies; // Para autenticación con cookies
using Microsoft.AspNetCore.Components.Authorization; // Para AuthenticationStateProvider

// -----------------------------------------------------------------------------
// SECCIÓN 2: CREACIÓN DEL BUILDER
// -----------------------------------------------------------------------------
// El "builder" es un objeto que permite configurar la aplicación antes de
// que se ejecute. Recibe la configuración de appsettings.json, variables
// de entorno, argumentos de línea de comandos, etc.

/// <summary>
/// WebApplicationBuilder: Clase que proporciona una interfaz fluida para
/// configurar servicios y opciones de la aplicación antes de construirla.
/// </summary>
/// <remarks>
/// El método CreateBuilder hace lo siguiente automáticamente:
/// 1. Carga la configuración desde appsettings.json y appsettings.{Environment}.json
/// 2. Carga variables de entorno
/// 3. Procesa argumentos de línea de comandos (args)
/// 4. Configura el sistema de logging (ILogger)
/// 5. Detecta el entorno de ejecución (Development, Staging, Production)
///
/// PATRÓN BUILDER:
/// Este patrón permite construir objetos complejos paso a paso de forma legible.
/// Ejemplo:
/// var builder = CreateBuilder();  // Paso 1: Crear el constructor
/// builder.Services.Add...();      // Paso 2: Agregar servicios
/// var app = builder.Build();      // Paso 3: Construir la aplicación
/// app.Run();                       // Paso 4: Ejecutar
/// </remarks>
var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// SECCIÓN 3: REGISTRO DE SERVICIOS EN EL CONTENEDOR DE INYECCIÓN DE DEPENDENCIAS
// =============================================================================
// Aquí se registran todos los servicios que la aplicación necesita.
// Cuando un componente o clase pida una dependencia (mediante el constructor
// o @inject), el contenedor proporciona la instancia adecuada automáticamente.
//
// TIEMPOS DE VIDA DE LOS SERVICIOS:
// 1. Transient: Se crea una nueva instancia cada vez que se solicita
//    - Usar para servicios ligeros y sin estado
//    - Método: AddTransient<T>()
//
// 2. Scoped: Se crea una instancia por cada solicitud HTTP (scope)
//    - En Blazor Server, un "scope" es la conexión SignalR del usuario
//    - La misma instancia se reutiliza durante toda la sesión del usuario
//    - Usar para servicios que necesitan mantener estado durante la sesión
//    - Método: AddScoped<T>()
//
// 3. Singleton: Se crea una única instancia para toda la aplicación
//    - La misma instancia se comparte entre todos los usuarios
//    - Usar para servicios sin estado o con estado global compartido
//    - Método: AddSingleton<T>()
// =============================================================================

// -----------------------------------------------------------------------------
// REGISTRO 1: Servicios de Razor Components
// -----------------------------------------------------------------------------

/// <summary>
/// Registra los servicios necesarios para Blazor Razor Components.
/// Incluye renderizado, routing, navegación y más.
/// </summary>
/// <remarks>
/// AddRazorComponents() registra servicios como:
/// - NavigationManager: Para navegación programática entre páginas
/// - IJSRuntime: Para interoperabilidad con JavaScript
/// - IComponentContext: Para contexto de renderizado de componentes
/// - HttpContext: Para acceder a información de la petición HTTP
///
/// Es el servicio fundamental que hace funcionar Blazor.
/// </remarks>
builder.Services.AddRazorComponents()
    // AddInteractiveServerComponents() habilita el modo interactivo Blazor Server
    // Esto configura SignalR para comunicación bidireccional en tiempo real
    // entre el navegador y el servidor mediante WebSockets
    .AddInteractiveServerComponents();

// Configurar opciones del circuito de Blazor Server
builder.Services.AddServerSideBlazor(options =>
{
    // Desconectar el circuito después de 30 segundos de inactividad
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(30);
    // Intentos de reconexión del cliente
    options.DisconnectedCircuitMaxRetained = 100;
    // Tamaño máximo del buffer de JavaScript interop
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
});

// -----------------------------------------------------------------------------
// REGISTRO: Autenticación SIMPLIFICADA
// -----------------------------------------------------------------------------
// SOLO un servicio: ServicioAutenticacion
builder.Services.AddScoped<FrontendBlazorApi.Servicios.ServicioAutenticacion>();

// CONCEPTOS DE BLAZOR SERVER:
// - El código C# se ejecuta en el servidor, no en el navegador
// - La UI se actualiza mediante mensajes de SignalR (WebSockets)
// - Ventajas:
//   1. Código del lado del servidor es seguro (no se expone al cliente)
//   2. Acceso directo a recursos del servidor (bases de datos, archivos, etc.)
//   3. Tamaño de descarga pequeño (solo HTML y JS de SignalR)
// - Desventajas:
//   1. Requiere conexión constante al servidor
//   2. Latencia en cada interacción del usuario
//   3. Consume más recursos del servidor (una conexión por usuario)

// -----------------------------------------------------------------------------
// REGISTRO 2: HttpClient Factory
// -----------------------------------------------------------------------------

/// <summary>
/// Registra un cliente HTTP configurado para consumir la API REST externa.
/// Se usa el patrón HttpClient Factory para evitar problemas de agotamiento
/// de sockets y mejorar el rendimiento.
/// </summary>
/// <remarks>
/// IMPORTANTE: Nunca crear HttpClient con "new HttpClient()" directamente.
/// Esto puede causar el agotamiento de sockets del sistema operativo.
///
/// HttpClient Factory soluciona este problema mediante:
/// 1. Pool de handlers HTTP reutilizables
/// 2. Gestión automática del ciclo de vida de las conexiones
/// 3. Manejo eficiente de DNS (evita problemas de DNS stale)
///
/// CONFIGURACIÓN:
/// - Nombre: "ApiGenerica" (identificador único para este cliente)
/// - BaseAddress: URL base de la API (http://localhost:5031/)
///
/// USO EN CÓDIGO:
/// var cliente = fabricaHttp.CreateClient("ApiGenerica");
/// // El cliente ya tiene configurada la BaseAddress
/// var respuesta = await cliente.GetAsync("api/producto");
/// // URL completa: http://localhost:5031/api/producto
/// </remarks>
// Configurar HttpClient para la API
builder.Services.AddHttpClient("ApiGenerica", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});
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
// Rename client to ApiMetaProyectos (was ApiMeta_Proyectos) to match usages in components
builder.Services.AddHttpClient("ApiMetaProyectos", cliente =>
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

// Registrar ServicioApiGenerico (CRUD)
builder.Services.AddScoped<FrontendBlazorApi.Servicios.ServicioApiGenerico>();

// =============================================================================
// SECCIÓN 4: CONFIGURACIÓN DE CORS (COMENTADA)
// =============================================================================
// CORS (Cross-Origin Resource Sharing) permite que el navegador haga peticiones
// a un dominio diferente al que sirve la aplicación web.
//
// ¿CUÁNDO ES NECESARIO CORS?
// - Cuando el frontend está en un dominio (ej: https://miweb.com)
// - Y la API está en otro dominio (ej: https://api.miweb.com)
// - El navegador bloquea estas peticiones por seguridad (Same-Origin Policy)
// - CORS configura qué dominios pueden hacer peticiones a la API
//
// ¿POR QUÉ ESTÁ COMENTADO AQUÍ?
// - En Blazor Server, las peticiones HTTP se hacen desde el SERVIDOR, no desde el navegador
// - El servidor no está sujeto a la política Same-Origin Policy
// - Por lo tanto, CORS no es necesario para Blazor Server
//
// ¿CUÁNDO SERÍA NECESARIO?
// - En Blazor WebAssembly (código que se ejecuta en el navegador)
// - Si el backend necesita recibir peticiones directas del navegador (AJAX)
// =============================================================================

/*
const string nombrePoliticaCors = "PermitirTodo";
builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy(nombrePoliticaCors, politica =>
        politica
            // AllowAnyOrigin: Permite peticiones desde cualquier dominio
            // PELIGRO: Esto es inseguro en producción. Especificar dominios permitidos:
            // .WithOrigins("https://miweb.com", "https://www.miweb.com")
            .AllowAnyOrigin()

            // AllowAnyMethod: Permite cualquier verbo HTTP (GET, POST, PUT, DELETE, etc.)
            .AllowAnyMethod()

            // AllowAnyHeader: Permite cualquier header HTTP
            .AllowAnyHeader());
});

// CONFIGURACIÓN SEGURA DE CORS PARA PRODUCCIÓN:
// builder.Services.AddCors(opciones =>
// {
//     opciones.AddPolicy("PoliticaSegura", politica =>
//         politica
//             .WithOrigins("https://miweb.com")           // Solo este dominio
//             .WithMethods("GET", "POST")                  // Solo estos métodos
//             .WithHeaders("Content-Type", "Authorization") // Solo estos headers
//             .AllowCredentials());                        // Permitir cookies
// });
*/

// =============================================================================
// SECCIÓN 5: CONSTRUCCIÓN DE LA APLICACIÓN
// =============================================================================
// Hasta aquí solo se han registrado servicios. Ahora se construye la aplicación
// con toda la configuración especificada.
// =============================================================================

/// <summary>
/// Construye la aplicación con todos los servicios registrados.
/// A partir de este punto, no se pueden agregar más servicios.
/// </summary>
/// <remarks>
/// El método Build() hace lo siguiente:
/// 1. Valida que todos los servicios registrados tengan sus dependencias satisfechas
/// 2. Crea el contenedor de inyección de dependencias
/// 3. Inicializa el pipeline HTTP (middleware)
/// 4. Prepara la aplicación para empezar a recibir peticiones
///
/// Después de Build(), el objeto cambia de WebApplicationBuilder a WebApplication.
/// WebApplication tiene métodos para configurar el pipeline HTTP (middleware).
/// </remarks>
var app = builder.Build();

// =============================================================================
// SECCIÓN 6: CONFIGURACIÓN DEL PIPELINE HTTP (MIDDLEWARE)
// =============================================================================
// El pipeline HTTP es una cadena de componentes (middleware) que procesan
// cada petición HTTP en orden secuencial.
//
// ORDEN IMPORTA:
// Los middleware se ejecutan en el orden en que se agregan con app.Use...()
//
// FLUJO DE UNA PETICIÓN:
// 1. Petición HTTP llega al servidor
// 2. Pasa por cada middleware en orden (hacia abajo)
// 3. Llega al endpoint final (controlador o componente Razor)
// 4. Respuesta sube por los middleware en orden inverso (hacia arriba)
// 5. Respuesta HTTP se envía al cliente
//
// EJEMPLO DE FLUJO:
// Petición → UseHttpsRedirection → UseStaticFiles → UseAntiforgery → Endpoint
//           ↓                     ↓                ↓                 ↓
// Respuesta ← UseHttpsRedirection ← UseStaticFiles ← UseAntiforgery ← Endpoint
// =============================================================================

// -----------------------------------------------------------------------------
// MIDDLEWARE 1: Manejo de Excepciones (Solo en Producción)
// -----------------------------------------------------------------------------

/// <summary>
/// Configuración diferenciada según el entorno de ejecución.
/// </summary>
/// <remarks>
/// ENTORNOS DE EJECUCIÓN:
/// - Development: Máquina del desarrollador, muestra información detallada de errores
/// - Staging: Entorno de pruebas previo a producción
/// - Production: Servidor en vivo, no debe mostrar información sensible
///
/// El entorno se configura mediante la variable ASPNETCORE_ENVIRONMENT:
/// - En Visual Studio: Properties/launchSettings.json
/// - En servidor: Variable de entorno del sistema
/// - Por defecto: Production
/// </remarks>
if (!app.Environment.IsDevelopment())
{
    // UseExceptionHandler: Middleware que captura excepciones no controladas
    // y las maneja de forma centralizada

    /// <summary>
    /// Maneja excepciones no controladas redirigiendo a una página de error amigable.
    /// </summary>
    /// <remarks>
    /// PARÁMETROS:
    /// - "/Error": Ruta de la página que se mostrará cuando ocurra un error
    /// - createScopeForErrors: true
    ///   * Crea un nuevo scope de servicios para el manejador de errores
    ///   * Aísla el error del contexto original de la petición
    ///   * Previene que el error contamine otros servicios
    ///
    /// ¿QUÉ SUCEDE CUANDO HAY UNA EXCEPCIÓN?
    /// 1. Se captura la excepción no controlada
    /// 2. Se registra en los logs (ILogger)
    /// 3. Se redirige al usuario a /Error
    /// 4. Se muestra un mensaje genérico sin detalles técnicos (seguridad)
    ///
    /// ¿POR QUÉ SOLO EN PRODUCCIÓN?
    /// En Development, queremos ver el error completo con stack trace para depurar.
    /// En Production, mostrar detalles técnicos sería un riesgo de seguridad.
    /// </remarks>
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // UseHsts: Middleware de seguridad que agrega el header Strict-Transport-Security

    /// <summary>
    /// Habilita HSTS (HTTP Strict Transport Security) para forzar conexiones HTTPS.
    /// </summary>
    /// <remarks>
    /// ¿QUÉ ES HSTS?
    /// Es un mecanismo de seguridad que instruye al navegador a usar solo HTTPS
    /// para comunicarse con el servidor durante un período de tiempo.
    ///
    /// FUNCIONAMIENTO:
    /// 1. El servidor envía el header: Strict-Transport-Security: max-age=2592000
    /// 2. El navegador recuerda que este sitio solo debe usarse con HTTPS
    /// 3. Durante 30 días (2592000 segundos), el navegador automáticamente
    ///    convierte http:// a https:// antes de hacer la petición
    /// 4. Esto previene ataques de tipo "man-in-the-middle" y "downgrade attacks"
    ///
    /// CONFIGURACIÓN POR DEFECTO:
    /// - max-age: 30 días (2592000 segundos)
    /// - includeSubDomains: false
    /// - preload: false
    ///
    /// CONFIGURACIÓN PERSONALIZADA:
    /// builder.Services.AddHsts(opciones =>
    /// {
    ///     opciones.MaxAge = TimeSpan.FromDays(365);
    ///     opciones.IncludeSubDomains = true;
    ///     opciones.Preload = true;
    /// });
    ///
    /// ¿POR QUÉ SOLO EN PRODUCCIÓN?
    /// En Development usamos HTTP (localhost), HSTS causaría problemas.
    /// </remarks>
    app.UseHsts();
}

// -----------------------------------------------------------------------------
// MIDDLEWARE 2: Redirección HTTPS
// -----------------------------------------------------------------------------

/// <summary>
/// Redirige automáticamente todas las peticiones HTTP a HTTPS.
/// </summary>
/// <remarks>
/// FUNCIONAMIENTO:
/// 1. Cliente hace petición a: http://miapp.com/productos
/// 2. Este middleware intercepta la petición
/// 3. Devuelve respuesta 307 Temporary Redirect o 301 Moved Permanently
/// 4. Header Location apunta a: https://miapp.com/productos
/// 5. El navegador automáticamente hace la petición a la URL HTTPS
///
/// CÓDIGOS DE ESTADO:
/// - 307 Temporary Redirect: Por defecto, mantiene el método HTTP (POST sigue siendo POST)
/// - 301 Moved Permanently: El navegador cachea la redirección
///
/// ¿POR QUÉ ES IMPORTANTE HTTPS?
/// 1. Cifrado: Los datos se transmiten encriptados (TLS/SSL)
/// 2. Integridad: Los datos no pueden ser modificados en tránsito
/// 3. Autenticidad: Se verifica que el servidor es quien dice ser (certificado)
/// 4. SEO: Google favorece sitios con HTTPS
/// 5. Requisito: Muchas APIs modernas solo funcionan con HTTPS
///
/// CONFIGURACIÓN PERSONALIZADA:
/// builder.Services.AddHttpsRedirection(opciones =>
/// {
///     opciones.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
///     opciones.HttpsPort = 5001;
/// });
/// </remarks>
app.UseHttpsRedirection();

// -----------------------------------------------------------------------------
// MIDDLEWARE 3: Archivos Estáticos
// -----------------------------------------------------------------------------

/// <summary>
/// Sirve archivos estáticos desde la carpeta wwwroot.
/// </summary>
/// <remarks>
/// ARCHIVOS ESTÁTICOS:
/// Son archivos que no necesitan procesamiento del servidor:
/// - CSS: Hojas de estilo (wwwroot/css/*.css)
/// - JavaScript: Scripts del cliente (wwwroot/js/*.js)
/// - Imágenes: PNG, JPG, SVG, ICO (wwwroot/images/*)
/// - Fuentes: WOFF, TTF (wwwroot/fonts/*)
/// - Otros: PDF, TXT, JSON, etc.
///
/// MAPEO DE URLS:
/// - wwwroot/css/app.css → https://miapp.com/css/app.css
/// - wwwroot/images/logo.png → https://miapp.com/images/logo.png
/// - wwwroot/js/site.js → https://miapp.com/js/site.js
///
/// CARACTERÍSTICAS:
/// 1. Cache automático: Los archivos se cachean en el navegador
/// 2. ETag: Se genera un hash del archivo para validación de cache
/// 3. Compresión: Se pueden comprimir con Gzip/Brotli
/// 4. Seguridad: Solo sirve archivos de wwwroot, no de otras carpetas
///
/// ORDEN EN EL PIPELINE:
/// Debe estar ANTES de UseRouting/UseEndpoints para que los archivos
/// estáticos no pasen por el routing innecesariamente (mejora rendimiento).
///
/// CONFIGURACIÓN AVANZADA:
/// app.UseStaticFiles(new StaticFileOptions
/// {
///     // Servir archivos desde otra carpeta
///     FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "MisCarpeta")),
///     RequestPath = "/archivos",
///
///     // Configurar headers de cache
///     OnPrepareResponse = ctx =>
///     {
///         ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=600");
///     }
/// });
/// </remarks>
app.UseStaticFiles();

// -----------------------------------------------------------------------------
// MIDDLEWARE: Deshabilitar caché del navegador (IMPORTANTE PARA SEGURIDAD)
// -----------------------------------------------------------------------------
app.Use(async (context, next) =>
{
    // Agregar headers para evitar caché en TODAS las respuestas
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

// -----------------------------------------------------------------------------
// MIDDLEWARE: Anti-falsificación (CSRF Protection)
// -----------------------------------------------------------------------------

/// <summary>
/// Protege contra ataques CSRF (Cross-Site Request Forgery).
/// </summary>
/// <remarks>
/// ¿QUÉ ES CSRF?
/// Un ataque donde un sitio malicioso hace que el navegador del usuario
/// envíe peticiones no autorizadas a otro sitio donde el usuario está autenticado.
///
/// EJEMPLO DE ATAQUE CSRF:
/// 1. Usuario inicia sesión en banco.com
/// 2. El navegador guarda la cookie de sesión
/// 3. Usuario visita sitio-malicioso.com (sin cerrar sesión del banco)
/// 4. sitio-malicioso.com tiene código oculto:
///    <img src="https://banco.com/transferir?destino=atacante&monto=1000">
/// 5. El navegador envía la petición CON la cookie de sesión automáticamente
/// 6. banco.com ejecuta la transferencia porque la cookie es válida
///
/// ¿CÓMO PROTEGE UseAntiforgery?
/// 1. Genera un token secreto único por sesión
/// 2. Lo incluye en formularios como campo oculto
/// 3. También lo guarda en una cookie
/// 4. Al enviar el formulario, valida que ambos tokens coincidan
/// 5. El sitio malicioso no puede leer el token (Same-Origin Policy)
/// 6. La petición maliciosa se rechaza porque no tiene el token válido
///
/// EN BLAZOR:
/// - EditForm automáticamente incluye el token anti-falsificación
/// - No es necesario hacer nada manualmente en los componentes
/// - El token se valida automáticamente en cada postback
///
/// CONFIGURACIÓN:
/// builder.Services.AddAntiforgery(opciones =>
/// {
///     opciones.HeaderName = "X-CSRF-TOKEN";
///     opciones.Cookie.Name = "X-CSRF-TOKEN-COOKIE";
///     opciones.Cookie.SameSite = SameSiteMode.Strict;
/// });
///
/// IMPORTANTE:
/// Este middleware solo protege formularios. Para APIs REST, considerar usar:
/// - Tokens JWT con validación de origen
/// - Headers personalizados (X-Requested-With: XMLHttpRequest)
/// - Validación de referer/origin
/// </remarks>
app.UseAntiforgery();

// =============================================================================
// SECCIÓN 7: CONFIGURACIÓN DE CORS (COMENTADA)
// =============================================================================
// Si se habilitó CORS en la sección de servicios, aquí se activa en el pipeline.
// DEBE estar entre UseRouting y UseEndpoints.
// =============================================================================

/*
app.UseCors(nombrePoliticaCors);

ORDEN CORRECTO DEL MIDDLEWARE CORS:
1. app.UseRouting();        // Primero routing
2. app.UseCors(...);         // Luego CORS
3. app.UseAuthorization();   // Luego autorización
4. app.MapRazorComponents();  // Finalmente endpoints
*/

// =============================================================================
// SECCIÓN 8: MAPEO DE COMPONENTES RAÍZ (ENDPOINTS)
// =============================================================================
// Aquí se define el componente raíz de la aplicación y el modo de renderizado.
// Este es el punto final del pipeline donde se procesa la lógica de negocio.
// =============================================================================

/// <summary>
/// Mapea el componente raíz de la aplicación (App.razor) y configura el modo de renderizado.
/// </summary>
/// <remarks>
/// MapRazorComponents<App>():
/// - Define que App.razor es el componente raíz de la aplicación
/// - App.razor contiene el Router que maneja la navegación entre páginas
/// - Todas las rutas (@page "/...") se resuelven desde aquí
///
/// AddInteractiveServerRenderMode():
/// - Habilita el modo interactivo Blazor Server
/// - Los componentes con @rendermode InteractiveServer usarán SignalR
/// - Permite interactividad en tiempo real (eventos, binding, etc.)
///
/// OTROS MODOS DE RENDERIZADO DISPONIBLES:
/// 1. Server (default): Renderizado estático en el servidor, sin interactividad
/// 2. InteractiveServer: Interactividad mediante SignalR (lo que usamos aquí)
/// 3. InteractiveWebAssembly: Código C# ejecutándose en el navegador con WebAssembly
/// 4. InteractiveAuto: Inicia con Server, descarga WebAssembly en segundo plano
///
/// ¿QUÉ ES App.razor?
/// Es el componente raíz que contiene:
/// - <Router>: Componente que maneja el enrutamiento
/// - <RouteView>: Renderiza el componente que coincide con la URL
/// - <FocusOnNavigate>: Maneja el foco del teclado en navegaciones
/// - Layout: Define el diseño común (MainLayout.razor con menú, footer, etc.)
///
/// FLUJO DE RENDERIZADO:
/// 1. Usuario navega a /productos
/// 2. Router busca un componente con @page "/productos"
/// 3. Encuentra Productos.razor
/// 4. Lo renderiza dentro del Layout (MainLayout.razor)
/// 5. El HTML resultante se envía al navegador
/// 6. SignalR establece conexión para interactividad
/// 7. Eventos del usuario (clicks, input) se envían al servidor via SignalR
/// 8. El servidor actualiza el estado y envía los cambios de UI via SignalR
///
/// PERSONALIZACIÓN:
/// app.MapRazorComponents<App>()
///     .AddInteractiveServerRenderMode()
///     .AddInteractiveWebAssemblyRenderMode()  // Habilitar WebAssembly también
///     .AddAdditionalAssemblies(typeof(OtroProyecto.Component).Assembly); // Agregar ensamblados
/// </remarks>
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// NOTA SOBRE RENDIMIENTO:
// Cada usuario conectado consume memoria en el servidor (estado de la aplicación).
// Para aplicaciones con muchos usuarios concurrentes, considerar:
// - Blazor WebAssembly (código se ejecuta en el navegador, no en el servidor)
// - Optimizar el estado de los componentes (liberar memoria cuando no se necesite)
// - Usar Scoped services en lugar de Singleton cuando sea posible

// =============================================================================
// SECCIÓN 9: INICIO DE LA APLICACIÓN
// =============================================================================
// Finalmente, se inicia la aplicación y comienza a escuchar peticiones HTTP.
// =============================================================================

/// <summary>
/// Inicia la aplicación y comienza a escuchar peticiones HTTP entrantes.
/// Este método es bloqueante: el programa se queda aquí hasta que se detenga.
/// </summary>
/// <remarks>
/// ¿QUÉ HACE app.Run()?
/// 1. Inicia el servidor web Kestrel (servidor HTTP de .NET)
/// 2. Comienza a escuchar en los puertos configurados (por defecto 5000 HTTP, 5001 HTTPS)
/// 3. Procesa cada petición HTTP a través del pipeline de middleware
/// 4. Mantiene la aplicación corriendo hasta que:
///    - Se presiona Ctrl+C
///    - Se envía una señal de terminación (SIGTERM)
///    - Ocurre un error fatal
///
/// CONFIGURACIÓN DE PUERTOS:
/// Los puertos se configuran en:
/// - Properties/launchSettings.json (Development)
/// - Argumentos de línea de comandos: --urls "http://localhost:5000"
/// - Variables de entorno: ASPNETCORE_URLS
/// - appsettings.json:
///   "Kestrel": {
///     "Endpoints": {
///       "Http": { "Url": "http://localhost:5000" },
///       "Https": { "Url": "https://localhost:5001" }
///     }
///   }
///
/// ALTERNATIVAS A app.Run():
/// - app.RunAsync(): Versión asíncrona, permite código después del inicio
/// - app.Start(): Inicia sin bloquear, permite múltiples hosts
/// - app.StopAsync(): Detiene la aplicación programáticamente
///
/// GRACEFUL SHUTDOWN:
/// Cuando se detiene la aplicación, se sigue este proceso:
/// 1. Se dejan de aceptar nuevas peticiones
/// 2. Se espera a que las peticiones en curso terminen (timeout: 5 segundos)
/// 3. Se llaman los métodos Dispose de los servicios registrados
/// 4. Se cierran las conexiones de base de datos
/// 5. Se liberan los recursos del sistema
/// </remarks>
app.Run();

// =============================================================================
// FIN DEL ARCHIVO Program.cs
// =============================================================================
// RESUMEN DE CONCEPTOS PARA ESTUDIANTES:
//
// 1. INYECCIÓN DE DEPENDENCIAS:
//    - Registrar servicios con builder.Services.Add...()
//    - Tres tiempos de vida: Transient, Scoped, Singleton
//    - Las dependencias se inyectan automáticamente en constructores
//
// 2. PIPELINE HTTP (MIDDLEWARE):
//    - Cadena de componentes que procesan cada petición en orden
//    - Cada middleware puede: procesar, modificar, cortocircuitar, pasar al siguiente
//    - El orden importa: UseHttpsRedirection antes de UseStaticFiles, etc.
//
// 3. SEGURIDAD:
//    - HTTPS: Cifrado de datos en tránsito
//    - HSTS: Forzar HTTPS en el navegador
//    - Anti-falsificación (CSRF): Protección contra ataques de sitios cruzados
//    - CORS: Control de acceso entre dominios (cuando aplica)
//
// 4. BLAZOR SERVER:
//    - Código C# se ejecuta en el servidor, no en el navegador
//    - Comunicación bidireccional mediante SignalR (WebSockets)
//    - Renderizado interactivo sin JavaScript personalizado
//    - Una conexión persistente por usuario conectado
//
// 5. HTTPLIENT FACTORY:
//    - Nunca crear HttpClient con "new"
//    - Registrar clientes nombrados con AddHttpClient
//    - Configurar BaseAddress, headers, timeout, etc.
//    - Evita agotamiento de sockets y problemas de DNS
//
// 6. CONFIGURACIÓN POR ENTORNO:
//    - Development: Información detallada de errores para depuración
//    - Production: Mensajes genéricos, seguridad reforzada
//    - Configurar con ASPNETCORE_ENVIRONMENT
//
// 7. ARCHIVOS ESTÁTICOS:
//    - Se sirven desde wwwroot
//    - CSS, JavaScript, imágenes, fuentes, etc.
//    - Cache automático en el navegador
//    - No requieren procesamiento del servidor
//
// 8. ROUTING:
//    - App.razor contiene el Router
//    - @page "/ruta" define rutas en componentes
//    - NavigationManager permite navegación programática
//    - Parámetros de ruta: @page "/producto/{id}"
// =============================================================================
