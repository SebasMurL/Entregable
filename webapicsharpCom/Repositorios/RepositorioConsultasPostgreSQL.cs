// --------------------------------------------------------------
// Archivo : RepositorioConsultasPostgreSQL.cs
// Ruta    : webapicsharp/Repositorios/RepositorioConsultasPostgreSQL.cs
// Propósito: Implementar IRepositorioConsultas con Npgsql para PostgreSQL
// Notas:
// - La interfaz usa Microsoft.Data.SqlClient.SqlParameter. Se aceptan así
//   y se convierten internamente a NpgsqlParameter.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;                     // SqlParameter (de la interfaz)
using Npgsql;                                       // Proveedor ADO.NET para PostgreSQL
using webapicsharp.Repositorios.Abstracciones;      // IRepositorioConsultas
using webapicsharp.Servicios.Abstracciones;         // IProveedorConexion

namespace webapicsharp.Repositorios
{
    /// <summary>
    /// Implementación de IRepositorioConsultas para PostgreSQL usando Npgsql.
    /// </summary>
    public sealed class RepositorioConsultasPostgreSQL : IRepositorioConsultas
    {
        private readonly IProveedorConexion _proveedorConexion;

        public RepositorioConsultasPostgreSQL(IProveedorConexion proveedorConexion)
        {
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(nameof(proveedorConexion));
        }

        // ============================================================
        // 1) Ejecutar CONSULTA parametrizada → DataTable
        // ============================================================
        public async Task<DataTable> EjecutarConsultaParametrizadaAsync(
            string consultaSQL,
            List<SqlParameter>? parametros
        )
        {
            if (string.IsNullOrWhiteSpace(consultaSQL))
                throw new ArgumentException("La consulta SQL no puede estar vacía.", nameof(consultaSQL));

            var cadena = _proveedorConexion.ObtenerCadenaConexion();
            var tabla = new DataTable();

            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            await using var comando = new NpgsqlCommand(consultaSQL, conexion);
            AgregarParametros(comando, parametros);

            // DataTable.Load lee el primer resultset
            await using var lector = await comando.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            tabla.Load(lector);
            return tabla;
        }

        // ============================================================
        // 2) Validar CONSULTA (sin ejecutarla "realmente")
        //    Se intenta preparar el comando. Si falla, se retorna error.
        // ============================================================
        public async Task<(bool esValida, string? mensajeError)> ValidarConsultaAsync(
            string consultaSQL,
            List<SqlParameter>? parametros
        )
        {
            if (string.IsNullOrWhiteSpace(consultaSQL))
                return (false, "La consulta SQL está vacía.");

            try
            {
                var cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();

                await using var comando = new NpgsqlCommand(consultaSQL, conexion);
                AgregarParametros(comando, parametros);

                // Preparar valida sintaxis y parámetros sin ejecutar el plan
                await comando.PrepareAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // ============================================================
        // 3) Ejecutar PROCEDIMIENTO almacenado → DataTable
        //    En PostgreSQL puede ser PROCEDURE (CALL ...) o FUNCTION (SELECT ...).
        //    Se intenta CALL; si no devuelve resultset, se intenta SELECT * FROM fn(...)
        // ============================================================
        public async Task<DataTable> EjecutarProcedimientoAlmacenadoAsync(
            string nombreSP,
            List<SqlParameter>? parametros
        )
        {
            if (string.IsNullOrWhiteSpace(nombreSP))
                throw new ArgumentException("El nombre del procedimiento no puede estar vacío.", nameof(nombreSP));

            var cadena = _proveedorConexion.ObtenerCadenaConexion();
            var tabla = new DataTable();

            // Se construyen placeholders con los nombres de @param
            var placeHolders = ConstruirPlaceholders(parametros);

            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            // Intento 1: CALL procedimiento(@a, @b, ...)
            string sqlCall = $"CALL {nombreSP}({placeHolders})";
            await using (var cmdCall = new NpgsqlCommand(sqlCall, conexion))
            {
                AgregarParametros(cmdCall, parametros);
                try
                {
                    await using var lectorCall = await cmdCall.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                    if (!lectorCall.IsClosed)
                    {
                        // Si el procedimiento retorna resultset (procedures con OUT params no retornan filas)
                        if (lectorCall.FieldCount > 0)
                        {
                            tabla.Load(lectorCall);
                            return tabla;
                        }
                    }
                }
                catch
                {
                    // Ignorar para intentar como función
                }
            }

            // Intento 2: SELECT * FROM funcion(@a, @b, ...)
            string sqlSelect = $"SELECT * FROM {nombreSP}({placeHolders})";
            await using (var cmdSel = new NpgsqlCommand(sqlSelect, conexion))
            {
                AgregarParametros(cmdSel, parametros);
                await using var lectorSel = await cmdSel.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                tabla.Load(lectorSel);
            }

            return tabla;
        }

        // ============================================================
        // 4) Resolver esquema real de una tabla
        // ============================================================
        public async Task<string?> ObtenerEsquemaTablaAsync(string nombreTabla, string esquemaPredeterminado)
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            var cadena = _proveedorConexion.ObtenerCadenaConexion();
            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            // 4.1 Primero verifica en el esquema indicado
            const string sql1 = @"
                SELECT table_schema
                FROM information_schema.tables
                WHERE table_schema = @esquema AND table_name = @tabla
                LIMIT 1;";

            await using (var cmd1 = new NpgsqlCommand(sql1, conexion))
            {
                cmd1.Parameters.AddWithValue("esquema", esquemaPredeterminado ?? "public");
                cmd1.Parameters.AddWithValue("tabla", nombreTabla);

                var r1 = await cmd1.ExecuteScalarAsync();
                if (r1 != null && r1 is string s1) return s1;
            }

            // 4.2 Si no está, buscar en todos los esquemas visibles (excepto system)
            const string sql2 = @"
                SELECT table_schema
                FROM information_schema.tables
                WHERE table_name = @tabla
                  AND table_schema NOT IN ('pg_catalog','information_schema')
                ORDER BY table_schema
                LIMIT 1;";

            await using var cmd2 = new NpgsqlCommand(sql2, conexion);
            cmd2.Parameters.AddWithValue("tabla", nombreTabla);

            var r2 = await cmd2.ExecuteScalarAsync();
            return r2 == null ? null : Convert.ToString(r2);
        }

        // ============================================================
        // 5) Estructura detallada de una tabla → DataTable
        // ============================================================
        public async Task<DataTable> ObtenerEstructuraTablaAsync(string nombreTabla, string esquema)
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            var cadena = _proveedorConexion.ObtenerCadenaConexion();
            var tabla = new DataTable();

            const string sql = @"
                SELECT
                    c.table_schema      AS esquema,
                    c.table_name        AS tabla,
                    c.column_name       AS columna,
                    c.ordinal_position  AS posicion,
                    c.data_type         AS tipo_dato,
                    c.is_nullable       AS es_nulo,
                    c.character_maximum_length AS longitud,
                    c.numeric_precision AS precision,
                    c.numeric_scale     AS escala,
                    c.column_default    AS valor_por_defecto
                FROM information_schema.columns c
                WHERE c.table_schema = @esquema
                  AND c.table_name   = @tabla
                ORDER BY c.ordinal_position;";

            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conexion);
            cmd.Parameters.AddWithValue("esquema", string.IsNullOrWhiteSpace(esquema) ? "public" : esquema);
            cmd.Parameters.AddWithValue("tabla", nombreTabla);

