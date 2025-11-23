// IPoliticaTablasProhibidas.cs — Interfaz para gestión de tablas permitidas/prohibidas
// Ubicación: Servicios/Abstracciones/IPoliticaTablasProhibidas.cs
//
// Principios SOLID aplicados:
// - SRP: Responsabilidad única = decidir si una tabla está permitida o prohibida
// - DIP: Los servicios dependen de esta abstracción, no de la configuración directa
// - OCP: Se puede extender con nuevas implementaciones (base de datos, archivos, API externa)
// - ISP: Interfaz pequeña y específica con un solo método

namespace webapicsharp.Servicios.Abstracciones
{
    /// <summary>
    /// Interfaz que define el contrato para validar si una tabla está permitida o prohibida.
    ///
    /// PROPÓSITO EDUCATIVO:
    /// Esta interfaz demuestra cómo aplicar el principio de Inversión de Dependencias (DIP).
    /// En lugar de que ServicioCrud dependa directamente de IConfiguration (implementación concreta),
    /// ahora depende de esta abstracción que puede tener múltiples implementaciones.
    ///
    /// BENEFICIOS DE ESTA ABSTRACCIÓN:
    /// 1. ServicioCrud no necesita conocer de dónde vienen las reglas (archivo JSON, BD, API)
    /// 2. Facilita testing: podemos crear un mock que siempre retorna true/false
    /// 3. Permite cambiar la fuente de configuración sin modificar ServicioCrud
    /// 4. Cumple con SRP: una clase, una responsabilidad (validar tablas)
    ///
    /// POSIBLES IMPLEMENTACIONES:
    /// - PoliticaTablasProhibidasDesdeJson: Lee de appsettings.json (implementación actual)
    /// - PoliticaTablasProhibidasDesdeBaseDatos: Lee de tabla de configuración
    /// - PoliticaTablasProhibidasDesdeCache: Lee de Redis o similar
    /// - PoliticaTablasProhibidasPorRoles: Valida según el rol del usuario autenticado
    ///
    /// FLUJO DE USO:
    /// 1. Program.cs registra: AddSingleton<IPoliticaTablasProhibidas, PoliticaTablasProhibidasDesdeJson>
    /// 2. ServicioCrud recibe IPoliticaTablasProhibidas en su constructor
    /// 3. ServicioCrud llama a EsTablaPermitida(nombreTabla) antes de cada operación
    /// 4. Si retorna false, lanza UnauthorizedAccessException
    /// </summary>
    public interface IPoliticaTablasProhibidas
    {
        /// <summary>
        /// Determina si una tabla específica está permitida para operaciones CRUD.
        ///
        /// Este método encapsula toda la lógica de validación de tablas:
        /// - Puede verificar listas negras (tablas prohibidas)
        /// - Puede verificar listas blancas (solo tablas permitidas)
        /// - Puede aplicar reglas complejas (según usuario, horario, etc.)
        ///
        /// CASO DE USO TÍPICO:
        /// Proteger tablas del sistema que no deben ser accesibles vía API genérica:
        /// - Tablas de usuarios y contraseñas (ya tienen su endpoint dedicado)
        /// - Tablas de configuración del sistema
        /// - Tablas de auditoría
        /// - Tablas temporales o de sistema
        ///
        /// EJEMPLO EN ACCIÓN:
        /// if (!_politicaTablas.EsTablaPermitida("usuarios"))
        /// {
        ///     throw new UnauthorizedAccessException(
        ///         "La tabla 'usuarios' está restringida. Use el endpoint de autenticación."
        ///     );
        /// }
        /// </summary>
        /// <param name="nombreTabla">
        /// Nombre de la tabla a validar. Puede venir de la URL del request HTTP.
        /// Ejemplo: en GET /api/productos, nombreTabla = "productos"
        ///
        /// La validación debe ser case-insensitive para evitar bypasses del tipo:
        /// - "Usuarios" vs "usuarios" vs "USUARIOS" deben tratarse igual
        /// </param>
        /// <returns>
        /// - true: La tabla está permitida, se puede proceder con la operación CRUD
        /// - false: La tabla está prohibida, se debe bloquear la operación
        ///
        /// El servicio que consume esta interfaz es responsable de lanzar la excepción
        /// apropiada cuando retorna false (típicamente UnauthorizedAccessException).
        /// </returns>
        /// <remarks>
        /// NOTA PEDAGÓGICA PARA ESTUDIANTES:
        ///
        /// Este es un ejemplo perfecto de cómo aplicar el principio "Program to an interface,
        /// not an implementation" (Programa hacia una interfaz, no una implementación).
        ///
        /// ANTES (MAL - violaba DIP):
        /// ServicioCrud dependía de IConfiguration (clase concreta de Microsoft)
        /// → Difícil de testear
        /// → Acoplamiento fuerte con la infraestructura de configuración
        /// → Mezcla de responsabilidades
        ///
        /// DESPUÉS (BIEN - cumple DIP):
        /// ServicioCrud depende de IPoliticaTablasProhibidas (abstracción nuestra)
        /// → Fácil de testear con mocks
        /// → Desacoplado de cómo se obtiene la configuración
        /// → Cada clase tiene una responsabilidad específica
        ///
        /// TESTING:
        /// // Crear un mock para testing
        /// public class PoliticaTablasProhibidasMock : IPoliticaTablasProhibidas
        /// {
        ///     public bool EsTablaPermitida(string nombreTabla) => true; // Siempre permite
        /// }
        ///
        /// // Inyectar en ServicioCrud para pruebas
        /// var servicio = new ServicioCrud(repositorioMock, politicaMock);
        ///
        /// EXTENSIBILIDAD FUTURA:
        /// Si mañana quieres leer las tablas prohibidas de una base de datos en lugar de JSON:
        /// 1. Crear nueva clase: PoliticaTablasProhibidasDesdeBaseDatos
        /// 2. Implementar IPoliticaTablasProhibidas
        /// 3. Cambiar el registro en Program.cs
        /// 4. ServicioCrud NO necesita cambiar (OCP - Open/Closed Principle)
        /// </remarks>
        bool EsTablaPermitida(string nombreTabla);
    }
}

