// Program.cs - Actualizado con nueva estructura de Abstracciones

// Importa tipos básicos como TimeSpan para configurar tiempos.
using System;

// Importa la API para construir la aplicación web y el pipeline HTTP.
using Microsoft.AspNetCore.Builder;

// Importa la API para registrar servicios en el contenedor de inyección de dependencias (DIP).
using Microsoft.Extensions.DependencyInjection;

// Importa utilidades para detectar el entorno (Desarrollo, Producción, etc.).
using Microsoft.Extensions.Hosting;

// Importa utilidades para leer configuraciones desde archivos JSON.
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using webapicsharp.Modelos; // donde está ConfiguracionJwt

// Crea el "builder": punto de inicio para configurar servicios y la aplicación.
var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// CONFIGURACÓN (OCP: se puede extender sin tocar la lógica)
// ---------------------------------------------------------

// Agrega un archivo JSON opcional para configuraciones adicionales.
// Si "tablasprohibidas.json" existe, se carga; si no existe, no falla.
builder.Configuration.AddJsonFile(
    "tablasprohibidas.json",
    optional: true,
    reloadOnChange: true
);

// ---------------------------------------------------------
// SERVICIOS (SRP: solo registro, sin lógica de negocio aquí)
// ---------------------------------------------------------

// Agrega soporte para controladores. Los controladores viven en la carpeta "Controllers".
builder.Services.AddControllers();

//CORS (Cross-Origin Resource Sharing) Intercambio de recursos de origen cruzado
// permite que la API sea consumida desde otros dominios.
// Agrega CORS con una política genérica llamada "PermitirTodo".
// Permite consumir la API desde cualquier origen, método y encabezado.
// Más adelante se puede endurecer (DOMINIOS específicos) sin cambiar controladores (OCP).
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("PermitirTodo", politica => politica
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
    );
});

// Agrega caché en memoria y sesión HTTP ligera (opcional).
// La sesión sirve para guardar datos pequeños por usuario durante un tiempo.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{
    // Tiempo de inactividad permitido antes de expirar la sesión.
    opciones.IdleTimeout = TimeSpan.FromMinutes(30);

    // Marca la cookie de sesión como accesible solo por HTTP (más seguro).
    opciones.Cookie.HttpOnly = true;

    // Indica que la cookie es esencial para el funcionamiento (no depende de consentimientos).
    opciones.Cookie.IsEssential = true;
});

// Agrega servicios para exponer documentación Swagger/OpenAPI.
// Útil para probar endpoints manualmente mientras no existe frontend.
builder.Services.AddEndpointsApiExplorer();

// Configuración avanzada de Swagger para mostrar el botón "Authorize"
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Genérica CRUD Multi-Base de Datos",
        Version = "v1",
        Description = "API REST genérica para operaciones CRUD sobre cualquier tabla. Soporta SQL Server, PostgreSQL, MySQL y MariaDB. Incluye autenticación JWT y acceso dinámico configurado por tabla."
    });

    // Define el esquema de seguridad JWT
    var esquemaSeguridad = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese el token con el prefijo 'Bearer'. Ejemplo: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    };

    // Añade la definición del esquema
    opciones.AddSecurityDefinition("Bearer", esquemaSeguridad);

    // Indica que todos los endpoints usan este esquema por defecto
    opciones.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


// -----------------------------------------------------------------
// REGISTRO DE SERVICIOS (Dependency Injection - DIP)
// -----------------------------------------------------------------
// Esta sección registra todas las abstracciones (interfaces) con sus implementaciones concretas.
// El contenedor de inyección de dependencias de ASP.NET Core resolverá automáticamente
// las dependencias cuando sean necesarias.
//
// TIPOS DE REGISTRO:
// - AddSingleton: Una instancia para toda la aplicación (compartida entre requests)
// - AddScoped: Una instancia por request HTTP (se crea y destruye con cada petición)
// - AddTransient: Una instancia nueva cada vez que se solicita
// -----------------------------------------------------------------

