using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Samples.Data.Exceptions;
using Const = Samples.Data.Constants.DBErrors;
using Samples.Data.Constants;
using Samples.Data.Helpers;
using System.Data;
using System.Linq;
using Samples.Data.Models;

namespace Samples.Data
{
    public class Db
    {
        public static Task<List<T>> All<T>(
            string sql,
            dynamic sqlParams) where T : new()
        {
            return runSQL<T>(sql, sqlParams, itemsToFetch: -1);
        }        
        
        public static async Task<T> One<T>(
            string sql,
            dynamic sqlParams) where T : new()
        {
            var results = await runSQL<T>(sql, sqlParams, itemsToFetch: 1).ConfigureAwait(false);

            var result = new T();
            
            if (results.Count > 0)
            {
                result = results[0];
            }
            else
            {
                result = default(T);
            }

            return result;
        }

        private static async Task<List<T>> runSQL<T>(
            string sql,
            dynamic sqlParams,
            int itemsToFetch = -1) where T : new()
        {
            T resObj = new T();
            Type resObjType = resObj.GetType();

            var resItems = new List<T>();

            try
            {
                using (var conn = Db.Connection)
                {
                    await conn.OpenAsync();

                    // Run the query
                    await runSQL<T>(
                        sql,
                        sqlParams,
                        conn,
                        resItems,
                        itemsToFetch).ConfigureAwait(false);
                    
                    // Put back into the pool
                    conn.Close();
                }
            }
            catch (PostgresException ex)
            {
                var errorCode = string.Empty;
                switch(ex.SqlState)
                {
                    case Const.INVALID_BINDING:
                        errorCode = Errors.INVALID_BINDING;
                        break;
                    case Const.INVALID_PARAM:
                        errorCode = Errors.INVALID_REQUEST;
                        break;
                    case Const.NOT_NULL:
                        errorCode = Errors.MISSING_VALUE;
                        break;
                    case Const.RESTRICTED:
                        errorCode = Errors.UNAUTHORIZED;
                        break;
                    case Const.FOREIGN_KEY:
                        errorCode = Errors.PARENT_MISSING;
                        break;
                    case Const.UNIQUE:
                        errorCode = Errors.DUPLICATE;
                        break;
                    case Const.NO_DATA:
                        errorCode = Errors.NOT_FOUND;
                        break;
                    default:
                        errorCode = $"DB Error: {ex.SqlState}";
                        break;
                }

                throw new DBException(errorCode, ex.Detail ?? ex.Hint, ex);
            }
            catch (TimeoutException ex)
            {
                throw new DBException(Errors.TIMEOUT, "DB timeout", ex);
            }
            catch (Exception ex)
            {
                throw new DBException(Errors.GENERAL, "DB Error", ex);
            }

            // Return the results normally
            return resItems;
        }

        private static async Task runSQL<T>(
            string sql,
            dynamic parameters,
            NpgsqlConnection conn,
            List<T> results,
            int max_rows = -1) where T : new()
        {
            T resObj = new T();
            Type resObjType = resObj.GetType();

            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    // Add current user id and company
                    var ctx = ContextHelper.GetContext();

                    using (var cmd = new NpgsqlCommand(sql, conn, tran))
                    {
                        addParamsBinding(cmd, parameters);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader != null)
                            {
                                while (await reader.ReadAsync())
                                {
                                    T pItem = new T();

                                    for (int iField = 0; iField < reader.FieldCount; iField++)
                                    {
                                        var fieldName = reader.GetName(iField);
                                        var dbValue = reader[iField];
                                        var property = resObjType.GetProperty(fieldName);

                                        if (dbValue == DBNull.Value)
                                        {
                                            continue;
                                        }

                                        if (property != null)
                                        {
                                            var value = dbValue;
                                            if (property.PropertyType == typeof(HtmlString))
                                            {
                                                value = (HtmlString)dbValue.ToString();
                                            }
                                            else
                                            if (property.PropertyType == typeof(JsonString))
                                            {
                                                value = (JsonString)dbValue.ToString();
                                            }
                                            else
                                            if (property.PropertyType == typeof(HashIdString))
                                            {
                                                value = new HashIdString((long)dbValue);
                                            }

                                            property.SetValue(pItem, value);
                                        }
                                    }

                                    results.Add(pItem);

                                    if (max_rows >= 0 && results.Count > max_rows)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        await tran.CommitAsync();
                    }
                }
                catch
                {
                    await tran.RollbackAsync();
                    throw;
                }
            }
        }


        public static Task<List<T>> AllSproc<T>(
            string sql,
            dynamic sqlParams) where T : BaseModel, new()
        {
            return runSproc<T>(sql, sqlParams, itemsToFetch: -1);
        }

