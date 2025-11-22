// -----------------------------------------------------------------------------
// Archivo   : RepositorioLecturaMysqlMariaDB.cs
// Ruta      : webapicsharp/Repositorios/RepositorioLecturaMysqlMariaDB.cs
// Propósito : Implementar IRepositorioLecturaTabla para MySQL/MariaDB,
//             con soporte de operaciones CRUD y encriptación de contraseñas.
// Dependencias:
//   - Paquetes NuGet instalados: MySql.Data y MySqlConnector
//   - Contratos: IRepositorioLecturaTabla, IProveedorConexion
//   - Utilidad: EncriptacionBCrypt
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
// Dependencias de MySQL/MariaDB
// -----------------------------------------------------------------------------
// Hay dos opciones. Basta con descomentar una y dejar la otra comentada.
// Ambas funcionan para MySQL y MariaDB.
//
// Opción 1: Conector oficial de Oracle (paquete MySql.Data)
// using MySql.Data.MySqlClient;
//
// Opción 2: Conector alternativo de alto rendimiento (paquete MySqlConnector)
using MySqlConnector;

using webapicsharp.Repositorios.Abstracciones;      // Contrato IRepositorioLecturaTabla
using webapicsharp.Servicios.Abstracciones;         // Contrato IProveedorConexion
using webapicsharp.Servicios.Utilidades;            // EncriptacionBCrypt

namespace webapicsharp.Repositorios
{
    /// <summary>
    /// Repositorio concreto para MySQL/MariaDB que implementa las operaciones
    /// de IRepositorioLecturaTabla. Soporta creación, lectura, actualización,
    /// eliminación y obtención de hash de contraseñas.
    /// </summary>
    public sealed class RepositorioLecturaMysqlMariaDB : IRepositorioLecturaTabla
    {
        private readonly IProveedorConexion _proveedorConexion;

        /// <summary>
        /// Constructor que recibe el proveedor de cadena de conexión.
        /// </summary>
        public RepositorioLecturaMysqlMariaDB(IProveedorConexion proveedorConexion)
        {
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(nameof(proveedorConexion));
        }

        // ---------------------------------------------------------------------
        // Método: ObtenerFilasAsync
        // Propósito: Devolver todas las filas de una tabla, con un límite máximo.
        // ---------------------------------------------------------------------
        public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerFilasAsync(
            string nombreTabla,
            string? esquema,
            int? limite
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            int limiteFinal = limite ?? 1000;  // Límite por defecto si no se indica
            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "" : $"`{esquema}`.";

            string sql = $"SELECT * FROM {esquemaFinal}`{nombreTabla}` LIMIT @limite";

            var filas = new List<Dictionary<string, object?>>();
            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@limite", limiteFinal);

            await using var lector = await comando.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            while (await lector.ReadAsync())
            {
                var fila = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < lector.FieldCount; i++)
                {
                    fila[lector.GetName(i)] = await lector.IsDBNullAsync(i) ? null! : lector.GetValue(i);
                }
                filas.Add(fila);
            }

            return filas;
        }

        // ---------------------------------------------------------------------
        // Método: ObtenerPorClaveAsync
        // Propósito: Devolver filas que coincidan con una clave específica.
        // ---------------------------------------------------------------------
        public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerPorClaveAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valor
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            if (string.IsNullOrWhiteSpace(nombreClave))
                throw new ArgumentException("El nombre de la columna clave no puede estar vacío.", nameof(nombreClave));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "" : $"`{esquema}`.";

            string sql = $"SELECT * FROM {esquemaFinal}`{nombreTabla}` WHERE `{nombreClave}` = @valor";

