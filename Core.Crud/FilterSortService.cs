using Core.Data;
using Core.Filters;
using Core.Filters.Handlers;
using Core.Extensions;
using Core.Exceptions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Core.Crud
{
    public static class FilterSortService<TEntity, TKey> // : IFilterSortService<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        public static IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, Filter filter)
        {
            if (filter.Filters == null || !filter.Filters.Any()) return query;

            foreach (var groupedFilters in filter.Filters.GroupBy(a => a.PropertyName.ToLowerInvariant()))
            {
                Expression<Func<TEntity, bool>> orExpression = null;
                foreach (var filterInfo in groupedFilters)
                {
                    CheckFilter(filterInfo);
                    var expr = FilterHandlersProvider.ProvideFilter<TEntity>(filterInfo);
                    if (expr != null)
                    {
                        orExpression = orExpression == null ? expr : orExpression.Or(expr);
                    }
                }

                if (orExpression != null)
                {
                    query = query.Where(orExpression);
                }
            }

            return query;
        }

        public static IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, ISorter command)
        {
            if (command.SortField.EndsWith("_original")) command.SortField = command.SortField.Remove(command.SortField.Length - 9);
            var sortPropertyInfo = typeof(TEntity).GetProperty(command.SortField, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (sortPropertyInfo == null)
            {
                return query;
            }

            var item = Expression.Parameter(typeof(TEntity), "item");
            var property = Expression.Property(item, sortPropertyInfo);
            var lambda = Expression.Lambda(property, item);

            var method = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(a => a.Name == $"OrderBy{(command.IsAsc ? string.Empty : "Descending")}")
                .Single(a => a.GetParameters().Length == 2);
            method = method.MakeGenericMethod(typeof(TEntity), property.Type);
            return (IQueryable<TEntity>)method.Invoke(method, new object[] { query, lambda });
        }

        private static void CheckFilter(FilterInfoBase filter)
        {
            var propertyName = filter.PropertyName.Split('.');
            PropertyInfo entityPropertyInfo = null;
            var type = typeof(TEntity);

            foreach (var part in propertyName)
            {
                entityPropertyInfo = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (entityPropertyInfo != null)
                {
                    type = entityPropertyInfo.PropertyType;
                }
                else
                {
                    break;
                }
            }

            if (entityPropertyInfo == null)
            {
                throw new BusinessException($"Can not find '{filter.PropertyName}' property " + $"in '{typeof(TEntity).FullName}' entity type");
            }
        }
    }
}