        public static async Task<T> OneSproc<T>(
            string sql,
            dynamic sqlParams) where T : BaseModel, new()
        {
            var results = await runSproc<T>(sql, sqlParams, itemsToFetch: 1).ConfigureAwait(false);

            var result = new T();

            if (results.Count > 0)
            {
                result = results[0];
            }
            else
            {
                result = default(T);
            }

            return result;
        }

        private static async Task<List<T>> runSproc<T>(
            string sprocName,
            dynamic sqlParams,
            int itemsToFetch = -1) where T : BaseModel, new()
        {
            T resObj = new T();
            Type resObjType = resObj.GetType();

            var resItems = new List<T>();

            try
            {
                var ctx = ContextHelper.GetContext();
                if (ctx.HasTransaction)
                {
                    // Run the query
                    await runSproc<T>(
                        sprocName,
                        sqlParams,
                        ctx.trans.Connection,
                        resItems,
                        itemsToFetch).ConfigureAwait(false);
                }
                else
                {
                    using (var conn = Db.Connection)
                    {
                        await conn.OpenAsync();

                        // Run the query
                        await runSproc<T>(
                            sprocName,
                            sqlParams,
                            conn,
                            resItems,
                            itemsToFetch).ConfigureAwait(false);

                        // Put back into the pool
                        conn.Close();
                    }
                }
            }
            catch (PostgresException ex)
            {
                var errorCode = string.Empty;
                switch (ex.SqlState)
                {
                    case Const.INVALID_BINDING:
                        errorCode = Errors.INVALID_BINDING;
                        break;
                    case Const.INVALID_SPROC:
                        errorCode = Errors.INVALID_SPROC;
                        break;
                    case Const.INVALID_PARAM:
                        errorCode = Errors.INVALID_REQUEST;
                        break;
                    case Const.NOT_NULL:
                        errorCode = Errors.MISSING_VALUE;
                        break;
                    case Const.RESTRICTED:
                        errorCode = Errors.UNAUTHORIZED;
                        break;
                    case Const.FOREIGN_KEY:
                        errorCode = Errors.PARENT_MISSING;
                        break;
                    case Const.UNIQUE:
                        errorCode = Errors.DUPLICATE;
                        break;
                    case Const.NO_DATA:
                        errorCode = Errors.NOT_FOUND;
                        break;
                    default:
                        errorCode = $"DB Error: {ex.SqlState}";
                        break;
                }

                throw new DBException(errorCode, ex.Detail ?? ex.Hint, ex);
            }
            catch (TimeoutException ex)
            {
                throw new DBException(Errors.TIMEOUT, "DB timeout", ex);
            }
            catch (Exception ex)
            {
                throw new DBException(Errors.GENERAL, "DB Error", ex);
            }

            // Return the results normally
            return resItems;
        }

