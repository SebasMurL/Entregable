// PoliticaTablasProhibidasDesdeJson.cs — Implementación que lee tablas prohibidas desde appsettings.json
// Ubicación: Servicios/Politicas/PoliticaTablasProhibidasDesdeJson.cs
//
// Principios SOLID aplicados:
// - SRP: Solo se encarga de leer configuración JSON y validar tablas
// - DIP: Implementa IPoliticaTablasProhibidas, permitiendo intercambiarla
// - OCP: Cerrada para modificación, abierta para extensión (podemos crear otras implementaciones)
// - LSP: Puede sustituir a IPoliticaTablasProhibidas sin problemas

using Microsoft.Extensions.Configuration;     // Para leer appsettings.json
using System;                                  // Para StringComparer
using System.Collections.Generic;              // Para HashSet
using System.Linq;                             // Para operaciones LINQ
using webapicsharp.Servicios.Abstracciones;   // Para IPoliticaTablasProhibidas

namespace webapicsharp.Servicios.Politicas
{
    /// <summary>
    /// Implementación concreta que lee la lista de tablas prohibidas desde appsettings.json.
    ///
    /// RESPONSABILIDAD ÚNICA (SRP):
    /// Esta clase tiene una sola responsabilidad: leer la configuración de tablas prohibidas
    /// desde archivos JSON y determinar si una tabla específica está permitida.
    ///
    /// CONFIGURACIÓN ESPERADA EN appsettings.json:
    /// {
    ///   "TablasProhibidas": ["usuarios", "contrasenas", "sys_config", "auditoria"]
    /// }
    ///
    /// ESTRATEGIA DE VALIDACIÓN:
    /// - Si la lista está vacía o no existe: TODAS las tablas están permitidas
    /// - Si la lista contiene tablas: Solo se PROHÍBEN las tablas listadas
    /// - La comparación es case-insensitive: "Usuarios" = "usuarios" = "USUARIOS"
    ///
    /// VENTAJAS DE ESTA IMPLEMENTACIÓN:
    /// 1. Configuración centralizada en appsettings.json
    /// 2. No requiere recompilar para cambiar tablas prohibidas
    /// 3. Fácil de entender y mantener
    /// 4. Performance optimizado usando HashSet (búsqueda O(1))
    ///
    /// ARQUITECTURA:
    /// Program.cs → Registra esta clase
    ///     ↓
    /// ServicioCrud → Recibe IPoliticaTablasProhibidas
    ///     ↓
    /// Antes de cada operación → Llama a EsTablaPermitida()
    ///     ↓
    /// Esta clase → Consulta el HashSet en memoria
    ///     ↓
    /// Retorna true/false → ServicioCrud decide si continúa o lanza excepción
    /// </summary>
    public class PoliticaTablasProhibidasDesdeJson : IPoliticaTablasProhibidas
    {
        // Campo privado que almacena las tablas prohibidas en un HashSet para búsqueda rápida
        // HashSet proporciona búsqueda O(1) vs List que sería O(n)
        // StringComparer.OrdinalIgnoreCase hace la comparación case-insensitive
        private readonly HashSet<string> _tablasProhibidas;

