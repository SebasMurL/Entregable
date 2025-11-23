// =============================================================================
// ServicioApiGenerico.cs
// =============================================================================
// DESCRIPCIÓN:
// Servicio reutilizable y genérico para comunicación HTTP con una API REST.
// Este servicio centraliza toda la lógica de comunicación con el backend,
// incluyendo:
//   - Autenticación mediante tokens JWT almacenados en sessionStorage
//   - Operaciones CRUD genéricas (Create, Read, Update, Delete)
//   - Manejo centralizado de errores HTTP
//   - Soporte para encriptación de campos sensibles
//
// CONCEPTOS CLAVE PARA ESTUDIANTES:
//   1. Inyección de dependencias: Este servicio se registra en Program.cs
//      y se inyecta automáticamente en los componentes Blazor que lo necesitan
//   2. Patrón genérico: Los métodos usan <T> para trabajar con cualquier tipo
//      de entidad sin necesidad de escribir código repetitivo
//   3. HttpClient Factory: Se usa IHttpClientFactory para crear clientes HTTP
//      configurados, evitando problemas de agotamiento de sockets
//   4. Async/Await: Todas las operaciones son asíncronas para no bloquear
//      la interfaz de usuario durante las llamadas al servidor
//   5. JavaScript Interop: Se usa IJSRuntime para acceder a localStorage
//      del navegador desde código C#
// =============================================================================

using System.Net.Http.Json;       // Extensiones para trabajar con JSON (PostAsJsonAsync, GetFromJsonAsync, etc.)
using Microsoft.JSInterop;        // Permite ejecutar código JavaScript desde C# (para acceder a localStorage)
using System.Text.Json;           // Librería moderna de .NET para serialización/deserialización JSON
using System.Net;                 // Contiene la enumeración HttpStatusCode (200, 404, 500, etc.)

namespace FrontendBlazorApi.Servicios
{
    /// <summary>
    /// Servicio genérico para consumir una API REST con soporte para autenticación JWT.
    /// Proporciona métodos CRUD que funcionan con cualquier tipo de entidad del backend.
    /// </summary>
    /// <remarks>
    /// Este servicio debe registrarse como Scoped en Program.cs:
    /// builder.Services.AddScoped&lt;ServicioApiGenerico&gt;();
    ///
    /// Luego se inyecta en componentes Blazor con:
    /// @inject ServicioApiGenerico servicioApi
    /// </remarks>
    public class ServicioApiGenerico
    {
        // =====================================================================
        // SECCIÓN 1: CAMPOS PRIVADOS
        // =====================================================================
        // Los campos privados almacenan las dependencias que necesita el servicio.
        // Se inicializan en el constructor mediante inyección de dependencias.

        /// <summary>
        /// Fábrica de HttpClient configurada en Program.cs.
        /// Permite crear instancias de HttpClient con configuración predefinida
        /// (URL base, timeout, headers por defecto, etc.).
        /// </summary>
        /// <remarks>
        /// IMPORTANTE: Nunca instanciar HttpClient directamente con "new HttpClient()".
        /// Esto puede causar agotamiento de sockets. Siempre usar IHttpClientFactory.
        /// </remarks>
        private readonly IHttpClientFactory _fabricaHttp;

        /// <summary>
        /// Runtime de JavaScript que permite ejecutar código JS desde C#.
        /// Se usa principalmente para acceder a sessionStorage del navegador,
        /// donde se almacena el token JWT de autenticación.
        /// </summary>
        /// <remarks>
        /// sessionStorage es un almacenamiento del navegador que se borra al cerrar la pestaña.
        /// Ejemplo de uso:
        /// await _js.InvokeAsync&lt;string&gt;("sessionStorage.getItem", "token");
        /// </remarks>
        private readonly IJSRuntime _js;

        /// <summary>
        /// Nombre del cliente HTTP configurado en Program.cs.
        /// Debe coincidir exactamente con el nombre usado en:
        /// builder.Services.AddHttpClient("ApiGenerica", ...)
        /// </summary>
        private const string NombreCliente = "ApiGenerica";

        // =====================================================================
        // SECCIÓN 2: CONSTRUCTOR
        // =====================================================================
        // El constructor recibe dependencias automáticamente gracias al
        // contenedor de inyección de dependencias de ASP.NET Core.

        /// <summary>
        /// Constructor del servicio. Recibe las dependencias necesarias mediante
        /// inyección de dependencias.
        /// </summary>
        /// <param name="fabricaHttp">
        /// Fábrica de HttpClient registrada en Program.cs con AddHttpClient.
        /// </param>
        /// <param name="js">
        /// Runtime de JavaScript inyectado automáticamente por Blazor.
        /// Permite interactuar con el navegador (localStorage, alert, console, etc.).
        /// </param>
        /// <remarks>
        /// CONCEPTOS DE INYECCIÓN DE DEPENDENCIAS:
        /// 1. Las dependencias se declaran como parámetros del constructor
        /// 2. ASP.NET Core las resuelve automáticamente del contenedor de servicios
        /// 3. No necesitamos crear las instancias manualmente con "new"
        /// 4. Esto facilita las pruebas unitarias (podemos inyectar mocks)
        /// </remarks>
        public ServicioApiGenerico(IHttpClientFactory fabricaHttp, IJSRuntime js)
        {
            // Asignación de parámetros a campos privados para usarlos en otros métodos
            _fabricaHttp = fabricaHttp;
            _js = js;
        }

