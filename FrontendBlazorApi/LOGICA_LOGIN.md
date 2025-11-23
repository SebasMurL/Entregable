# Logica de Login y Control de Acceso Basado en Roles (RBAC)

## Resumen

Este documento explica como funciona el sistema de autenticacion y autorizacion implementado en el frontend Blazor. El sistema utiliza JWT tokens para autenticacion y un modelo de control de acceso basado en roles (RBAC) para determinar que rutas puede acceder cada usuario.

## Arquitectura del Sistema

### Componentes Principales

1. **Login.razor** - Pagina de inicio de sesion
2. **ServicioAutenticacion.cs** - Servicio que maneja tokens y permisos
3. **PaginaAutenticada.cs** - Clase base que verifica permisos antes de renderizar paginas
4. **ServicioApiGenerico.cs** - Servicio HTTP para comunicacion con la API

### Flujo de Autenticacion

```
Usuario ingresa credenciales
    |
    v
Login.razor envio credenciales a API
    |
    v
API valida y retorna JWT token
    |
    v
Frontend guarda token en sessionStorage
    |
    v
Frontend carga rutas permitidas del usuario
    |
    v
Usuario navega a pagina protegida
    |
    v
PaginaAutenticada verifica token y permisos
    |
    v
Permite o deniega acceso
```

## Estructura de Base de Datos

### Tablas Relevantes

**usuario**
- email (PK) - Email del usuario
- contrasena - Contrasena encriptada

**rol**
- id (PK) - ID del rol
- nombre - Nombre del rol (ej: "Administrador", "Vendedor")

**rol_usuario** (Relacion muchos-a-muchos)
- fkemail (FK) - Email del usuario
- fkidrol (FK) - ID del rol

**ruta**
- ruta (PK) - Path de la ruta (ej: "/clientes")
- descripcion - Descripcion de la ruta

**rutarol** (Relacion muchos-a-muchos)
- ruta (FK) - Path de la ruta
- rol (FK) - Nombre del rol

## Proceso de Login Detallado

### Paso 1: Envio de Credenciales

Ubicacion: `Login.razor` linea 320-330

```csharp
var datosToken = new
{
    tabla = "usuario",
    campoUsuario = "email",
    campoContrasena = "contrasena",
    usuario = credenciales.Usuario,
    contrasena = credenciales.Contrasena
};

var resultado = await servicioApi.PostAsync<RespuestaToken>("api/Autenticacion/token", datosToken);
```

El frontend envia las credenciales al endpoint `api/Autenticacion/token` del backend.

### Paso 2: Guardar Token JWT

Ubicacion: `Login.razor` linea 339

```csharp
await servicioAuth.IniciarSesionAsync(resultado.Token, credenciales.Usuario);
```

El metodo `IniciarSesionAsync` guarda el token en sessionStorage:

Ubicacion: `ServicioAutenticacion.cs` linea 56-60

```csharp
await _js.InvokeVoidAsync("sessionStorage.setItem", "token", token);
await _js.InvokeVoidAsync("sessionStorage.setItem", "email", email);
_tokenEnMemoria = token;
```

### Paso 3: Cargar Rutas Permitidas

Ubicacion: `Login.razor` linea 341-378

Este es el proceso mas importante y complejo:

#### 3.1. Obtener roles del usuario

```csharp
var todosRolUsuario = await servicioApi.ObtenerTodosAsync<RolUsuario>("rol_usuario");
var rolesDelUsuario = todosRolUsuario?.Where(ru => ru.FkEmail == credenciales.Usuario).ToList();
```

Se consulta la tabla `rol_usuario` y se filtran solo los registros donde `fkemail` coincide con el email del usuario que inicio sesion.

Ejemplo: Si `vendedor1@correo.com` tiene roles con IDs 2 y 3:
```
[
  { fkemail: "vendedor1@correo.com", fkidrol: 2 },
  { fkemail: "vendedor1@correo.com", fkidrol: 3 }
]
```

#### 3.2. Obtener nombres de roles

```csharp
var todosRoles = await servicioApi.ObtenerTodosAsync<Rol>("rol");

var nombresRolesUsuario = rolesDelUsuario
    .Select(ru => todosRoles?.FirstOrDefault(r => r.IdRol == ru.FkIdRol)?.Nombre)
    .Where(nombre => !string.IsNullOrEmpty(nombre))
    .ToList();
```