// -----------------------------------------------------------------
// REGISTRO DE POLÍTICA DE TABLAS PROHIBIDAS (MEJORA ARQUITECTÓNICA)
// -----------------------------------------------------------------
// NUEVO: Registro de la política que determina qué tablas están permitidas/prohibidas.
//
// MEJORA ARQUITECTÓNICA - POR QUÉ ESTO ES IMPORTANTE PARA TUS ESTUDIANTES:
// Antes: ServicioCrud dependía directamente de IConfiguration (violaba DIP)
// Ahora: ServicioCrud depende de IPoliticaTablasProhibidas (cumple DIP perfectamente)
//
// BENEFICIOS DE ESTA MEJORA:
// 1. SEPARACIÓN DE RESPONSABILIDADES (SRP):
//    - ServicioCrud solo se encarga de lógica de negocio CRUD
//    - PoliticaTablasProhibidasDesdeJson se encarga de leer configuración
//    - Cada clase tiene una responsabilidad única y bien definida
//
// 2. INVERSIÓN DE DEPENDENCIAS (DIP):
//    - ServicioCrud (módulo de alto nivel) NO depende de IConfiguration (detalle de implementación)
//    - Ambos dependen de IPoliticaTablasProhibidas (abstracción del dominio)
//    - Cumple perfectamente el principio: "depende de abstracciones, no de implementaciones"
//
// 3. ABIERTO/CERRADO (OCP):
//    - Podemos cambiar cómo se leen las tablas prohibidas sin modificar ServicioCrud
//    - Podemos crear PoliticaTablasProhibidasDesdeBaseDatos mañana
//    - ServicioCrud está cerrado para modificación, abierto para extensión
//
// 4. TESTABILIDAD:
//    - Fácil crear mocks de IPoliticaTablasProhibidas para testing
//    - No necesitas configurar IConfiguration completo en tests
//    - Tests más simples y más rápidos
//
// POR QUÉ AddSingleton:
// - La configuración de tablas prohibidas no cambia durante la ejecución de la aplicación
// - Queremos una sola instancia compartida por todos los requests (mejor performance)
// - No tiene estado mutable, es thread-safe (solo lee configuración una vez en el constructor)
// - Reduce overhead de crear instancias en cada request
//
// ARQUITECTURA DE DEPENDENCIAS:
// ServicioCrud → IPoliticaTablasProhibidas → PoliticaTablasProhibidasDesdeJson → IConfiguration
// (Lógica de      (Abstracción              (Implementación                      (Infraestructura
//  negocio)        del dominio)               concreta)                            de config)
builder.Services.AddSingleton<
    webapicsharp.Servicios.Abstracciones.IPoliticaTablasProhibidas,
    webapicsharp.Servicios.Politicas.PoliticaTablasProhibidasDesdeJson>();

// -----------------------------------------------------------------
// REGISTRO DE SERVICIO CRUD (DIP)
// -----------------------------------------------------------------
// Registra el servicio principal de lógica de negocio CRUD.
// AddScoped: Una instancia por request HTTP (patrón común para servicios de negocio)
//
// DEPENDENCIAS DE ServicioCrud:
// - IRepositorioLecturaTabla: Para acceder a datos (se registra más abajo según DatabaseProvider)
// - IPoliticaTablasProhibidas: Para validar tablas permitidas (registrado arriba)
//
// El contenedor DI inyectará automáticamente ambas dependencias cuando cree ServicioCrud.
builder.Services.AddScoped<
    webapicsharp.Servicios.Abstracciones.IServicioCrud,
    webapicsharp.Servicios.ServicioCrud>();

// -----------------------------------------------------------------
// REGISTRO DEL PROVEEDOR DE CONEXIÓN (DIP)
// -----------------------------------------------------------------
// Registra el proveedor que entrega cadenas de conexión a la base de datos.
// AddSingleton: La configuración de conexión no cambia durante la ejecución.
//
// RESPONSABILIDAD:
// - Lee "DatabaseProvider" y "ConnectionStrings" de appsettings.json
// - Proporciona la cadena de conexión correcta según el proveedor configurado
// - Encapsula toda la lógica de configuración de conexiones
//
// NOTA: IProveedorConexion está en Servicios.Abstracciones (parte del dominio, no infraestructura)
builder.Services.AddSingleton<
    webapicsharp.Servicios.Abstracciones.IProveedorConexion,
    webapicsharp.Servicios.Conexion.ProveedorConexion>();

// REGISTRO AUTOMÁTICO DEL REPOSITORIO SEGÚN DatabaseProvider (DIP + OCP)
// La API genérica lee la configuración y usa el proveedor correcto automáticamente
var proveedorBD = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

// -----------------------------------------------------------------------------
// REGISTRO DE SERVICIO CONSULTAS (DIP)
// -----------------------------------------------------------------------------
// Este registro enlaza IServicioConsultas con la clase ServicioConsultas.
// Para que funcione correctamente, siempre debe estar registrado también
// un IRepositorioConsultas que cubra al motor de base de datos en uso.
//
// Si no existe un repositorio de consultas para el motor activo, conviene
// mover esta línea dentro del switch de Program.cs y dejarla solamente
// en los casos donde sí esté implementado el repositorio correspondiente.
// -----------------------------------------------------------------------------
builder.Services.AddScoped<webapicsharp.Servicios.Abstracciones.IServicioConsultas,
    webapicsharp.Servicios.ServicioConsultas>();


