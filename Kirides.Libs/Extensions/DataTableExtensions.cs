using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kirides.Libs.Extensions
{
    public static class DataTableExtensions
    {
        private static ConcurrentDictionary<Type, Func<object[], object>> cachedFactories = new ConcurrentDictionary<Type, Func<object[], object>>();

        /// <summary>
        /// Converts a DataTable to a <see cref="List{T}"/>
        /// </summary>
        /// <typeparam name="T">Type to Map this Datatable to</typeparam>
        public static List<T> ToTypedList<T>(this DataTable dataTable) where T : new()
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            List<T> result = new List<T>(dataTable.Rows.Count);
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var it = FromDataRow<T>(dataTable.Rows[i].ItemArray, dataTable.Columns);
                result.Add(it);
            }

            return result;
        }

        private static Func<object[], T> CreateFactory<T>(IDictionary<string, Type> cols)
        {
            var type = typeof(T);
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
            foreach (var kvp in cols)
            {
                idx++;
                if (!props.TryGetValue(kvp.Key, out var prop))
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
            return lambda.Compile();
        }

        private static T FromDataRow<T>(object[] values, DataColumnCollection cols)
        {
            var type = typeof(T);

            if (!cachedFactories.TryGetValue(type, out var factory))
            {
                Dictionary<string, Type> columns = new Dictionary<string, Type>();
                for (int i = 0; i < cols.Count; i++)
                {
                    columns.Add(cols[i].ColumnName, cols[i].DataType);
                }
                var genericFactory = CreateFactory<T>(columns);
                factory = (args) => genericFactory(args);
                cachedFactories.TryAdd(type, factory);
            }

            T itm = (T)factory(values);
            return itm;
        }

        private static readonly ConstantExpression DBNullExpression = Expression.Constant(DBNull.Value);
        private static ConditionalExpression ValueOrDefaultExpression(Expression value, Type defaultType)
        {
            return Expression.Condition(
                Expression.Equal(value, DBNullExpression),
                defaultType.IsValueType ? (Expression)Expression.Default(defaultType) : Expression.Constant(null, defaultType),
                Expression.Convert(value, defaultType));
        }
    }
}