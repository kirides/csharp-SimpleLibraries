using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Kirides.Libs.Extensions.IQueryable.Search
{
    public static class IQueryableSearchExtensions
    {
        /// <summary>
        /// The "string.Contains(..)"-method
        /// </summary>
        static MethodInfo containsMethod = typeof(string).GetMethod("Contains", new Type[] { typeof(string) });

        /// <summary>
        /// Let's you search for keywords inside every public "string"-type variable of the given type
        /// </summary>
        /// <typeparam name="TSource">The type of the collection</typeparam>
        /// <param name="queryable">The collection that this should be applied on</param>
        /// <param name="searchKey">The keyword(s) to look out for, seperated by an empty space</param>
        /// <param name="exactMatch">If the key should be looked at as a whole, instead of each keysword seperately</param>
        /// <param name="matchAllWords">If every keyword needs to be found inside the object</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        public static IQueryable<TSource> FullTextSearch<TSource>(this IQueryable<TSource> queryable, string searchKey,
                                                      bool exactMatch = false, bool matchAllWords = true)
        {
            if (queryable == null) throw new ArgumentNullException(nameof(queryable));
            if (searchKey == null) throw new ArgumentNullException(nameof(searchKey));

            ParameterExpression parameter = Expression.Parameter(typeof(TSource), "c");

            var publicProperties =
                typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                          .Where(p => p.PropertyType == typeof(string));

            Expression filterExpression = null;
            string[] searchKeyParts;

            searchKeyParts = !exactMatch ? searchKey.Split(' ') : new[] { searchKey };
            string prevProp = null;

            foreach (MethodCallExpression callContainsMethod in from property in publicProperties
                                                                select Expression.Property(parameter, property) into nameProperty
                                                                from searchKeyPart in searchKeyParts

                                                                let searchKeyExpression = Expression.Constant(searchKeyPart)
                                                                let containsParamConverted = Expression.Convert(searchKeyExpression, typeof(string))

                                                                select Expression.Call(nameProperty, containsMethod, (Expression)containsParamConverted))
            {
                if (filterExpression == null)
                { filterExpression = callContainsMethod; }
                else
                {
                    if (matchAllWords)
                    {
                        //Too stupid to figure out why it wont work with only 1 word. Complex stuff is complex...
                        if (searchKeyParts.Length > 1)
                        {
                            var aktProp = (callContainsMethod.Object as MemberExpression);
                            if (aktProp.Member.Name != prevProp)
                            {
                                filterExpression = Expression.Or(filterExpression, callContainsMethod);
                                prevProp = aktProp.Member.Name;
                            }
                            filterExpression = Expression.And(filterExpression, callContainsMethod);
                        }
                        else
                        {
                            filterExpression = Expression.Or(filterExpression, callContainsMethod);
                        }
                    }
                    else
                    { filterExpression = Expression.Or(filterExpression, callContainsMethod); }
                }
            }


            MethodCallExpression whereCallExpression = Expression.Call(
                typeof(Queryable),
                "Where",
                new Type[] { queryable.ElementType },
                queryable.Expression,
                Expression.Lambda<Func<TSource, bool>>(filterExpression, new ParameterExpression[] { parameter }));

            return queryable.Provider.CreateQuery<TSource>(whereCallExpression);
        }

        /// <summary>
        /// Let's you search for keywords inside a "string"-type variable of the given type
        /// </summary>
        /// <typeparam name="TSource">The type of the collection</typeparam>
        /// <param name="queryable">The collection that this should be applied on</param>
        /// <param name="value">The expression that points to a "string"-property/field</param>
        /// <param name="searchKey">The keyword(s) to look out for, seperated by an empty space</param>
        /// <param name="exactMatch">If the key should be looked at as a whole, instead of each keysword seperately</param>
        /// <param name="matchAllWords">If every keyword needs to be found inside the <paramref name="value"/></param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        public static IQueryable<TSource> FullTextSearch<TSource>(this IQueryable<TSource> queryable, Expression<Func<TSource, string>> value, string searchKey,
                                              bool exactMatch = false, bool matchAllWords = true)
        {
            if (queryable == null) throw new ArgumentNullException(nameof(queryable));
            if (searchKey == null) throw new ArgumentNullException(nameof(searchKey));

            var member = value.Body as MemberExpression;
            if (member == null) throw new InvalidOperationException($"{nameof(value)} has an invalid format!");

            var property = member.Member as PropertyInfo;
            if (property == null) throw new InvalidOperationException($"{nameof(value)} does not point to a property or field!");

            ParameterExpression parameter = Expression.Parameter(typeof(TSource), "c");
            Expression filterExpression = null;
            string[] searchKeyParts;

            searchKeyParts = !exactMatch ? searchKey.Split(' ') : new[] { searchKey };

            var parts = searchKeyParts
                .Select(x => Expression.Convert(Expression.Constant(x), typeof(string)))
                .Select((Expression convertedExpression) => Expression.Call(Expression.Property(parameter, property), containsMethod, convertedExpression));

            foreach (var callContainsMethod in parts)
            {
                if (filterExpression == null)
                { filterExpression = callContainsMethod; }
                else
                {
                    if (matchAllWords)
                    { filterExpression = Expression.And(filterExpression, callContainsMethod); }
                    else
                    { filterExpression = Expression.Or(filterExpression, callContainsMethod); }
                }
            }

            MethodCallExpression whereCallExpression = Expression.Call(
                typeof(Queryable),
                "Where",
                new Type[] { queryable.ElementType },
                queryable.Expression,
                Expression.Lambda<Func<TSource, bool>>(filterExpression, new ParameterExpression[] { parameter }));

            return queryable.Provider.CreateQuery<TSource>(whereCallExpression);
        }

    }
}