            await using var lector = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            tabla.Load(lector);
            return tabla;
        }

        // ============================================================
        // 6) Estructura completa de la base de datos → DataTable
        // ============================================================
        public async Task<DataTable> ObtenerEstructuraBaseDatosAsync(string? nombreBD)
        {
            // nombreBD no es necesario en PostgreSQL para consultar information_schema
            var cadena = _proveedorConexion.ObtenerCadenaConexion();
            var tabla = new DataTable();

            const string sql = @"
                SELECT
                    c.table_schema  AS esquema,
                    c.table_name    AS tabla,
                    c.column_name   AS columna,
                    c.data_type     AS tipo_dato
                FROM information_schema.columns c
                WHERE c.table_schema NOT IN ('pg_catalog','information_schema')
                ORDER BY c.table_schema, c.table_name, c.ordinal_position;";

            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conexion);
            await using var lector = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            tabla.Load(lector);
            return tabla;
        }

        // ============================================================
        // Utilidades internas
        // ============================================================
        private static void AgregarParametros(NpgsqlCommand comando, List<SqlParameter>? parametros)
        {
            if (parametros == null || parametros.Count == 0) return;

            foreach (var p in parametros)
            {
                var np = new NpgsqlParameter
                {
                    ParameterName = LimpiarArroba(p.ParameterName),
                    Value = p.Value ?? DBNull.Value
                };

                // Si se necesita, aquí se puede mapear DbType/Size/Direction:
                if (p.Size > 0) np.Size = p.Size;
                np.Direction = p.Direction switch
                {
                    ParameterDirection.Input => ParameterDirection.Input,
                    ParameterDirection.Output => ParameterDirection.Output,
                    ParameterDirection.InputOutput => ParameterDirection.InputOutput,
                    ParameterDirection.ReturnValue => ParameterDirection.ReturnValue,
                    _ => ParameterDirection.Input
                };

                // Importante: en Npgsql los parámetros se nombran sin '@' al agregarlos,
                // pero al usarlos en SQL sí se referencian con @nombre.
                comando.Parameters.Add(np);
            }
        }

        private static string ConstruirPlaceholders(List<SqlParameter>? parametros)
        {
            if (parametros == null || parametros.Count == 0) return string.Empty;

            var nombres = new List<string>(parametros.Count);
            foreach (var p in parametros)
            {
                // En el texto SQL se usa @nombre
                var nombre = p.ParameterName?.StartsWith("@") == true ? p.ParameterName : $"@{p.ParameterName}";
                nombres.Add(nombre);
            }
            return string.Join(", ", nombres);
        }

        private static string LimpiarArroba(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return nombre;
            return nombre[0] == '@' ? nombre.Substring(1) : nombre;
        }
    }
}