        /// <summary>
        /// Constructor que lee la configuración y prepara el HashSet de tablas prohibidas.
        ///
        /// FLUJO DE INICIALIZACIÓN:
        /// 1. Recibe IConfiguration por inyección de dependencias (DIP)
        /// 2. Lee la sección "TablasProhibidas" del JSON
        /// 3. Convierte el array JSON a HashSet de strings
        /// 4. Configura comparación case-insensitive
        /// 5. Queda listo para validaciones ultrarrápidas
        ///
        /// OPTIMIZACIÓN:
        /// - La lectura del JSON se hace UNA SOLA VEZ en el constructor
        /// - El HashSet se mantiene en memoria durante toda la vida de la aplicación
        /// - Cada validación es O(1) gracias al HashSet
        /// - No hay lecturas repetidas del archivo de configuración
        ///
        /// SCOPE RECOMENDADO EN PROGRAM.CS:
        /// AddSingleton porque:
        /// - La configuración no cambia durante la ejecución
        /// - Queremos una sola instancia compartida por toda la app
        /// - Mejor performance (no se recrea en cada request)
        /// </summary>
        /// <param name="configuration">
        /// Configuración de la aplicación inyectada automáticamente.
        /// Contiene todos los valores de appsettings.json y appsettings.Development.json.
        ///
        /// Esta clase SÍ puede depender de IConfiguration porque:
        /// - Es su responsabilidad específica leer configuración
        /// - Está en la capa de infraestructura, no en la capa de dominio
        /// - ServicioCrud NO depende de IConfiguration, solo de IPoliticaTablasProhibidas
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si configuration es null, indicando problema en la inyección de dependencias.
        /// </exception>
        public PoliticaTablasProhibidasDesdeJson(IConfiguration configuration)
        {
            // Validación defensiva: asegurar que la configuración fue inyectada correctamente
            if (configuration == null)
                throw new ArgumentNullException(
                    nameof(configuration),
                    "IConfiguration no puede ser null. Verificar registro de servicios en Program.cs."
                );

            // FASE 1: LEER LA SECCIÓN "TablasProhibidas" DEL JSON
            // GetSection("TablasProhibidas") busca esta clave en el JSON
            // Get<string[]>() convierte el array JSON a array de strings de C#
            // ?? Array.Empty<string>() proporciona array vacío si la sección no existe
            //
            // Ejemplos de configuración válida:
            // "TablasProhibidas": ["usuarios", "contrasenas"]          → array con 2 elementos
            // "TablasProhibidas": []                                   → array vacío (todo permitido)
            // (sin la clave "TablasProhibidas" en el JSON)            → Array.Empty (todo permitido)
            var tablasProhibidasArray = configuration.GetSection("TablasProhibidas")
                .Get<string[]>() ?? Array.Empty<string>();

            // FASE 2: CONVERTIR A HASHSET CON COMPARACIÓN CASE-INSENSITIVE
            // HashSet ventajas:
            // - Búsqueda O(1) vs List O(n)
            // - No permite duplicados automáticamente
            // - Ideal para verificar pertenencia (Contains)
            //
            // StringComparer.OrdinalIgnoreCase:
            // - "usuarios" == "Usuarios" == "USUARIOS" → true
            // - Previene bypass por diferencias de mayúsculas/minúsculas
            // - Ordinal = comparación rápida basada en valores Unicode (más rápida que CurrentCulture)
            //
            // Where(!string.IsNullOrWhiteSpace):
            // - Filtra entradas vacías o con solo espacios del JSON
            // - Previene errores si alguien pone: "TablasProhibidas": ["usuarios", "", "  "]
            _tablasProhibidas = new HashSet<string>(
                tablasProhibidasArray.Where(t => !string.IsNullOrWhiteSpace(t)),
                StringComparer.OrdinalIgnoreCase
            );

            // LOGGING OPCIONAL PARA DEBUGGING (comentado para producción):
            // Console.WriteLine($"[PoliticaTablasProhibidas] Inicializada con {_tablasProhibidas.Count} tablas prohibidas");
            // foreach (var tabla in _tablasProhibidas)
            // {
            //     Console.WriteLine($"  - {tabla}");
            // }
        }