// ====================================================================================
// NOTAS PEDAGÓGICAS PARA EL TUTORIAL
// ====================================================================================
//
// 1. ¿POR QUÉ ESTA INTERFAZ?
//    Antes: ServicioCrud inyectaba IConfiguration directamente
//    Problema: Mezclaba responsabilidades (lógica de negocio + lectura de configuración)
//    Ahora: ServicioCrud solo conoce IPoliticaTablasProhibidas (abstracción limpia)
//    Beneficio: Mejor separación de responsabilidades y testabilidad
//
// 2. PRINCIPIO DIP EN ACCIÓN:
//    - Módulos de alto nivel (ServicioCrud) NO dependen de módulos de bajo nivel (IConfiguration)
//    - Ambos dependen de abstracciones (IPoliticaTablasProhibidas)
//    - Las abstracciones NO dependen de detalles, los detalles dependen de abstracciones
//
// 3. COMPARACIÓN CON EL CÓDIGO ANTERIOR:
//    ANTES en ServicioCrud.cs línea 74:
//    private readonly IConfiguration _configuration;
//
//    DESPUÉS:
//    private readonly IPoliticaTablasProhibidas _politicaTablas;
//
//    ¿Cuál es mejor? El segundo, porque:
//    - Es más específico (dice exactamente qué hace)
//    - Es más testeable (fácil crear un mock)
//    - Es más mantenible (cambios de implementación no afectan ServicioCrud)
//
// 4. PATRÓN STRATEGY:
//    Esta interfaz también implementa el patrón Strategy:
//    - Define una familia de algoritmos (formas de validar tablas)
//    - Los hace intercambiables
//    - El cliente (ServicioCrud) no conoce los detalles de la implementación
//
// 5. PRÓXIMOS PASOS:
//    - Crear la implementación: PoliticaTablasProhibidasDesdeJson
//    - Registrar en Program.cs: AddSingleton<IPoliticaTablasProhibidas, ...>
//    - Modificar ServicioCrud para usar esta interfaz
//    - Eliminar la dependencia de IConfiguration de ServicioCrud
//
// 6. PARA TUS ESTUDIANTES:
//    Pregúntales:
//    - ¿Qué ventajas tiene esta interfaz vs usar IConfiguration directamente?
//    - ¿Cómo facilitaría esta interfaz el testing unitario?
//    - ¿Qué otras implementaciones podrían crear además de leer de JSON?
//    - ¿Cómo esto demuestra el principio de Inversión de Dependencias?