        // =====================================================================
        // SECCIÓN 3: MÉTODOS PRIVADOS AUXILIARES
        // =====================================================================
        // Estos métodos no se exponen públicamente. Son utilidades internas
        // para evitar duplicación de código.

        /// <summary>
        /// Crea un HttpClient configurado y le adjunta el token JWT de autenticación
        /// si existe en localStorage del navegador.
        /// </summary>
        /// <returns>
        /// HttpClient listo para hacer peticiones autenticadas a la API.
        /// </returns>
        /// <remarks>
        /// FLUJO DE AUTENTICACIÓN JWT:
        /// 1. El usuario inicia sesión en Login.razor
        /// 2. El backend devuelve un token JWT (JSON Web Token)
        /// 3. El frontend guarda el token en sessionStorage
        /// 4. En cada petición, este método recupera el token y lo agrega como
        ///    header "Authorization: Bearer {token}"
        /// 5. El backend valida el token y autoriza o rechaza la petición
        ///
        /// FORMATO DEL HEADER DE AUTENTICACIÓN:
        /// Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
        ///
        /// El prefijo "Bearer" es un estándar de OAuth 2.0 que indica el tipo
        /// de token que se está enviando.
        /// </remarks>
        private async Task<HttpClient> CrearClienteConTokenAsync()
        {
            // Crear HttpClient con la configuración definida en Program.cs
            // Esto incluye la BaseAddress (URL base de la API)
            var cliente = _fabricaHttp.CreateClient(NombreCliente);

            // Intentar recuperar el token JWT desde sessionStorage del navegador
            // sessionStorage.getItem("token") devuelve null si no existe
            var token = await _js.InvokeAsync<string>("sessionStorage.getItem", "token");

            // Si hay un token válido, agregarlo como header de autenticación
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Crear el header Authorization con el esquema Bearer
                // Formato: "Authorization: Bearer {token}"
                cliente.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            // Devolver el cliente HTTP configurado y autenticado
            return cliente;
        }

        /// <summary>
        /// Valida la respuesta HTTP y lanza una excepción descriptiva si hubo un error.
        /// Centraliza el manejo de errores para todos los métodos del servicio.
        /// </summary>
        /// <param name="respuesta">
        /// Objeto HttpResponseMessage devuelto por cualquier llamada HTTP
        /// (GET, POST, PUT, DELETE).
        /// </param>
        /// <exception cref="Exception">
        /// Se lanza cuando la respuesta indica un error (código de estado 4xx o 5xx).
        /// El mensaje de la excepción describe el problema en español.
        /// </exception>
        /// <remarks>
        /// CÓDIGOS DE ESTADO HTTP COMUNES:
        /// - 2xx: Éxito
        ///   - 200 OK: Petición exitosa con datos en el cuerpo
        ///   - 201 Created: Recurso creado exitosamente
        ///   - 204 No Content: Éxito sin datos en el cuerpo
        ///
        /// - 4xx: Error del cliente
        ///   - 400 Bad Request: Datos enviados incorrectos o inválidos
        ///   - 401 Unauthorized: No autenticado (token inválido o ausente)
        ///   - 403 Forbidden: Autenticado pero sin permisos
        ///   - 404 Not Found: Recurso no existe
        ///
        /// - 5xx: Error del servidor
        ///   - 500 Internal Server Error: Error inesperado en el backend
        ///   - 503 Service Unavailable: Servidor no disponible
        ///
        /// Este método convierte códigos HTTP en mensajes amigables para el usuario.
        /// </remarks>
        private async Task LanzarSiError(HttpResponseMessage respuesta)
        {
            // Si la respuesta es exitosa (código 2xx), no hacer nada
            if (respuesta.IsSuccessStatusCode)
                return;

            // Variable para almacenar detalles del error devueltos por el backend
            string detalle = "";

            try
            {
                // Intentar leer el error en formato JSON estructurado
                // El backend puede devolver: { "Estado": 400, "Mensaje": "Error..." }
                var error = await respuesta.Content.ReadFromJsonAsync<ApiError>();
                detalle = error?.Mensaje ?? "";
            }
            catch
            {
                // Si falla la deserialización JSON, leer como texto plano
                detalle = await respuesta.Content.ReadAsStringAsync();
            }

            // Construir mensaje de error según el código de estado HTTP
            // Se usa "switch expression" (sintaxis moderna de C# 8.0+)
            string mensaje = respuesta.StatusCode switch
            {
                // 400: El servidor no pudo procesar la petición (validación falló, datos incorrectos)
                HttpStatusCode.BadRequest => $"Solicitud incorrecta (400). {detalle}",

                // 401: Token inválido, expirado o ausente
                HttpStatusCode.Unauthorized => "Acceso no autorizado. Verifique sus credenciales o el token.",

                // 403: Token válido pero el usuario no tiene permisos para esta acción
                HttpStatusCode.Forbidden => "Acceso denegado. No tiene permisos suficientes.",

                // 404: La URL solicitada no existe en el servidor
                HttpStatusCode.NotFound => "Recurso no encontrado en el servidor.",

                // 500: Error interno del backend (excepción no controlada, error de BD, etc.)
                HttpStatusCode.InternalServerError => "Error interno en el servidor.",

                // Cualquier otro código de estado
                _ => $"Error inesperado ({(int)respuesta.StatusCode}). {detalle}"
            };

            // Lanzar excepción con el mensaje construido
            // Esta excepción será capturada en los bloques catch de los componentes
            throw new Exception(mensaje);
        }