        private static async Task runSproc<T>(
            string sprocName,
            dynamic parameters,
            NpgsqlConnection conn,
            List<T> results,
            int max_rows = -1) where T : BaseModel, new()
        {
            T resObj = new T();
            Type resObjType = resObj.GetType();

            var ctx = ContextHelper.GetContext();
            if (ctx.HasTransaction)
            {
                await runSprocWithTran<T>(
                    sprocName,
                    parameters,
                    ctx.trans,
                    results,
                    max_rows);
            }
            else
            {
                // Start a new transaction
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Pass the  current user id and company to DB
                        await SetDbContext(tran);

                        await runSprocWithTran<T>(
                            sprocName,
                            parameters,
                            tran,
                            results,
                            max_rows);

                        await tran.CommitAsync();
                    }
                    catch
                    {
                        await tran.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        private static async Task runSprocWithTran<T>(
            string sprocName,
            dynamic parameters,
            NpgsqlTransaction tran,
            List<T> results,
            int max_rows = -1) where T : BaseModel, new()
        {
            T resObj = new T();
            Type resObjType = resObj.GetType();

            // Run the requested sproc
            using (var cmd = new NpgsqlCommand(sprocName, tran.Connection, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                addParamsBinding(cmd, parameters);

                using (var reader = await CallSprocWithCursor(cmd))
                {
                    if (reader != null)
                    {
                        while (await reader.ReadAsync())
                        {
                            T pItem = new T();

                            for (int iField = 0; iField < reader.FieldCount; iField++)
                            {
                                var fieldName = reader.GetName(iField);
                                var dbValue = reader[iField];
                                var property = resObjType.GetProperty(fieldName);

                                if (dbValue == DBNull.Value)
                                {
                                    continue;
                                }

                                if (property != null)
                                {
                                    var value = dbValue;
                                    if (property.PropertyType == typeof(HtmlString))
                                    {
                                        value = (HtmlString)dbValue.ToString();
                                    }
                                    else
                                    if (property.PropertyType == typeof(JsonString))
                                    {
                                        value = (JsonString)dbValue.ToString();
                                    }
                                    else
                                    if (property.PropertyType == typeof(HashIdString))
                                    {
                                        value = new HashIdString((long)dbValue);
                                    }

                                    property.SetValue(pItem, value);
                                }
                                else
                                {
                                    // Place all the unknown fields' values into the aux of BaseModel
                                    pItem.aux.Add(new FieldValue { name = fieldName, value = dbValue.ToString() });
                                }
                            }

                            results.Add(pItem);

                            if (max_rows >= 0 && results.Count > max_rows)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            if (ContextHelper.GetContext().HasTransaction)
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("CLOSE ALL;", tran.Connection, tran))
                    {
                        cmd.CommandType = CommandType.Text;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch
                {
                    ; // Ignore
                }
            }
        }

        public static async Task<NpgsqlTransaction> BeginTransaction()
        {
            var context = ContextHelper.GetContext();
            var conn = Db.Connection;
            try
            {
                await conn.OpenAsync();


                context.trans = conn.BeginTransaction();
                try
                {
                    await SetDbContext(context.trans);
                }
                catch
                {
                    context.trans.Rollback();
                    context.trans.Dispose();
                    context.trans = null;
                    throw;
                }
            }
            catch
            {
                conn.Close();
                conn.Dispose();
                throw;
            }

            return context.trans;
        }

        public static async Task CommitTransaction()
        {
            var context = ContextHelper.GetContext();
            if(!context.HasTransaction)
            {
                return;
            }


            var trans = context.trans;
            var conn = trans.Connection;
            context.trans = null;
            try
            {
                await trans.CommitAsync();
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }
        }

        public static async Task RollbackTransaction()
        {
            var context = ContextHelper.GetContext();
            if (!context.HasTransaction)
            {
                return;
            }


            var trans = context.trans;
            var conn = trans.Connection;
            context.trans = null;
            try
            {
                await trans.RollbackAsync();
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }
        }

        private static async Task<System.Data.Common.DbDataReader> CallSprocWithCursor(NpgsqlCommand cmd)
        {
            // Transparently dereference returned cursors, where possible
            bool cursors = false;
            bool noncursors = false;
            string cursorName = string.Empty;
            using (var reader = await cmd.ExecuteReaderAsync())
            {

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.GetDataTypeName(i) == "refcursor")
                    {
                        if (cursors)
                        {
                            throw new InvalidOperationException("Sproc should always return one cursor");
                        }

                        // Read the first record
                        var read = await reader.ReadAsync();
                        if (!read)
                        {
                            break;
                        }
                        cursors = true;
                        if(reader.IsDBNull(i))
                        {
                            break;
                        }
                        cursorName = reader.GetString(i);
                    }
                    else
                    {
                        noncursors = true;
                    }
                }
            }

            // Don't consider dereferencing if no returned columns are cursors
            if (cursors && !string.IsNullOrEmpty(cursorName))
            {
                // Iff dereferencing was turned on, this will stop and complain if some but not all columns are cursors
                if (noncursors)
                {
                    throw new InvalidOperationException("Command returns both cursor and non-cursor results.");
                }

                cmd.CommandText = $"FETCH ALL in \"{cursorName}\";";
                cmd.CommandType = CommandType.Text;
                return await cmd.ExecuteReaderAsync();
            }
            return null;
        }

        public static async Task SetDbContext(NpgsqlTransaction tran)
        {
            var ctx = ContextHelper.GetContext();
            var hostName    = !string.IsNullOrEmpty(ctx.host) ? $"'{ctx.host}'" : "''";
            var userId      = ctx.user_id.HasValue ? $"'{ctx.signin_user_id}'" : "''";
            var loginUserId = ctx.user_id.HasValue ? $"'{ctx.user_id}'" : "''";
            var companyId   = ctx.company_id.HasValue ? $"'{ctx.company_id.Value}'" : "''";
            var sql = $@"
                SET LOCAL {DBContextStrings.SCHEMA_NAME}.{DBContextStrings.HOST_NAME} TO {hostName};
                SET LOCAL {DBContextStrings.SCHEMA_NAME}.{DBContextStrings.USER_ID_NAME} TO {userId};
                SET LOCAL {DBContextStrings.SCHEMA_NAME}.{DBContextStrings.SIGNIN_USER_ID_NAME} TO {loginUserId};
                SET LOCAL {DBContextStrings.SCHEMA_NAME}.{DBContextStrings.COMPANY_ID} TO  {companyId};";
  
            using (var cmd = new NpgsqlCommand(sql, tran.Connection, tran))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static void addParamsBinding(NpgsqlCommand cmd, dynamic sqlParams)
        {
            foreach (PropertyDescriptor prop in  TypeDescriptor.GetProperties(sqlParams))
            {
                string propName = prop.Name;
                object propValueObj = prop.GetValue(sqlParams);
                Type propType = prop.PropertyType;

                if(propType == null || propType == typeof(List<FieldValue>))
                {
                    continue;
                }

                cmd.Parameters.Add(buildParam(propType, propName, propValueObj));
            }
        }

        private static NpgsqlParameter buildParam(Type type, string name, object value)
        {
            var param = new NpgsqlParameter(name, null);

            if (value == null)
            {
                if (isNullableType(type))
                {
                    var realType = Nullable.GetUnderlyingType(type);
                    object dummy = Activator.CreateInstance(realType);
                    param.NpgsqlDbType = convertType(dummy);
                }
                else
                {
                    param.NpgsqlDbType = convertType(type);
                }
                
                param.Value = DBNull.Value;
            }
            else
            {
                param.NpgsqlDbType = convertType(value);
                
                if (param.NpgsqlDbType == NpgsqlDbType.Text && (value == null || value.ToString() == null || value.ToString().Length == 0))
                {
                    param.Value = DBNull.Value;
                }
                else
                {
                    var valueType = (value is Type) ? (value as Type) : value.GetType();
                    if (valueType == typeof(HtmlString))
                    {
                        param.Value = (value as HtmlString).ToString();
                    }
                    else
                    if (valueType == typeof(JsonString))
                    {
                        param.Value = (value as JsonString).ToString();
                    }
                    else
                    if (valueType == typeof(HashIdString))
                    {
                        param.Value = (value as HashIdString).Id.Value;
                    }
                    else
                    {
                        param.Value = value;
                    }
                }
            }

            return param;
        }

        private static NpgsqlDbType convertType(object value)
        {
            NpgsqlDbType resDbType = NpgsqlDbType.Unknown;
            var valueType = (value is Type) ? (value as Type) : value.GetType();
            
            if (valueType == typeof(string))
            {
                resDbType = NpgsqlDbType.Text;
            }
            else
            if (valueType == typeof(HtmlString) || valueType == typeof(JsonString))
            {
                resDbType = NpgsqlDbType.Text;
            }
            else
            if (valueType == typeof(Guid))
            {
                resDbType = NpgsqlDbType.Uuid;
            }
            else
            if (valueType == typeof(int) || valueType == typeof(Enum))
            {
                resDbType = NpgsqlDbType.Integer;
            }
            else
            if (valueType == typeof(long) || valueType == typeof(HashIdString))
            {
                resDbType = NpgsqlDbType.Bigint;
            }
            else
            if (valueType == typeof(bool))
            {
                resDbType = NpgsqlDbType.Boolean;
            }
            else
            if (valueType == typeof(DateTime))
            {
                resDbType = NpgsqlDbType.Timestamp;
            }
            else
            if (valueType == typeof(double))
            {
                resDbType = NpgsqlDbType.Double;
            }
            else
            if (valueType == typeof(decimal))
            {
                resDbType = NpgsqlDbType.Numeric;
            }
            else
            if (valueType == typeof(float))
            {
                resDbType = NpgsqlDbType.Real;
            }
            else
            if (valueType == typeof(JObject) || valueType == typeof(JToken))
            {
                resDbType = NpgsqlDbType.Json;
            }
            else
            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type itemType = valueType.GetGenericArguments().FirstOrDefault();
                if(itemType == null)
                {
                    throw new Exception(Const.ERR_PARAM_BIND);
                }

                if (isNullableType(itemType))
                {
                    itemType = Nullable.GetUnderlyingType(itemType);
                }

                resDbType = NpgsqlDbType.Array | convertType(itemType);
            }
            else
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(valueType))
            {
                Type itemType = valueType.GetElementType();

                if (isNullableType(itemType))
                {
                    itemType = Nullable.GetUnderlyingType(itemType);
                }

                resDbType = NpgsqlDbType.Array | convertType(itemType);
            }
            else
            {
                throw new Exception(Const.ERR_PARAM_BIND);
            }
            return resDbType;
        }

        private static bool isNullableType(Type objType)
        {
            if (!objType.GetTypeInfo().IsGenericType)
                return false;

            return objType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        
        public static NpgsqlConnection Connection
        {
            get
            {
                if (ConnectionString == null)
                {
                    return null;
                }

                return new NpgsqlConnection(ConnectionString);
            }
        }
        
        public static string ConnectionString { get; set; }
    }
}