switch (proveedorBD.ToLower())
{
    case "postgres":
        // Usar PostgreSQL cuando DatabaseProvider = "Postgres"
                builder.Services.AddScoped<webapicsharp.Repositorios.Abstracciones.IRepositorioLecturaTabla,
                                           webapicsharp.Repositorios.RepositorioLecturaPostgreSQL>();
        // Repositorio de consultas para PostgreSQL (necesario porque IServicioConsultas se registra global)
        builder.Services.AddScoped<
            webapicsharp.Repositorios.Abstracciones.IRepositorioConsultas,
            webapicsharp.Repositorios.RepositorioConsultasPostgreSQL
        >();                                           
        break;
    case "mariadb":
    case "mysql":
        // Repositorio de lectura genérico para MySQL/MariaDB
        builder.Services.AddScoped<
            webapicsharp.Repositorios.Abstracciones.IRepositorioLecturaTabla,
            webapicsharp.Repositorios.RepositorioLecturaMysqlMariaDB>();

        // Repositorio de consultas para MySQL/MariaDB
        builder.Services.AddScoped<
            webapicsharp.Repositorios.Abstracciones.IRepositorioConsultas,
            webapicsharp.Repositorios.RepositorioConsultasMysqlMariaDB>();

        // Nota: si IServicioConsultas está registrado de forma global (como en tu patrón),
        // aquí no se agrega nada más; el contenedor ya podrá construirlo porque existe
        // IRepositorioConsultas para este motor.
        break;

    case "sqlserver":
    case "sqlserverexpress":
    case "localdb":
    default:
        // Usar SQL Server para todos los demás casos (incluyendo el valor por defecto)
        builder.Services.AddScoped<webapicsharp.Repositorios.Abstracciones.IRepositorioLecturaTabla,
                                   webapicsharp.Repositorios.RepositorioLecturaSqlServer>();

        builder.Services.AddScoped<webapicsharp.Repositorios.Abstracciones.IRepositorioConsultas,
                               webapicsharp.Repositorios.RepositorioConsultasSqlServer>();


        break;
}

// Nueva sección para JWT
// ---------------------------------------------------------
// CONFIGURACIÓN JWT (para autenticación segura con tokens)
// ---------------------------------------------------------

// Vincula la sección "Jwt" del archivo appsettings.Development.json a la clase ConfiguracionJwt.
builder.Services.Configure<ConfiguracionJwt>(
    builder.Configuration.GetSection("Jwt")
);

// Crea una instancia temporal con la configuración de JWT.
var configuracionJwt = new ConfiguracionJwt();
builder.Configuration.GetSection("Jwt").Bind(configuracionJwt);

// Registra el servicio de autenticación basado en JWT Bearer.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        // Define las reglas de validación del token.
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Valida el emisor del token.
            ValidateAudience = true, // Valida el público objetivo.
            ValidateLifetime = true, // Valida que no esté expirado.
            ValidateIssuerSigningKey = true, // Valida la firma.
            ValidIssuer = configuracionJwt.Issuer, // Emisor válido.
            ValidAudience = configuracionJwt.Audience, // Audiencia válida.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuracionJwt.Key) // Clave secreta.
            )
        };
    });

// También registra la configuración de JWT para inyección de dependencias si se necesita en otros lugares.
builder.Services.Configure<ConfiguracionJwt>(builder.Configuration.GetSection("Jwt"));


// Construye la aplicación con todo lo configurado arriba.
var app = builder.Build();

// ---------------------------------------------------------
// MIDDLEWARE (orden importa: se ejecuta de arriba hacia abajo)
// ---------------------------------------------------------

// En Desarrollo se muestran páginas de error detalladas para depurar.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Habilita Swagger siempre y expone la UI en la ruta /swagger.
// Esto no afecta la lógica de negocio y se puede quitar en Producción.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Indica dónde vive el documento OpenAPI.
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "webapicsharp v1");

    // Define el prefijo de ruta. Con esto, la UI queda en /swagger.
    c.RoutePrefix = "swagger";
});

// Redirige HTTP a HTTPS para mejorar la seguridad, si el certificado está configurado.
app.UseHttpsRedirection();

// Aplica la política CORS definida como "PermitirTodo".
app.UseCors("PermitirTodo");

// Habilita la sesión HTTP (si no se usa, se puede quitar sin tocar controladores).
app.UseSession();

//nueva línesa para jwt
// Activa la autenticación JWT antes de aplicar la autorización.
app.UseAuthentication();


// Agrega el middleware de autorización (para cuando existan endpoints protegidos).
app.UseAuthorization();

// Mapea las rutas de los controladores. Sin controladores aún, la API expone solo Swagger.
app.MapControllers();

// Arranca la aplicación y queda escuchando solicitudes HTTP.
app.Run();