        // =====================================================================
        // SECCIÓN 4: MÉTODOS PÚBLICOS - OPERACIONES CRUD
        // =====================================================================
        // Estos métodos implementan las operaciones básicas de bases de datos:
        // Create (Crear), Read (Leer), Update (Actualizar), Delete (Eliminar).
        // Son genéricos (<T>) para funcionar con cualquier tipo de entidad.

        // ---------------------------------------------------------------------
        // OPERACIONES SIMPLES GENÉRICAS (PARA LOGIN Y RUTASROL)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Realiza una petición GET simple a cualquier endpoint.
        /// </summary>
        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var cliente = await CrearClienteConTokenAsync();
            var respuesta = await cliente.GetAsync(endpoint);
            await LanzarSiError(respuesta);
            return await respuesta.Content.ReadFromJsonAsync<T>();
        }

        /// <summary>
        /// Realiza una petición POST simple a cualquier endpoint.
        /// </summary>
        public async Task<T?> PostAsync<T>(string endpoint, object datos)
        {
            var cliente = await CrearClienteConTokenAsync();
            var respuesta = await cliente.PostAsJsonAsync(endpoint, datos);
            await LanzarSiError(respuesta);
            return await respuesta.Content.ReadFromJsonAsync<T>();
        }

        // ---------------------------------------------------------------------
        // OPERACIÓN: READ (LEER TODOS)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Obtiene todos los registros de una tabla del backend.
        /// Corresponde a una petición GET a /api/{tabla}
        /// </summary>
        /// <typeparam name="T">
        /// Tipo de entidad a recuperar (Producto, Cliente, Factura, etc.).
        /// Debe tener propiedades que coincidan con el JSON del backend.
        /// </typeparam>
        /// <param name="tabla">
        /// Nombre de la tabla o endpoint en el backend (ej: "producto", "cliente").
        /// Se usará en la URL: /api/{tabla}
        /// </param>
        /// <returns>
        /// Lista de objetos del tipo T. Si no hay datos, devuelve lista vacía (nunca null).
        /// </returns>
        /// <remarks>
        /// EJEMPLO DE USO:
        /// var productos = await servicioApi.ObtenerTodosAsync&lt;Producto&gt;("producto");
        ///
        /// FLUJO DE EJECUCIÓN:
        /// 1. Se crea un HttpClient con token JWT
        /// 2. Se hace GET a http://localhost:5031/api/producto
        /// 3. El backend devuelve JSON: { "estado": 200, "datos": [...] }
        /// 4. Se deserializa el JSON a ApiRespuesta&lt;List&lt;Producto&gt;&gt;
        /// 5. Se extrae y devuelve la propiedad "Datos"
        ///
        /// FORMATO JSON ESPERADO DEL BACKEND:
        /// {
        ///   "estado": 200,
        ///   "mensaje": "Consulta exitosa",
        ///   "datos": [
        ///     { "codigo": "PR001", "nombre": "Laptop", "stock": 10, "valorUnitario": 1500 },
        ///     { "codigo": "PR002", "nombre": "Mouse", "stock": 50, "valorUnitario": 25 }
        ///   ]
        /// }
        /// </remarks>
        public async Task<List<T>> ObtenerTodosAsync<T>(string tabla)
        {
            // Crear cliente HTTP autenticado con el token JWT
            var cliente = await CrearClienteConTokenAsync();

            // Realizar petición GET a /api/{tabla}
            // Ejemplo: GET http://localhost:5031/api/producto
            var respuesta = await cliente.GetAsync($"api/{tabla}");

            // Validar que no hubo errores (lanza excepción si código es 4xx o 5xx)
            await LanzarSiError(respuesta);

            // Deserializar la respuesta JSON a un objeto ApiRespuesta<List<T>>
            // El backend envuelve los datos en esta estructura estándar
            var resultado = await respuesta.Content.ReadFromJsonAsync<ApiRespuesta<List<T>>>();

            // Devolver la lista de datos, o lista vacía si es null
            // Usar ?? (null-coalescing operator) garantiza que nunca devolvemos null
            return resultado?.Datos ?? new List<T>();
        }

