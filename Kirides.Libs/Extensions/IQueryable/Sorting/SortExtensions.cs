using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kirides.Libs.Extensions.IQueryable.Sorting
{
    public static class SortExtensions
    {
        #region Private Methods
        private static LambdaExpression GetSorting<TSource>(IEnumerable<TSource> source, string column)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            string[] parts = new[] { column };
            
            if (column.Contains(".")) parts = column.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            else parts = new[] { column };

            // x ...
            var parameter = Expression.Parameter(typeof(TSource), "x");
            // x ... x.[part1].[part2].[partX]
            Expression property = null;
            for (int i = 0; i < parts.Length; i++)
            {
                if (property == null) property = Expression.Property(parameter, parts[i]);
                else property = Expression.Property(property, parts[i]);
            }

            // x => x.[column]
            return Expression.Lambda(property, new ParameterExpression[] { parameter });
        }
        #endregion

        #region Public API
        public static IOrderedQueryable<TSource> SortBy<TSource>(this IQueryable<TSource> source, string column, SortDirection sortDirection = SortDirection.asc)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            // x => x.[column]
            var sortSelector = GetSorting(source, column);

            // source.OrderBy(x => x.[column])
            MethodCallExpression orderByCallExpression = Expression.Call(
                typeof(Queryable),
                sortDirection == SortDirection.asc ? "OrderBy" : "OrderByDescending",
                new Type[] { source.ElementType, sortSelector.Body.Type },
                source.Expression,
                sortSelector);

            return source.Provider.CreateQuery<TSource>(orderByCallExpression) as IOrderedQueryable<TSource>;
        }
        public static IOrderedQueryable<TSource> ThenSortBy<TSource>(this IOrderedQueryable<TSource> source, string column, SortDirection sortDirection = SortDirection.asc)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            // x => x.[column]
            var sortSelector = GetSorting(source, column);
            // source.ThenBy(x => x.[column])
            MethodCallExpression thenByCallExpression = Expression.Call(
                typeof(Queryable),
                sortDirection == SortDirection.asc ? "ThenBy" : "ThenByDescending",
                new Type[] { source.ElementType, sortSelector.Body.Type },
                source.Expression,
                sortSelector);

            return source.Provider.CreateQuery<TSource>(thenByCallExpression) as IOrderedQueryable<TSource>;
        }
        #endregion
    }

    public enum SortDirection
    {
        asc,
        desc
    }
}
