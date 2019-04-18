using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kirides.Libs.Data
{
    public class SimpleDbMapper
    {
        ThreadLocal<object[]> buffer = new ThreadLocal<object[]>(() => new object[5]);

        public async Task<T> GetAsync<T>(DbConnection dbConnection, string sql, object parameters, CancellationToken cancellationToken)
        {
            var rdr = await GetReaderAsync(dbConnection, sql, parameters, cancellationToken).ConfigureAwait(false);

            if (rdr == null || !rdr.HasRows)
            {
                return default;
            }

            await rdr.ReadAsync(cancellationToken).ConfigureAwait(false);
            var rowValues = GetBuffer(rdr.VisibleFieldCount);
            var totalValues = rdr.GetValues(rowValues);

            var factory = ObjectFactory<T>.Create(GenerateColumns(rdr, totalValues, rowValues));

            T result = factory(rowValues);
            return result;
        }
        public async Task<List<T>> QueryAsync<T>(DbConnection dbConnection, string sql, object parameters, CancellationToken cancellationToken)
        {
            var rdr = await GetReaderAsync(dbConnection, sql, parameters, cancellationToken).ConfigureAwait(false);
            if (rdr == null || !rdr.HasRows)
            {
                return default;
            }

            if (!await rdr.ReadAsync(cancellationToken).ConfigureAwait(false)) return default;

            var rowValues = GetBuffer(rdr.VisibleFieldCount);
            var totalValues = rdr.GetValues(rowValues);

            var factory = ObjectFactory<T>.Create(GenerateColumns(rdr, totalValues, rowValues));

            List<T> result = new List<T>
            {
                factory(rowValues)
            };
            while (await rdr.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rdr.GetValues(rowValues);
                result.Add(factory(rowValues));
            }

            return result;
        }

        public IEnumerable<T> QueryLazy<T>(DbConnection dbConnection, string sql, object parameters)
        {
            var rdr = GetReaderAsync(dbConnection, sql, parameters, CancellationToken.None).GetAwaiter().GetResult();
            if (rdr != null && rdr.HasRows && rdr.Read())
            {
                var rowValues = GetBuffer(rdr.VisibleFieldCount);
                var totalValues = rdr.GetValues(rowValues);

                var factory = ObjectFactory<T>.Create(GenerateColumns(rdr, totalValues, rowValues));

                yield return factory(rowValues);

                while (rdr.Read())
                {
                    rdr.GetValues(rowValues);
                    yield return factory(rowValues);
                }
            }
        }

        object[] GetBuffer(int minSize)
        {
            if (buffer.Value.Length < minSize)
            {
                buffer.Value = new object[minSize];
            }
            return buffer.Value;
        }

        IEnumerable<TableColumn> GenerateColumns(DbDataReader reader, int totalColumns, object[] rows)
        {
            for (int i = 0; i < totalColumns; i++)
                yield return new TableColumn { ColumnName = reader.GetName(i), DataType = rows[i].GetType() };
        }

        private Task<DbDataReader> GetReaderAsync(DbConnection dbConnection, string sql, object parameters, CancellationToken cancellationToken)
        {
            using (var cmd = dbConnection.CreateCommand())
            {
                cmd.CommandText = sql;
                if (parameters != null)
                {
                    AddCommandParameters(cmd, parameters);
                }
                return cmd.ExecuteReaderAsync(cancellationToken);
            }
        }


        private void AddCommandParameters(DbCommand cmd, object parameters)
        {
            var paramsType = parameters.GetType();
            var props = paramsType.GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = "@" + props[i].Name;
                param.Value = props[i].GetValue(parameters, null);
                cmd.Parameters.Add(param);
            }
        }

        private struct TableColumn
        {
            public Type DataType { get; set; }
            public string ColumnName { get; set; }
        }

        private static class ObjectFactory<T>
        {
            static Func<object[], T> factory = null;

            public static Func<object[], T> Create(IEnumerable<TableColumn> cols)
            {
                if (factory != null)
                {
                    return factory;
                }
                var type = typeof(T);
                if (type.IsPrimitive)
                {
                    return factory = (data) => (T)data[0];
                }
                var props = type.GetProperties().ToList<MemberInfo>().Concat(type.GetFields()).ToDictionary(k => k.Name, t => t);
                var toAdd = new Dictionary<string, MemberInfo>();
                foreach (var kvp in props)
                {
                    var customName = kvp.Value.GetCustomAttribute<ColumnAttribute>()?.Name;
                    if ((customName != null) && (customName != kvp.Key))
                    {
                        toAdd.Add(customName, kvp.Value);
                    }
                }
                if (toAdd.Count > 0)
                {
                    props = props.Concat(toAdd).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }

                var ctorDepsExpr = Expression.Parameter(typeof(object[]), "args");
                List<MemberBinding> memberBindings = new List<MemberBinding>();

                int idx = -1;
                // { X = (type)args[0], Y = (type)args[1], ... }
                foreach (var col in cols)
                {
                    idx++;
                    if (!props.TryGetValue(col.ColumnName, out var prop))
                    {
                        continue;
                    }
                    var index = Expression.Constant(idx);

                    var elementAtIndex = Expression.ArrayIndex(ctorDepsExpr, index);
                    var memberType = (prop as PropertyInfo)?.PropertyType ?? (prop as FieldInfo)?.FieldType ?? throw new InvalidCastException("Not a Property or Field");
                    var convertExpression = ValueOrDefaultExpression(elementAtIndex, memberType);

                    memberBindings.Add(Expression.Bind(prop, convertExpression));
                }
                // new Item() { X = (type)args[0], Y = (type)args[1], ... }
                var minit = Expression.MemberInit(Expression.New(type), memberBindings);

                // args => new Item() { X = (type)args[0], Y = (type)args[1], ... }
                var lambda = Expression.Lambda<Func<object[], T>>(minit, ctorDepsExpr);
                return factory = lambda.Compile();
            }
        }
        private static readonly ConstantExpression DBNullExpression = Expression.Constant(DBNull.Value);
        private static ConditionalExpression ValueOrDefaultExpression(Expression value, Type defaultType)
        {
            return Expression.Condition(
                Expression.Equal(value, DBNullExpression),
                Expression.Default(defaultType),
                Expression.Convert(value, defaultType));


        }
    }
}