        // ---------------------------------------------------------------------
        // OPERACIÓN: READ (LEER UNO)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Obtiene un único registro de una tabla buscando por su clave primaria.
        /// Corresponde a una petición GET a /api/{tabla}/{campoClave}/{valor}
        /// </summary>
        /// <typeparam name="T">
        /// Tipo de entidad a recuperar.
        /// </typeparam>
        /// <param name="tabla">
        /// Nombre de la tabla o endpoint (ej: "producto").
        /// </param>
        /// <param name="campoClave">
        /// Nombre del campo de la clave primaria (ej: "codigo", "id", "email").
        /// </param>
        /// <param name="valor">
        /// Valor de la clave primaria a buscar (ej: "PR001", 5, "usuario@email.com").
        /// Se convierte automáticamente a string en la URL.
        /// </param>
        /// <returns>
        /// Objeto del tipo T si se encuentra, o null si no existe.
        /// </returns>
        /// <remarks>
        /// EJEMPLO DE USO:
        /// var producto = await servicioApi.ObtenerPorClaveAsync&lt;Producto&gt;("producto", "codigo", "PR001");
        ///
        /// PETICIÓN GENERADA:
        /// GET http://localhost:5031/api/producto/codigo/PR001
        ///
        /// FORMATO JSON ESPERADO:
        /// {
        ///   "estado": 200,
        ///   "mensaje": "Producto encontrado",
        ///   "datos": { "codigo": "PR001", "nombre": "Laptop", "stock": 10, "valorUnitario": 1500 }
        /// }
        ///
        /// Si no se encuentra, el backend devuelve 404 y este método lanza excepción.
        /// </remarks>
        public async Task<T?> ObtenerPorClaveAsync<T>(string tabla, string campoClave, object valor)
        {
            // Crear cliente autenticado
            var cliente = await CrearClienteConTokenAsync();

            // Petición GET con parámetros en la URL
            // Ejemplo: GET /api/producto/codigo/PR001
            var respuesta = await cliente.GetAsync($"api/{tabla}/{campoClave}/{valor}");

            // Validar respuesta (puede lanzar excepción si es 404 o error)
            await LanzarSiError(respuesta);

            // Deserializar respuesta a ApiRespuesta<T>
            var resultado = await respuesta.Content.ReadFromJsonAsync<ApiRespuesta<T>>();

            // Devolver el dato o default(T) si es null
            // Para tipos referencia, default(T) es null
            // Para tipos valor (int, DateTime), default(T) es el valor por defecto (0, DateTime.MinValue)
            return resultado == null ? default : resultado.Datos;
        }

        // ---------------------------------------------------------------------
        // OPERACIÓN: CREATE (CREAR)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Crea un nuevo registro en la tabla del backend.
        /// Corresponde a una petición POST a /api/{tabla}
        /// </summary>
        /// <typeparam name="T">
        /// Tipo de entidad a crear. Debe ser serializable a JSON.
        /// </typeparam>
        /// <param name="tabla">
        /// Nombre de la tabla o endpoint (ej: "producto").
        /// </param>
        /// <param name="entidad">
        /// Objeto con los datos a insertar. Se enviará como JSON en el cuerpo de la petición.
        /// </param>
        /// <returns>
        /// Mensaje de éxito si se creó correctamente.
        /// </returns>
        /// <remarks>
        /// EJEMPLO DE USO:
        /// var nuevoProducto = new Producto {
        ///     Codigo = "PR003",
        ///     Nombre = "Teclado",
        ///     Stock = 30,
        ///     ValorUnitario = 45
        /// };
        /// await servicioApi.CrearAsync("producto", nuevoProducto);
        ///
        /// PETICIÓN GENERADA:
        /// POST http://localhost:5031/api/producto
        /// Content-Type: application/json
        ///
        /// CUERPO JSON:
        /// {
        ///   "codigo": "PR003",
        ///   "nombre": "Teclado",
        ///   "stock": 30,
        ///   "valorUnitario": 45
        /// }
        ///
        /// El backend inserta el registro en la base de datos y devuelve:
        /// {
        ///   "estado": 201,
        ///   "mensaje": "Producto creado exitosamente"
        /// }
        /// </remarks>
        public async Task<string> CrearAsync<T>(string tabla, T entidad)
        {
            // Crear cliente autenticado
            var cliente = await CrearClienteConTokenAsync();

            // Petición POST con el objeto serializado a JSON
            // PostAsJsonAsync automáticamente:
            // 1. Serializa el objeto a JSON
            // 2. Establece Content-Type: application/json
            // 3. Envía el JSON en el cuerpo de la petición
            var respuesta = await cliente.PostAsJsonAsync($"api/{tabla}", entidad);

            // Validar respuesta
            await LanzarSiError(respuesta);

            // Devolver mensaje de éxito
            return "Registro creado correctamente.";
        }

        // ---------------------------------------------------------------------
        // OPERACIÓN: CREATE CON ENCRIPTACIÓN (SOBRECARGA)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Crea un nuevo registro en la tabla del backend con encriptación de campos sensibles.
        /// Es una sobrecarga del método CrearAsync que agrega soporte para encriptar campos
        /// como contraseñas antes de insertarlos en la base de datos.
        /// </summary>
        /// <typeparam name="T">
        /// Tipo de entidad a crear.
        /// </typeparam>
        /// <param name="tabla">
        /// Nombre de la tabla o endpoint (ej: "usuario").
        /// </param>
        /// <param name="entidad">
        /// Objeto con los datos a insertar.
        /// </param>
        /// <param name="camposEncriptar">
        /// Nombres de los campos que deben encriptarse en el backend, separados por comas.
        /// Ejemplo: "contrasena" o "contrasena,pin"
        /// </param>
        /// <returns>
        /// Mensaje de éxito si se creó correctamente.
        /// </returns>
        /// <remarks>
        /// EJEMPLO DE USO:
        /// var nuevoUsuario = new Usuario {
        ///     Email = "nuevo@ejemplo.com",
        ///     Contrasena = "MiPassword123"
        /// };
        /// await servicioApi.CrearAsync("usuario", nuevoUsuario, "contrasena");
        ///
        /// PETICIÓN GENERADA:
        /// POST http://localhost:5031/api/usuario?camposEncriptar=contrasena
        ///
        /// CUERPO JSON:
        /// {
        ///   "email": "nuevo@ejemplo.com",
        ///   "contrasena": "MiPassword123"
        /// }
        ///
        /// PROCESAMIENTO EN EL BACKEND:
        /// 1. El backend recibe el parámetro ?camposEncriptar=contrasena
        /// 2. Lee el campo "contrasena" del JSON: "MiPassword123"
        /// 3. Lo encripta con BCrypt: "$2a$11$..."
        /// 4. Inserta en la BD el hash en lugar del texto plano
        /// 5. El usuario queda registrado con contraseña segura
        ///
        /// SEGURIDAD:
        /// - Nunca se almacenan contraseñas en texto plano
        /// - BCrypt es un algoritmo de hash unidireccional (no se puede desencriptar)
        /// - Cada hash incluye un "salt" aleatorio para evitar ataques de rainbow tables
        /// </remarks>
        public async Task<string> CrearAsync<T>(string tabla, T entidad, string camposEncriptar)
        {
            // Crear cliente autenticado
            var cliente = await CrearClienteConTokenAsync();

            // Construir URL con parámetro de query string
            // Ejemplo: /api/usuario?camposEncriptar=contrasena
            var url = $"api/{tabla}?camposEncriptar={camposEncriptar}";

            // Petición POST con JSON en el cuerpo
            var respuesta = await cliente.PostAsJsonAsync(url, entidad);

            // Validar respuesta
            await LanzarSiError(respuesta);

            // Devolver mensaje de éxito
            return "Registro creado correctamente.";
        }