            var filas = new List<Dictionary<string, object?>>();
            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@valor", valor);

            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                var fila = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < lector.FieldCount; i++)
                {
                    fila[lector.GetName(i)] = await lector.IsDBNullAsync(i) ? null! : lector.GetValue(i);
                }
                filas.Add(fila);
            }

            return filas;
        }

        // ---------------------------------------------------------------------
        // Método: CrearAsync
        // Propósito: Insertar un nuevo registro en la tabla.
        // Nota: Soporta encriptación de campos sensibles como contraseñas.
        // ---------------------------------------------------------------------
        public async Task<bool> CrearAsync(
            string nombreTabla,
            string? esquema,
            Dictionary<string, object?> datos,
            string? camposEncriptar = null
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            if (datos is null || datos.Count == 0)
                throw new ArgumentException("El diccionario de datos no puede estar vacío.", nameof(datos));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "" : $"`{esquema}`.";

            // Encriptar campos sensibles si se solicita (ejemplo: "password,otro")
            if (!string.IsNullOrWhiteSpace(camposEncriptar))
            {
                foreach (var campo in camposEncriptar.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (datos.ContainsKey(campo))
                    {
                        var original = datos[campo]?.ToString() ?? string.Empty;
                        datos[campo] = EncriptacionBCrypt.Encriptar(original, costo: 10);
                    }
                }
            }

            var nombres = new List<string>();
            var parametros = new List<string>();
            var parametrosComando = new List<MySqlParameter>();

            foreach (var par in datos)
            {
                string nombreCol = par.Key;
                string nombreParam = $"p_{nombreCol}";
                nombres.Add($"`{nombreCol}`");
                parametros.Add($"@{nombreParam}");
                parametrosComando.Add(new MySqlParameter(nombreParam, par.Value ?? DBNull.Value));
            }

            string sql =
                $"INSERT INTO {esquemaFinal}`{nombreTabla}` ({string.Join(", ", nombres)}) VALUES ({string.Join(", ", parametros)})";

            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddRange(parametrosComando.ToArray());

            int afectados = await comando.ExecuteNonQueryAsync();
            return afectados > 0;
        }

        // ---------------------------------------------------------------------
        // Método: ActualizarAsync
        // Propósito: Modificar columnas de un registro identificado por una clave.
        // ---------------------------------------------------------------------
        public async Task<int> ActualizarAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valorClave,
            Dictionary<string, object?> datos,
            string? camposEncriptar = null
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            if (string.IsNullOrWhiteSpace(nombreClave))
                throw new ArgumentException("El nombre de la columna clave no puede estar vacío.", nameof(nombreClave));
            if (datos is null || datos.Count == 0)
                throw new ArgumentException("El diccionario de datos no puede estar vacío.", nameof(datos));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "" : $"`{esquema}`.";

            // Encriptar campos sensibles si se solicita
            if (!string.IsNullOrWhiteSpace(camposEncriptar))
            {
                foreach (var campo in camposEncriptar.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (datos.ContainsKey(campo))
                    {
                        var original = datos[campo]?.ToString() ?? string.Empty;
                        datos[campo] = EncriptacionBCrypt.Encriptar(original, costo: 10);
                    }
                }
            }

            var asignaciones = new List<string>();
            var parametrosComando = new List<MySqlParameter>();

            foreach (var par in datos)
            {
                string nombreCol = par.Key;
                string nombreParam = $"p_{nombreCol}";
                asignaciones.Add($"`{nombreCol}` = @{nombreParam}");
                parametrosComando.Add(new MySqlParameter(nombreParam, par.Value ?? DBNull.Value));
            }

            string sql =
                $"UPDATE {esquemaFinal}`{nombreTabla}` SET {string.Join(", ", asignaciones)} WHERE `{nombreClave}` = @valorClave";

            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddRange(parametrosComando.ToArray());
            comando.Parameters.AddWithValue("@valorClave", valorClave);

            int afectados = await comando.ExecuteNonQueryAsync();
            return afectados;
        }

        // ---------------------------------------------------------------------
        // Método: EliminarAsync
        // Propósito: Borrar registros a partir de una clave.
        // ---------------------------------------------------------------------
        public async Task<int> EliminarAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valorClave
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));
            if (string.IsNullOrWhiteSpace(nombreClave))
                throw new ArgumentException("El nombre de la columna clave no puede estar vacío.", nameof(nombreClave));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "" : $"`{esquema}`.";

            string sql = $"DELETE FROM {esquemaFinal}`{nombreTabla}` WHERE `{nombreClave}` = @valorClave";

            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@valorClave", valorClave);

            int afectados = await comando.ExecuteNonQueryAsync();
            return afectados;
        }

        // ---------------------------------------------------------------------
        // Método: ObtenerHashContrasenaAsync
        // Propósito: Recuperar el hash de contraseña de un usuario.
        // ---------------------------------------------------------------------
        public async Task<string?> ObtenerHashContrasenaAsync(
            string nombreTabla,
            string? esquema,
            string campoUsuario,
            string campoContrasena,
            string valorUsuario
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "" : $"`{esquema}`.";

            string sql =
                $"SELECT `{campoContrasena}` FROM {esquemaFinal}`{nombreTabla}` WHERE `{campoUsuario}` = @usuario LIMIT 1";

            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new MySqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@usuario", valorUsuario);

            object? resultado = await comando.ExecuteScalarAsync();
            return resultado == null || resultado is DBNull ? null : Convert.ToString(resultado);
        }
    }
}