Se consulta la tabla `rol` y se obtienen los nombres de los roles del usuario.

Ejemplo:
```
Entrada: [2, 3]
Salida: ["Vendedor", "Cajero"]
```

#### 3.3. Obtener rutas de todos los roles del usuario

```csharp
var todasRutasRol = await servicioApi.ObtenerTodosAsync<RutaRol>("rutarol");
var rutasDelUsuario = todasRutasRol?
    .Where(rr => nombresRolesUsuario.Contains(rr.NombreRol))
    .Distinct()
    .ToList();
```

Se consulta la tabla `rutarol` y se filtran solo las rutas que pertenecen a cualquiera de los roles del usuario.

Ejemplo:
```
Si el usuario tiene roles ["Vendedor", "Cajero"]
Y rutarol contiene:
  { ruta: "/home", rol: "Vendedor" }
  { ruta: "/clientes", rol: "Vendedor" }
  { ruta: "/facturas", rol: "Vendedor" }
  { ruta: "/home", rol: "Cajero" }
  { ruta: "/facturas", rol: "Cajero" }

Resultado (despues de Distinct):
  [
    { ruta: "/home", rol: "Vendedor" },
    { ruta: "/clientes", rol: "Vendedor" },
    { ruta: "/facturas", rol: "Vendedor" },
    { ruta: "/home", rol: "Cajero" },
    { ruta: "/facturas", rol: "Cajero" }
  ]
```

IMPORTANTE: `.Distinct()` elimina duplicados basandose en la REFERENCIA del objeto, no en los valores. En este caso, como cada objeto es diferente (aunque tengan misma ruta pero diferente rol), todos se mantienen.

#### 3.4. Guardar rutas en sessionStorage

```csharp
await servicioAuth.GuardarRutasRolAsync(rutasDelUsuario);
```

Las rutas se guardan en sessionStorage como JSON:

Ubicacion: `ServicioAutenticacion.cs` linea 152-161

```csharp
_rutasRolEnMemoria = rutasRol;
var json = JsonSerializer.Serialize(rutasRol);
await _js.InvokeVoidAsync("sessionStorage.setItem", "rutasRol", json);
```

### Paso 4: Redireccion a Home

Ubicacion: `Login.razor` linea 380-382

```csharp
MostrarMensaje("Inicio de sesion exitoso! Redirigiendo...", "success");
await Task.Delay(1500);
navegador.NavigateTo("/home", forceLoad: true);
```

## Verificacion de Permisos en Paginas

### Clase Base PaginaAutenticada

Todas las paginas protegidas heredan de `PaginaAutenticada.cs`.

Ubicacion: `PaginaAutenticada.cs` linea 9

```csharp
public abstract class PaginaAutenticada : ComponentBase
```

### Verificacion en OnAfterRenderAsync

Ubicacion: `PaginaAutenticada.cs` linea 19-28

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await VerificarAutenticacion();
        StateHasChanged();
    }

    await base.OnAfterRenderAsync(firstRender);
}
```

El metodo `VerificarAutenticacion` se ejecuta SOLO en el primer render de la pagina.

### Proceso de Verificacion

Ubicacion: `PaginaAutenticada.cs` linea 30-65

#### Paso 1: Verificar Token

```csharp
var autenticado = await ServicioAuth.EstaAutenticadoAsync();