        // ---------------------------------------------------------------------
        // OPERACIÓN: UPDATE (ACTUALIZAR)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Actualiza un registro existente en la tabla del backend.
        /// Corresponde a una petición PUT a /api/{tabla}/{campoClave}/{valor}
        /// </summary>
        /// <typeparam name="T">
        /// Tipo de entidad a actualizar.
        /// </typeparam>
        /// <param name="tabla">
        /// Nombre de la tabla o endpoint (ej: "producto").
        /// </param>
        /// <param name="campoClave">
        /// Nombre del campo de la clave primaria (ej: "codigo").
        /// </param>
        /// <param name="valor">
        /// Valor de la clave primaria del registro a actualizar (ej: "PR001").
        /// </param>
        /// <param name="entidad">
        /// Objeto con los nuevos datos. Se enviará como JSON en el cuerpo.
        /// </param>
        /// <returns>
        /// Mensaje de éxito si se actualizó correctamente.
        /// </returns>
        /// <remarks>
        /// EJEMPLO DE USO:
        /// var productoModificado = new Producto {
        ///     Codigo = "PR001",
        ///     Nombre = "Laptop Actualizada",
        ///     Stock = 15,
        ///     ValorUnitario = 1600
        /// };
        /// await servicioApi.ActualizarAsync("producto", "codigo", "PR001", productoModificado);
        ///
        /// PETICIÓN GENERADA:
        /// PUT http://localhost:5031/api/producto/codigo/PR001
        /// Content-Type: application/json
        ///
        /// CUERPO JSON:
        /// {
        ///   "codigo": "PR001",
        ///   "nombre": "Laptop Actualizada",
        ///   "stock": 15,
        ///   "valorUnitario": 1600
        /// }
        ///
        /// El backend ejecuta un UPDATE en la base de datos y devuelve:
        /// {
        ///   "estado": 200,
        ///   "mensaje": "Producto actualizado exitosamente"
        /// }
        ///
        /// DIFERENCIA ENTRE PUT Y PATCH:
        /// - PUT: Reemplaza completamente el recurso (todos los campos)
        /// - PATCH: Actualiza solo campos específicos (parcial)
        /// Este método usa PUT, por lo que deben enviarse todos los campos.
        /// </remarks>
        public async Task<string> ActualizarAsync<T>(string tabla, string campoClave, object valor, T entidad)
        {
            // Crear cliente autenticado
            var cliente = await CrearClienteConTokenAsync();

            // Petición PUT con la clave del registro en la URL
            // Ejemplo: PUT /api/producto/codigo/PR001
            var respuesta = await cliente.PutAsJsonAsync($"api/{tabla}/{campoClave}/{valor}", entidad);

            // Validar respuesta
            await LanzarSiError(respuesta);

            // Devolver mensaje de éxito
            return "Registro actualizado correctamente.";
        }

        // ---------------------------------------------------------------------
        // OPERACIÓN: UPDATE CON ENCRIPTACIÓN (SOBRECARGA)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Actualiza un registro existente con soporte para encriptar campos sensibles.
        /// Es una sobrecarga del método ActualizarAsync.
        /// </summary>
        /// <typeparam name="T">
        /// Tipo de entidad a actualizar.
        /// </typeparam>
        /// <param name="tabla">
        /// Nombre de la tabla o endpoint (ej: "usuario").
        /// </param>
        /// <param name="campoClave">
        /// Nombre del campo de la clave primaria (ej: "email").
        /// </param>
        /// <param name="valor">
        /// Valor de la clave primaria (ej: "usuario@ejemplo.com").
        /// </param>
        /// <param name="entidad">
        /// Objeto con los nuevos datos.
        /// </param>
        /// <param name="camposEncriptar">
        /// Campos a encriptar, separados por comas (ej: "contrasena").
        /// </param>
        /// <returns>
        /// Mensaje de éxito si se actualizó correctamente.
        /// </returns>
        /// <remarks>
        /// EJEMPLO DE USO PARA CAMBIAR CONTRASEÑA:
        /// var usuarioModificado = new Usuario {
        ///     Email = "usuario@ejemplo.com",
        ///     Contrasena = "NuevaPassword456"
        /// };
        /// await servicioApi.ActualizarAsync("usuario", "email", "usuario@ejemplo.com",
        ///                                     usuarioModificado, "contrasena");
        ///
        /// PETICIÓN GENERADA:
        /// PUT http://localhost:5031/api/usuario/email/usuario@ejemplo.com?camposEncriptar=contrasena
        ///
        /// El backend encripta "NuevaPassword456" antes de actualizar la base de datos.
        ///
        /// CASO DE USO - NO CAMBIAR CONTRASEÑA:
        /// Si el usuario NO quiere cambiar su contraseña, se envía la entidad sin el campo
        /// de contraseña, o con el campo vacío. El backend debe detectar esto y no tocar
        /// el campo de contraseña en la base de datos.
        /// </remarks>
        public async Task<string> ActualizarAsync<T>(string tabla, string campoClave, object valor, T entidad, string camposEncriptar)
        {
            // Crear cliente autenticado
            var cliente = await CrearClienteConTokenAsync();

            // Construir URL con parámetros de clave y campos a encriptar
            // Ejemplo: /api/usuario/email/usuario@ejemplo.com?camposEncriptar=contrasena
            var url = $"api/{tabla}/{campoClave}/{valor}?camposEncriptar={camposEncriptar}";

            // Petición PUT con JSON en el cuerpo
            var respuesta = await cliente.PutAsJsonAsync(url, entidad);

            // Validar respuesta
            await LanzarSiError(respuesta);

            // Devolver mensaje de éxito
            return "Registro actualizado correctamente.";
        }

