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
            // x ...
            var parameter = Expression.Parameter(typeof(TSource), "x");
            // x ... x.[column]
            var property = Expression.Property(parameter, column);

            // x => x.[column]
            return Expression.Lambda(property, new ParameterExpression[] { parameter });
        }
        #endregion

        #region Public API
        public static IQueryable<TSource> SortBy<TSource>(this IQueryable<TSource> source, string column, SortDirection sortDirection = SortDirection.asc)
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

            return source.Provider.CreateQuery<TSource>(orderByCallExpression);
        }
        public static IQueryable<TSource> ThenSortBy<TSource>(this IQueryable<TSource> source, string column, SortDirection sortDirection = SortDirection.asc)
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

            return source.Provider.CreateQuery<TSource>(thenByCallExpression);
        }
        #endregion
    }

    public enum SortDirection
    {
        asc,
        desc
    }
}
