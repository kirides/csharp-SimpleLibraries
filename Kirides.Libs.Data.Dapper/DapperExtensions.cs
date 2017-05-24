using System;
using System.Linq;
using System.Data;
using Dapper;
using System.Collections.Generic;
using System.Reflection;

namespace Kirides.Libs.Data.Dapper
{
    public static class DapperExtensions
    {
        public static Dictionary<Type, PropertyInfo[]> cachedTypes = new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Inserts a new entry to the Database using the supplied entity
        /// </summary>
        /// <param name="connection">The DbConnection to use for this operation</param>
        /// <param name="entityToInsert">The Entity to use for inserting</param>
        /// <param name="table">(optional)Table name if the entity is anonymous or named differently than the table</param>
        /// <param name="idProperty">The property that defines the Id of the object</param>
        /// <param name="tran">(optional)The transaction to be used by the query</param>
        /// <returns>Amount of inserted Rows</returns>
        public static int Insert(
            this IDbConnection connection,
            object entityToInsert,
            string table = null,
            string idProperty = "Id",
            IDbTransaction tran = null)
        {
            const string insertTemplate = @"INSERT INTO {0} ({1}) VALUES ({2})";
            var type = entityToInsert.GetType();
            var properties = GetPropertyInfoFromType(type);

            // Collects all propertynames, except the Id-Column
            var propertyNames = properties.Select(p => p.Name).Where(p => p.ToUpper() != idProperty.ToUpperInvariant()).ToArray();

            // Constructs the Columns
            var names = string.Join(",", propertyNames);
            // Constructs the Parameters
            var values = string.Join(",", propertyNames.Select(n => "@" + n).ToArray());
            // Format the SQL
            var query = string.Format(insertTemplate,
                                        table ?? type.Name,
                                        names,
                                        values);

            return connection.Execute(query, entityToInsert, tran);
        }

        /// <summary>
        /// Updates a given entity using its "Id"-Property
        /// </summary>
        /// <param name="connection">The DbConnection to use for this operation</param>
        /// <param name="entityToUpdate">The Entity to use for Updating existing ones</param>
        /// <param name="table">(optional)Table name if the entity is anonymous or named differently than the table</param>
        /// <param name="idProperty">The property that defines the Id of the object</param>
        /// <param name="tran">(optional)The transaction to be used by the query</param>
        /// <returns>Id of the Row that got updated</returns>
        public static int Update(
            this IDbConnection connection,
            object entityToUpdate,
            string table = null,
            string idProperty = "Id",
            IDbTransaction tran = null)
        {
            const string insertTemplate = @"UPDATE {0} SET {1} WHERE Id=@";
            string template = insertTemplate + idProperty?.Replace(" ", "") ?? "Id";
            var type = entityToUpdate.GetType();
            // Check if it has a valid Id. 
            int Id = Convert.ToInt32((type.GetProperty(idProperty).GetValue(entityToUpdate, null)));
            if (Id == 0)
                return 0;

            var typeProps = GetPropertyInfoFromType(type);
            // Construct Columns - skip Id Column.
            var properties = typeProps.Select(p => p.Name).Where(p => p.ToUpperInvariant() != idProperty.ToUpperInvariant()).ToArray();
            // Construct Parameters (Ex. @ColumnName)
            var param = properties.Select(n => "@" + n).ToArray();
            // Combine them. (Ex Name=@Name)
            var combined = properties.Zip(param, (first, second) => first + "=" + second).ToArray();
            // Join all Properties together. (Name=@Name,Age=@Age,...)
            var qVal = string.Join(",", combined);
            // Format the SQL
            var query = string.Format(insertTemplate,
                                        table ?? type.Name,
                                        qVal,
                                        Id);

            connection.Execute(query, entityToUpdate, tran);
            return Id;
        }


        private static PropertyInfo[] GetPropertyInfoFromType(Type type)
        {
            lock (cachedTypes)
            {
                if (cachedTypes.TryGetValue(type, out PropertyInfo[] properties))
                {
                    return properties;
                }
                else
                {
                    var props = type.GetProperties();
                    cachedTypes.Add(type, props);
                    return props;
                }
            }
        }
    }
}