        // ---------------------------------------------------------------------
        // OPERACIÓN: DELETE (ELIMINAR)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Elimina un registro de la tabla del backend.
        /// Corresponde a una petición DELETE a /api/{tabla}/{campoClave}/{valor}
        /// </summary>
        /// <param name="tabla">
        /// Nombre de la tabla o endpoint (ej: "producto").
        /// </param>
        /// <param name="campoClave">
        /// Nombre del campo de la clave primaria (ej: "codigo").
        /// </param>
        /// <param name="valor">
        /// Valor de la clave primaria del registro a eliminar (ej: "PR001").
        /// </param>
        /// <returns>
        /// Mensaje de éxito si se eliminó correctamente.
        /// </returns>
        /// <remarks>
        /// EJEMPLO DE USO:
        /// await servicioApi.EliminarAsync("producto", "codigo", "PR001");
        ///
        /// PETICIÓN GENERADA:
        /// DELETE http://localhost:5031/api/producto/codigo/PR001
        ///
        /// El backend ejecuta un DELETE en la base de datos y devuelve:
        /// {
        ///   "estado": 200,
        ///   "mensaje": "Producto eliminado exitosamente"
        /// }
        ///
        /// CONSIDERACIONES IMPORTANTES:
        /// 1. La eliminación es permanente (no recuperable)
        /// 2. Si hay registros relacionados (claves foráneas), el backend puede:
        ///    - Rechazar la eliminación (Restrict)
        ///    - Eliminar en cascada (Cascade)
        ///    - Establecer null en las FK (Set Null)
        /// 3. Siempre confirmar con el usuario antes de eliminar (dialog de confirmación)
        /// 4. Si el registro no existe, el backend devuelve 404 y este método lanza excepción
        /// </remarks>
        public async Task<string> EliminarAsync(string tabla, string campoClave, object valor)
        {
            // Crear cliente autenticado
            var cliente = await CrearClienteConTokenAsync();

            // Petición DELETE con la clave del registro en la URL
            // Ejemplo: DELETE /api/producto/codigo/PR001
            var respuesta = await cliente.DeleteAsync($"api/{tabla}/{campoClave}/{valor}");

            // Validar respuesta
            await LanzarSiError(respuesta);

            // Devolver mensaje de éxito
            return "Registro eliminado correctamente.";
        }

        // ---------------------------------------------------------------------
        // OPERACIÓN: EJECUTAR STORED PROCEDURE
        // ---------------------------------------------------------------------