if (!autenticado)
{
    Navigation.NavigateTo("/login", forceLoad: true);
    return;
}
```

Verifica que exista un token en sessionStorage.

#### Paso 2: Verificar Permisos de Ruta

```csharp
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
```

Las rutas `/`, `/login` y `/home` son publicas y no requieren verificacion.

Para cualquier otra ruta, se verifica si el usuario tiene permiso.

### Verificacion de Permiso Especifico

Ubicacion: `ServicioAutenticacion.cs` linea 194-222

```csharp
public async Task<bool> TienePermisoParaRutaAsync(string ruta)
{
    try
    {
        var rutasRol = await ObtenerRutasRolAsync();

        if (rutasRol == null || rutasRol.Count == 0)
            return false;

        var email = await ObtenerEmailUsuarioAsync();

        if (string.IsNullOrEmpty(email))
            return false;

        return rutasRol.Any(r =>
            r.NombreRuta?.Equals(ruta, StringComparison.OrdinalIgnoreCase) == true);
    }
    catch
    {
        return false;
    }
}
```

Este metodo:
1. Obtiene las rutas guardadas en sessionStorage
2. Busca si existe alguna ruta que coincida con la ruta solicitada
3. Retorna `true` si encuentra coincidencia, `false` si no

## Ejemplo Practico Completo

### Escenario: Usuario vendedor1@correo.com

**Datos en BD:**

rol_usuario:
```
{ fkemail: "vendedor1@correo.com", fkidrol: 2 }
{ fkemail: "vendedor1@correo.com", fkidrol: 3 }
```

rol:
```
{ id: 2, nombre: "Vendedor" }
{ id: 3, nombre: "Cajero" }
```

rutarol:
```
{ ruta: "/home", rol: "Vendedor" }
{ ruta: "/clientes", rol: "Vendedor" }
{ ruta: "/facturas", rol: "Vendedor" }
{ ruta: "/home", rol: "Cajero" }
{ ruta: "/facturas", rol: "Cajero" }
```

### Flujo:

1. Usuario ingresa `vendedor1@correo.com` / `vend123`
2. API valida y retorna token JWT
3. Frontend guarda token en sessionStorage
4. Frontend carga rutas:
   - Obtiene roles del usuario: [2, 3]
   - Convierte a nombres: ["Vendedor", "Cajero"]
   - Filtra rutarol por esos roles
   - Guarda en sessionStorage:
   ```json
   [
     {"ruta":"/home","rol":"Vendedor"},
     {"ruta":"/clientes","rol":"Vendedor"},
     {"ruta":"/facturas","rol":"Vendedor"},
     {"ruta":"/home","rol":"Cajero"},
     {"ruta":"/facturas","rol":"Cajero"}
   ]
   ```
5. Usuario navega a `/clientes`
6. PaginaAutenticada verifica:
   - Hay token? Si
   - Es ruta publica? No
   - Esta `/clientes` en rutasRol? Si
   - Permitir acceso
7. Usuario navega a `/productos`
8. PaginaAutenticada verifica:
   - Hay token? Si
   - Es ruta publica? No
   - Esta `/productos` en rutasRol? No
   - Redirigir a `/home`

## Ventajas de esta Implementacion

1. **Soporte multi-rol**: Un usuario puede tener multiples roles y acceder a todas las rutas de todos sus roles
2. **Rendimiento**: Las rutas se cargan una sola vez al hacer login y se guardan en memoria y sessionStorage
3. **Seguridad**: Cada pagina verifica permisos antes de renderizar
4. **Simplicidad**: La verificacion es case-insensitive y maneja errores de forma silenciosa
5. **Mantenibilidad**: Agregar nuevas rutas solo requiere insertar registros en la tabla rutarol

## Consideraciones Tecnicas

### sessionStorage vs localStorage

Se usa `sessionStorage` porque:
- Se borra automaticamente al cerrar la pestana del navegador
- Mayor seguridad: el token no persiste indefinidamente
- Apropiado para sesiones de usuario

### Case-Insensitive Matching

La comparacion de rutas usa `StringComparison.OrdinalIgnoreCase`:
```csharp
r.NombreRuta?.Equals(ruta, StringComparison.OrdinalIgnoreCase)
```

Esto significa que `/Clientes`, `/clientes` y `/CLIENTES` son tratadas como la misma ruta.

### Manejo de Errores

Los bloques try-catch silenciosos:
```csharp
try
{
    // Cargar permisos
}
catch
{
    // Si falla cargar permisos, continuar igual
}
```

Permiten que el login funcione incluso si falla la carga de permisos. En ese caso, el usuario solo tendra acceso a rutas publicas.

## Archivos Modificados

1. `Components/Pages/Login.razor` - Implementa carga de rutas multi-rol
2. `Servicios/ServicioAutenticacion.cs` - Maneja tokens y verificacion de permisos
3. `Components/PaginaAutenticada.cs` - Clase base para paginas protegidas
4. `Servicios/ServicioApiGenerico.cs` - Sin cambios relevantes para RBAC

## Configuracion de Program.cs

Para que el sistema de autenticacion y control de acceso funcione correctamente, el archivo `Program.cs` debe estar configurado de la siguiente manera:

### Servicios Requeridos

```csharp
// 1. Servicios de Razor Components con modo interactivo
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. Configurar circuito de Blazor Server
builder.Services.AddServerSideBlazor(options =>
{
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(30);
    options.DisconnectedCircuitMaxRetained = 100;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
});

