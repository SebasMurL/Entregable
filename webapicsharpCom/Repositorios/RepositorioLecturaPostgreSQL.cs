// --------------------------------------------------------------
// Archivo : RepositorioLecturaPostgreSQL.cs
// Ruta    : webapicsharp/Repositorios/RepositorioLecturaPostgreSQL.cs
// Propósito: Implementar IRepositorioLecturaTabla para PostgreSQL
// Dependencias clave: Npgsql, IProveedorConexion, EncriptacionBCrypt
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Npgsql;                                        // proveedor ADO.NET para PostgreSQL
using webapicsharp.Repositorios.Abstracciones;      // IRepositorioLecturaTabla
using webapicsharp.Servicios.Abstracciones;         // IProveedorConexion
using webapicsharp.Servicios.Utilidades;            // EncriptacionBCrypt

namespace webapicsharp.Repositorios
{
    /// <summary>
    /// Implementa operaciones CRUD básicas y utilidades de autenticación
    /// contra PostgreSQL respetando IRepositorioLecturaTabla.
    /// </summary>
    public sealed class RepositorioLecturaPostgreSQL : IRepositorioLecturaTabla
    {
        private readonly IProveedorConexion _proveedorConexion;

        /// <summary>
        /// Recibe el proveedor de cadena de conexión vía DI.
        /// </summary>
        public RepositorioLecturaPostgreSQL(IProveedorConexion proveedorConexion)
        {
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(nameof(proveedorConexion));
        }

        /// <summary>
        /// Devuelve filas de una tabla con límite (SELECT * ... LIMIT @limite).
        /// Usa comillas dobles para proteger identificadores en PostgreSQL.
        /// </summary>
        public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerFilasAsync(
            string nombreTabla,
            string? esquema,
            int? limite
        )
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();
            int limiteFinal = limite ?? 1000;

            string sql = $"SELECT * FROM \"{esquemaFinal}\".\"{nombreTabla}\" LIMIT @limite";

            var filas = new List<Dictionary<string, object?>>();
            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("limite", limiteFinal);

            await using var lector = await comando.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            while (await lector.ReadAsync())
            {
                var fila = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < lector.FieldCount; i++)
                {
                    object valor = await lector.IsDBNullAsync(i) ? null! : lector.GetValue(i);
                    fila[lector.GetName(i)] = valor;
                }
                filas.Add(fila);
            }

            return filas;
        }

        /// <summary>
        /// Devuelve filas que cumplan igualdad exacta en una columna clave.
        /// </summary>
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

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();

            string sql = $"SELECT * FROM \"{esquemaFinal}\".\"{nombreTabla}\" WHERE \"{nombreClave}\" = @valor";

            var filas = new List<Dictionary<string, object?>>();
            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("valor", valor);

            await using var lector = await comando.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            while (await lector.ReadAsync())
            {
                var fila = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < lector.FieldCount; i++)
                {
                    object dato = await lector.IsDBNullAsync(i) ? null! : lector.GetValue(i);
                    fila[lector.GetName(i)] = dato;
                }
                filas.Add(fila);
            }

            return filas;
        }

        /// <summary>
        /// Inserta un registro. Si se indican campos a encriptar (coma-separados),
        /// se encripta su valor con BCrypt antes de ejecutar INSERT.
        /// </summary>
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

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();

            // Encripta campos sensibles si se solicitan (e.g., "password,otro_campo")
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
            var parametrosComando = new List<NpgsqlParameter>();

            foreach (var par in datos)
            {
                string nombreCol = par.Key;
                string nombreParam = $"p_{nombreCol}";
                nombres.Add($"\"{nombreCol}\"");
                parametros.Add($"@{nombreParam}");
                parametrosComando.Add(new NpgsqlParameter(nombreParam, par.Value ?? DBNull.Value));
            }

            string sql =
                $"INSERT INTO \"{esquemaFinal}\".\"{nombreTabla}\" ({string.Join(", ", nombres)}) VALUES ({string.Join(", ", parametros)})";

            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddRange(parametrosComando.ToArray());

            int afectados = await comando.ExecuteNonQueryAsync();
            return afectados > 0;
        }

        /// <summary>
        /// Actualiza columnas por clave primaria/única. Aplica encriptación opcional como en INSERT.
        /// </summary>
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

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();

            // Encripta campos sensibles si se solicitaron
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
            var parametrosComando = new List<NpgsqlParameter>();

            foreach (var par in datos)
            {
                string nombreCol = par.Key;
                string nombreParam = $"p_{nombreCol}";
                asignaciones.Add($"\"{nombreCol}\" = @{nombreParam}");
                parametrosComando.Add(new NpgsqlParameter(nombreParam, par.Value ?? DBNull.Value));
            }

            string sql =
                $"UPDATE \"{esquemaFinal}\".\"{nombreTabla}\" SET {string.Join(", ", asignaciones)} WHERE \"{nombreClave}\" = @valorClave";

            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddRange(parametrosComando.ToArray());
            comando.Parameters.AddWithValue("valorClave", valorClave);

            int afectados = await comando.ExecuteNonQueryAsync();
            return afectados;
        }

        /// <summary>
        /// Elimina filas por columna clave con igualdad exacta.
        /// </summary>
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

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();

            string sql = $"DELETE FROM \"{esquemaFinal}\".\"{nombreTabla}\" WHERE \"{nombreClave}\" = @valorClave";

            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("valorClave", valorClave);

            int afectados = await comando.ExecuteNonQueryAsync();
            return afectados;
        }

        /// <summary>
        /// Devuelve el hash de contraseña almacenado para un usuario dado.
        /// Útil para validación de credenciales en capa de servicios.
        /// </summary>
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

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();

            string sql =
                $"SELECT \"{campoContrasena}\" FROM \"{esquemaFinal}\".\"{nombreTabla}\" WHERE \"{campoUsuario}\" = @usuario LIMIT 1";

            var cadena = _proveedorConexion.ObtenerCadenaConexion();

            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("usuario", valorUsuario);

            object? resultado = await comando.ExecuteScalarAsync();
            return resultado == null || resultado is DBNull ? null : Convert.ToString(resultado);
        }
    }
}