        /// <summary>
        /// Ejecuta un stored procedure (procedimiento almacenado) en el backend.
        /// Se usa para operaciones complejas que requieren transacciones o lógica especial.
        /// </summary>
        /// <typeparam name="T">
        /// Tipo de objeto con los parámetros del stored procedure.
        /// </typeparam>
        /// <param name="nombreSP">
        /// Nombre del stored procedure a ejecutar (ej: "crear_usuario_con_roles").
        /// </param>
        /// <param name="parametros">
        /// Objeto con los parámetros que el SP necesita.
        /// Las propiedades deben coincidir con los nombres de parámetros del SP.
        /// </param>
        /// <returns>
        /// Mensaje de éxito devuelto por el backend.
        /// </returns>
        /// <remarks>
        /// EJEMPLO DE USO PARA CREAR USUARIO CON ROLES:
        /// var parametros = new {
        ///     email = "usuario@ejemplo.com",
        ///     contrasena = "MiPassword123",
        ///     roles_json = "[{\"fkidrol\":1},{\"fkidrol\":2}]"
        /// };
        /// await servicioApi.EjecutarStoredProcedureAsync("crear_usuario_con_roles", parametros);
        ///
        /// PETICIÓN GENERADA:
        /// POST http://localhost:5031/api/procedimientos/ejecutarsp
        ///
        /// CUERPO JSON:
        /// {
        ///   "nombreSP": "crear_usuario_con_roles",
        ///   "email": "usuario@ejemplo.com",
        ///   "contrasena": "MiPassword123",
        ///   "roles_json": "[{\"fkidrol\":1},{\"fkidrol\":2}]"
        /// }
        ///
        /// El backend:
        /// 1. Recibe el nombreSP y los parámetros
        /// 2. Ejecuta el SP en la base de datos (con EXEC)
        /// 3. El SP realiza las operaciones en transacción
        /// 4. Devuelve éxito o error
        ///
        /// VENTAJAS DE LOS STORED PROCEDURES:
        /// - Transacciones: Todas las operaciones se ejecutan o ninguna
        /// - Rendimiento: Código compilado en la base de datos
        /// - Seguridad: Lógica compleja en el servidor
        /// - Mantenibilidad: Cambios en la BD sin tocar el código
        ///
        /// IMPORTANTE:
        /// El backend debe tener un endpoint para SPs en:
        /// POST /api/procedimientos/ejecutarsp
        /// </remarks>
        public async Task<string> EjecutarStoredProcedureAsync<T>(string nombreSP, T parametros)
        {
            // Crear cliente autenticado
            var cliente = await CrearClienteConTokenAsync();

            // Convertir el objeto parametros a un diccionario
            var parametrosDict = new Dictionary<string, object?>();

            // Agregar el nombre del SP como primer parámetro
            parametrosDict["nombreSP"] = nombreSP;

            // Agregar todos los parámetros del objeto T al diccionario
            var propiedades = typeof(T).GetProperties();
            foreach (var propiedad in propiedades)
            {
                var valor = propiedad.GetValue(parametros);
                parametrosDict[propiedad.Name] = valor;
            }

            // Petición POST al endpoint de procedimientos almacenados
            // Ejemplo: POST /api/procedimientos/ejecutarsp
            var respuesta = await cliente.PostAsJsonAsync("api/procedimientos/ejecutarsp", parametrosDict);

            // Validar respuesta
            await LanzarSiError(respuesta);

            // Devolver mensaje de éxito
            return "Stored procedure ejecutado correctamente.";
        }

        /// <summary>
        /// Ejecuta un stored procedure y devuelve los resultados completos.
        /// Similar a EjecutarStoredProcedureAsync pero devuelve los datos en lugar de solo un mensaje.
        /// </summary>
        public async Task<RespuestaSP> EjecutarStoredProcedureConResultadosAsync<T>(string nombreSP, T parametros)
        {
            // Crear cliente autenticado
            var cliente = await CrearClienteConTokenAsync();

            // Convertir el objeto parametros a un diccionario
            var parametrosDict = new Dictionary<string, object?>();

            // Agregar el nombre del SP como primer parámetro
            parametrosDict["nombreSP"] = nombreSP;

            // Agregar todos los parámetros del objeto T al diccionario
            var propiedades = typeof(T).GetProperties();
            foreach (var propiedad in propiedades)
            {
                var valor = propiedad.GetValue(parametros);
                parametrosDict[propiedad.Name] = valor;
            }

            // Petición POST al endpoint de procedimientos almacenados
            var respuesta = await cliente.PostAsJsonAsync("api/procedimientos/ejecutarsp", parametrosDict);

            // Validar respuesta
            await LanzarSiError(respuesta);

            // Deserializar y devolver resultados completos
            var resultado = await respuesta.Content.ReadFromJsonAsync<RespuestaSP>();
            return resultado ?? new RespuestaSP();
        }