        /// <summary>
        /// Implementa IPoliticaTablasProhibidas.EsTablaPermitida()
        ///
        /// Determina si una tabla está permitida verificando si NO está en la lista de prohibidas.
        ///
        /// LÓGICA DE VALIDACIÓN:
        /// 1. Si nombreTabla es null/vacío → false (no se permite operar sobre tabla sin nombre)
        /// 2. Si _tablasProhibidas está vacío → true (todo está permitido)
        /// 3. Si nombreTabla está en _tablasProhibidas → false (prohibida explícitamente)
        /// 4. Si nombreTabla NO está en _tablasProhibidas → true (permitida por defecto)
        ///
        /// ESTRATEGIA: WHITELIST vs BLACKLIST
        /// Esta implementación usa BLACKLIST (lista negra):
        /// - Por defecto todo está permitido
        /// - Solo se prohíben las tablas explícitamente listadas
        ///
        /// Alternativa WHITELIST (lista blanca) sería:
        /// - Por defecto todo está prohibido
        /// - Solo se permiten las tablas explícitamente listadas
        /// - Más seguro pero menos flexible
        ///
        /// PERFORMANCE:
        /// - HashSet.Contains() es O(1) - tiempo constante
        /// - No importa si tienes 10 o 10,000 tablas prohibidas
        /// - Comparación case-insensitive gracias al StringComparer del constructor
        /// </summary>
        /// <param name="nombreTabla">
        /// Nombre de la tabla a validar. Viene típicamente de la URL del request HTTP.
        /// Ejemplo: GET /api/productos → nombreTabla = "productos"
        /// </param>
        /// <returns>
        /// - true: La tabla está permitida, ServicioCrud puede continuar
        /// - false: La tabla está prohibida, ServicioCrud debe lanzar UnauthorizedAccessException
        /// </returns>
        public bool EsTablaPermitida(string nombreTabla)
        {
            // VALIDACIÓN 1: Nombres de tabla vacíos o null no están permitidos
            // Esto previene intentos de acceder a tablas sin nombre
            if (string.IsNullOrWhiteSpace(nombreTabla))
                return false;

            // VALIDACIÓN 2: Verificar si la tabla está en la lista de prohibidas
            // Contains() usa el StringComparer.OrdinalIgnoreCase del constructor
            // Por lo tanto: "Usuarios", "usuarios", "USUARIOS" se tratan igual
            //
            // Lógica invertida: retorna true si NO está prohibida
            // !_tablasProhibidas.Contains(nombreTabla) significa:
            // - Si la tabla NO está en la lista prohibida → true (permitida)
            // - Si la tabla SÍ está en la lista prohibida → false (no permitida)
            return !_tablasProhibidas.Contains(nombreTabla);
        }

        // MÉTODOS ADICIONALES ÚTILES PARA DEBUGGING Y ADMINISTRACIÓN (opcionales)

        /// <summary>
        /// Método auxiliar para obtener la lista de tablas prohibidas (útil para debugging y administración).
        /// No forma parte de la interfaz IPoliticaTablasProhibidas, es específico de esta implementación.
        /// </summary>
        /// <returns>
        /// Array de solo lectura con los nombres de las tablas prohibidas.
        /// </returns>
        public IReadOnlyCollection<string> ObtenerTablasProhibidas()
        {
            return _tablasProhibidas;
        }

        /// <summary>
        /// Método auxiliar para verificar si hay tablas prohibidas configuradas.
        /// Útil para validaciones y tests.
        /// </summary>
        /// <returns>
        /// - true: Hay al menos una tabla prohibida
        /// - false: No hay tablas prohibidas (todo está permitido)
        /// </returns>
        public bool TieneRestricciones()
        {
            return _tablasProhibidas.Count > 0;
        }
    }
}