// 3. Registrar ServicioAutenticacion (Scoped porque maneja estado de sesion)
builder.Services.AddScoped<FrontendBlazorApi.Servicios.ServicioAutenticacion>();

// 4. Configurar HttpClient para comunicacion con la API
builder.Services.AddHttpClient("ApiGenerica", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});

// 5. Registrar ServicioApiGenerico (Scoped para consistencia con sesion)
builder.Services.AddScoped<FrontendBlazorApi.Servicios.ServicioApiGenerico>();
```

### Pipeline de Middleware Requerido

El orden de los middleware es CRITICO:

```csharp
var app = builder.Build();

// 1. Manejo de excepciones (solo en produccion)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// 2. Redireccion HTTPS
app.UseHttpsRedirection();

// 3. Archivos estaticos (CSS, JS, imagenes)
app.UseStaticFiles();

// 4. IMPORTANTE: Deshabilitar cache del navegador
// Esto evita que el navegador guarde paginas en cache despues de cerrar sesion
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

// 5. Proteccion CSRF
app.UseAntiforgery();

// 6. Mapear componentes con modo interactivo
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// 7. Iniciar aplicacion
app.Run();
```

### Explicacion de Servicios Criticos

#### ServicioAutenticacion (Scoped)

Se registra como `Scoped` porque:
- Mantiene el estado de autenticacion del usuario durante su sesion
- En Blazor Server, un scope = una conexion SignalR = una sesion de usuario
- Cada usuario tiene su propia instancia con su propio token y rutas

```csharp
builder.Services.AddScoped<FrontendBlazorApi.Servicios.ServicioAutenticacion>();
```

#### HttpClient Factory

NUNCA crear HttpClient con `new HttpClient()`. Usar HttpClient Factory:

```csharp
builder.Services.AddHttpClient("ApiGenerica", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
});
```

Ventajas:
- Pool de conexiones HTTP reutilizables
- Gestion automatica del ciclo de vida
- Evita agotamiento de sockets
- Manejo eficiente de DNS

#### Middleware de Cache

CRITICO para seguridad:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});
```

Sin este middleware:
- El navegador podria guardar paginas protegidas en cache
- Despues de cerrar sesion, el boton "Atras" podria mostrar datos sensibles
- El token y las rutas permaneceran en cache

Con este middleware:
- El navegador no guarda cache de ninguna pagina
- Al cerrar sesion, todo se borra
- Mayor seguridad

### Orden del Pipeline

El orden de los middleware es CRITICO:

1. **UseExceptionHandler** - Debe ser primero para capturar todos los errores
2. **UseHttpsRedirection** - Redirigir a HTTPS antes de servir archivos
3. **UseStaticFiles** - Servir archivos estaticos antes del routing
4. **Middleware de Cache** - Deshabilitar cache antes de procesar logica
5. **UseAntiforgery** - Proteccion CSRF antes de endpoints
6. **MapRazorComponents** - Endpoints finales del pipeline

### Configuracion Adicional (Opcional)

#### Para Production con HTTPS

```csharp
builder.Services.AddHttpsRedirection(opciones =>
{
    opciones.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
    opciones.HttpsPort = 443;
});
```

#### Para Timeout de HttpClient

```csharp
builder.Services.AddHttpClient("ApiGenerica", cliente =>
{
    cliente.BaseAddress = new Uri("http://localhost:5031/");
    cliente.Timeout = TimeSpan.FromSeconds(30);
});
```

#### Para Manejo de Errores Personalizado

```csharp
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = app.Environment.IsDevelopment();
});
```

## Proximas Mejoras Posibles

1. Agregar expiracion de token en el frontend
2. Implementar refresh token
3. Agregar cache de rutas con tiempo de expiracion
4. Implementar permisos granulares (lectura/escritura/eliminacion)
5. Agregar logging de intentos de acceso no autorizado