        // ---------------------------------------------------------------------
        // OPERACIÓN: EJECUTAR STORED PROCEDURE CON ENCRIPTACIÓN (SOBRECARGA)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Ejecuta un stored procedure con soporte para encriptar campos sensibles.
        /// Es una sobrecarga que agrega el parámetro camposEncriptar.
        /// </summary>
        /// <typeparam name="T">Tipo de objeto con los parámetros del stored procedure.</typeparam>
        /// <param name="nombreSP">Nombre del stored procedure a ejecutar.</param>
        /// <param name="parametros">Objeto con los parámetros que el SP necesita.</param>
        /// <param name="camposEncriptar">Campos a encriptar, separados por comas (ej: "p_contrasena").</param>
        /// <returns>Mensaje de éxito devuelto por el backend.</returns>
        public async Task<string> EjecutarStoredProcedureAsync<T>(string nombreSP, T parametros, string camposEncriptar)
        {
            // Crear cliente autenticado
            var cliente = await CrearClienteConTokenAsync();

            // Convertir el objeto parametros a un diccionario
            var parametrosDict = new Dictionary<string, object?>();

            // Agregar el nombre del SP como primer parámetro
            parametrosDict["nombreSP"] = nombreSP;

            // Agregar todos los parámetros del objeto T al diccionario
            // Detectar si T es un Dictionary o un objeto anónimo
            if (parametros is Dictionary<string, object?> dict)
            {
                // Si ya es un diccionario, copiar los elementos
                foreach (var kvp in dict)
                {
                    parametrosDict[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                // Si es un objeto anónimo, usar reflexión
                var propiedades = typeof(T).GetProperties();
                foreach (var propiedad in propiedades)
                {
                    var valor = propiedad.GetValue(parametros);
                    parametrosDict[propiedad.Name] = valor;
                }
            }

            // Construir URL con parámetro de encriptación
            var url = $"api/procedimientos/ejecutarsp?camposEncriptar={camposEncriptar}";

            // Petición POST al endpoint de procedimientos almacenados
            var respuesta = await cliente.PostAsJsonAsync(url, parametrosDict);

            // Validar respuesta
            await LanzarSiError(respuesta);

            // Devolver mensaje de éxito
            return "Stored procedure ejecutado correctamente.";
        }

        // =====================================================================
        // SECCIÓN 5: CLASES AUXILIARES PRIVADAS (DTOs)
        // =====================================================================
        // Estas clases representan la estructura JSON de las respuestas del backend.
        // Se usan solo internamente en este servicio para deserializar respuestas.

        /// <summary>
        /// Representa la estructura estándar de respuesta del backend.
        /// El backend envuelve todos los datos en este formato consistente.
        /// </summary>
        /// <typeparam name="T">
        /// Tipo de datos que contiene la respuesta (puede ser un objeto o una lista).
        /// </typeparam>
        /// <remarks>
        /// EJEMPLO DE JSON DESERIALIZADO A ESTA CLASE:
        /// {
        ///   "estado": 200,
        ///   "mensaje": "Operación exitosa",
        ///   "datos": { "codigo": "PR001", "nombre": "Laptop" }
        /// }
        ///
        /// VENTAJAS DE ESTE FORMATO:
        /// 1. Consistencia: Todas las respuestas tienen la misma estructura
        /// 2. Metadatos: Incluye código de estado y mensaje descriptivo
        /// 3. Tipado: El campo "datos" puede ser de cualquier tipo (genérico)
        /// 4. Facilita manejo de errores: Siempre sabemos dónde buscar mensajes de error
        /// </remarks>
        private class ApiRespuesta<T>
        {
            /// <summary>
            /// Código de estado HTTP (200, 201, 400, 404, 500, etc.).
            /// </summary>
            public int Estado { get; set; }

            /// <summary>
            /// Mensaje descriptivo del resultado de la operación.
            /// </summary>
            public string? Mensaje { get; set; }

            /// <summary>
            /// Datos devueltos por el backend (puede ser un objeto, lista o null).
            /// </summary>
            public T? Datos { get; set; }
        }

        /// <summary>
        /// Representa la estructura de un mensaje de error devuelto por el backend.
        /// Se usa cuando ocurre un error y necesitamos leer el detalle.
        /// </summary>
        /// <remarks>
        /// EJEMPLO DE JSON DE ERROR:
        /// {
        ///   "estado": 400,
        ///   "mensaje": "El campo 'nombre' es obligatorio"
        /// }
        ///
        /// Este formato se usa cuando el backend rechaza una petición por:
        /// - Validación de datos fallida
        /// - Autenticación incorrecta
        /// - Permisos insuficientes
        /// - Error de lógica de negocio
        /// </remarks>
        private class ApiError
        {
            /// <summary>
            /// Código de estado HTTP del error.
            /// </summary>
            public int Estado { get; set; }

            /// <summary>
            /// Mensaje descriptivo del error en español.
            /// </summary>
            public string? Mensaje { get; set; }
        }
    }

    /// <summary>
    /// Representa la respuesta al ejecutar un procedimiento almacenado.
    /// Esta clase es pública para que pueda ser usada por los componentes Razor.
    /// </summary>
    public class RespuestaSP
    {
        public string? Procedimiento { get; set; }
        public List<Dictionary<string, object>>? Resultados { get; set; }
        public int Total { get; set; }
        public string? Mensaje { get; set; }
    }
}

// =============================================================================
// FIN DEL ARCHIVO ServicioApiGenerico.cs
// =============================================================================
// RESUMEN DE CONCEPTOS PARA ESTUDIANTES:
//
// 1. INYECCIÓN DE DEPENDENCIAS:
//    - No crear instancias con "new" en el constructor
//    - Declarar dependencias como parámetros del constructor
//    - ASP.NET Core las resuelve automáticamente
//
// 2. PROGRAMACIÓN ASÍNCRONA:
//    - Todos los métodos usan async/await
//    - Permite que la UI siga respondiendo durante llamadas HTTP
//    - Las tareas largas no bloquean el hilo principal
//
// 3. GENÉRICOS (<T>):
//    - Permiten escribir código reutilizable para cualquier tipo
//    - Evitan duplicación de código (DRY: Don't Repeat Yourself)
//    - Mantienen el tipado fuerte (type-safe)
//
// 4. PATRONES HTTP:
//    - GET: Obtener datos (ObtenerTodosAsync, ObtenerPorClaveAsync)
//    - POST: Crear nuevo recurso (CrearAsync)
//    - PUT: Actualizar recurso completo (ActualizarAsync)
//    - DELETE: Eliminar recurso (EliminarAsync)
//
// 5. AUTENTICACIÓN JWT:
//    - Token se almacena en localStorage del navegador
//    - Se envía en cada petición como header "Authorization: Bearer {token}"
//    - El backend valida el token y autoriza o rechaza la petición
//
// 6. MANEJO DE ERRORES:
//    - Centralizado en el método LanzarSiError
//    - Convierte códigos HTTP en mensajes amigables
//    - Propaga excepciones a los componentes para mostrar al usuario
//
// 7. SERIALIZACIÓN JSON:
//    - Conversión automática entre objetos C# y JSON
//    - PostAsJsonAsync serializa objetos a JSON
//    - ReadFromJsonAsync deserializa JSON a objetos C#
//
// 8. SOBRECARGA DE MÉTODOS:
//    - Dos versiones del mismo método con diferentes parámetros
//    - Permite funcionalidad adicional sin romper código existente
//    - Ejemplo: CrearAsync con y sin parámetro de encriptación
// =============================================================================