// ====================================================================================
// NOTAS PEDAGÓGICAS PARA TUS ESTUDIANTES
// ====================================================================================
//
// 1. PATRÓN DE DISEÑO APLICADO: STRATEGY
//    - IPoliticaTablasProhibidas define la estrategia (qué se debe hacer)
//    - Esta clase implementa UNA estrategia específica (cómo hacerlo desde JSON)
//    - Podrías crear otras estrategias: desde BD, desde API, desde Redis, etc.
//    - ServicioCrud no sabe ni le importa cuál estrategia se usa
//
// 2. OPTIMIZACIÓN DE PERFORMANCE:
//    ANTES (si usaras List):
//    var lista = new List<string> { "tabla1", "tabla2", ..., "tabla1000" };
//    bool permitida = !lista.Contains("tabla999"); // O(n) - recorre toda la lista
//
//    DESPUÉS (con HashSet):
//    var hashset = new HashSet<string> { "tabla1", "tabla2", ..., "tabla1000" };
//    bool permitida = !hashset.Contains("tabla999"); // O(1) - búsqueda instantánea
//
// 3. SEGURIDAD: CASE-INSENSITIVE
//    Sin StringComparer.OrdinalIgnoreCase:
//    - Cliente malicioso podría hacer: GET /api/Usuarios (con U mayúscula)
//    - Si la configuración dice: "TablasProhibidas": ["usuarios"]
//    - La validación fallaría porque "Usuarios" != "usuarios"
//    - ¡Bypass de seguridad!
//
//    Con StringComparer.OrdinalIgnoreCase:
//    - "usuarios" == "Usuarios" == "USUARIOS" == "UsUaRiOs"
//    - Imposible hacer bypass cambiando mayúsculas/minúsculas
//
// 4. INYECCIÓN DE DEPENDENCIAS - CICLO COMPLETO:
//    Program.cs:
//    builder.Services.AddSingleton<IPoliticaTablasProhibidas, PoliticaTablasProhibidasDesdeJson>();
//
//    Cuando se crea ServicioCrud:
//    public ServicioCrud(IRepositorioLecturaTabla repo, IPoliticaTablasProhibidas politica)
//
//    El contenedor DI:
//    1. Ve que ServicioCrud necesita IPoliticaTablasProhibidas
//    2. Busca qué implementación está registrada → encuentra PoliticaTablasProhibidasDesdeJson
//    3. Crea instancia de PoliticaTablasProhibidasDesdeJson
//    4. Ve que PoliticaTablasProhibidasDesdeJson necesita IConfiguration
//    5. Inyecta IConfiguration (ya disponible como singleton en ASP.NET Core)
//    6. Inyecta PoliticaTablasProhibidasDesdeJson en ServicioCrud
//    7. Todo funciona automáticamente
//
// 5. TESTING FACILITADO:
//    Test 1 - Mock simple:
//    public class PoliticaMock : IPoliticaTablasProhibidas
//    {
//        public bool EsTablaPermitida(string tabla) => true; // Todo permitido
//    }
//
//    Test 2 - Mock configurable:
//    public class PoliticaMockConfigurable : IPoliticaTablasProhibidas
//    {
//        private readonly HashSet<string> _prohibidas;
//        public PoliticaMockConfigurable(params string[] prohibidas)
//        {
//            _prohibidas = new HashSet<string>(prohibidas, StringComparer.OrdinalIgnoreCase);
//        }
//        public bool EsTablaPermitida(string tabla) => !_prohibidas.Contains(tabla);
//    }
//
//    Uso en test:
//    var politica = new PoliticaMockConfigurable("usuarios", "contrasenas");
//    var servicio = new ServicioCrud(repositorioMock, politica);
//    // Ahora puedes testear ServicioCrud sin necesidad de appsettings.json
//
// 6. EXTENSIBILIDAD FUTURA:
//    Crear PoliticaTablasProhibidasDesdeBaseDatos:
//    public class PoliticaTablasProhibidasDesdeBaseDatos : IPoliticaTablasProhibidas
//    {
//        private readonly IRepositorio _repo;
//
//        public PoliticaTablasProhibidasDesdeBaseDatos(IRepositorio repo)
//        {
//            _repo = repo;
//        }
//
//        public bool EsTablaPermitida(string tabla)
//        {
//            // Consultar tabla de configuración en BD
//            return !_repo.ExisteEnTablasProhibidas(tabla);
//        }
//    }
//
//    Cambiar en Program.cs:
//    // ANTES:
//    builder.Services.AddSingleton<IPoliticaTablasProhibidas, PoliticaTablasProhibidasDesdeJson>();
//
//    // DESPUÉS:
//    builder.Services.AddSingleton<IPoliticaTablasProhibidas, PoliticaTablasProhibidasDesdeBaseDatos>();
//
//    ServicioCrud no necesita cambiar NADA → OCP (Open/Closed Principle)
//
// 7. PREGUNTAS PARA TUS ESTUDIANTES:
//    - ¿Por qué usar HashSet en lugar de List?
//    - ¿Qué pasaría si no usáramos StringComparer.OrdinalIgnoreCase?
//    - ¿Cómo crearías una implementación que lea de base de datos?
//    - ¿Por qué esta clase puede depender de IConfiguration pero ServicioCrud no?
//    - ¿Qué ventajas tiene usar AddSingleton vs AddScoped para esta clase?
