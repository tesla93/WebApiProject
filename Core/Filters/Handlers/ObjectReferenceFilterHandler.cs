using System;
using System.Linq.Expressions;

namespace Core.Filters.Handlers
{
    internal class ObjectReferenceFilterHandler : ValueFilterHandlerBase
    {
        private readonly ObjectReferenceFilter filter;

        public ObjectReferenceFilterHandler(ObjectReferenceFilter filter)
        {
            this.filter = filter;
        }

        public override Expression<Func<TEntity, bool>> Handle<TEntity>()
        {
            var item = Expression.Parameter(typeof(TEntity), "item");
            Expression instance = GetProperty(item, filter.PropertyName);
            var property = Expression.Property(instance, "Id");
            if (property.Type == typeof(int))
            {
                var constant = Expression.Constant(filter.Value);
                var method = typeof(int).GetMethod("Equals", new Type[] { typeof(int) });
                var body = Expression.Call(property, method, constant);
                return Expression.Lambda<Func<TEntity, bool>>(body, item);
            }
            if (property.Type == typeof(Guid))
            {
                var constant = Expression.Constant(filter.Value);
                var method = typeof(Guid).GetMethod("Equals", new Type[] { typeof(Guid) });
                var body = Expression.Call(property, method, constant);
                return Expression.Lambda<Func<TEntity, bool>>(body, item);
            }
            if (property.Type == typeof(string))
            {
                var constant = Expression.Constant(filter.Value);
                var method = typeof(string).GetMethod("Equals", new Type[] { typeof(string) });
                var body = Expression.Call(property, method, constant);
                return Expression.Lambda<Func<TEntity, bool>>(body, item);
            }

            throw new Exception($"Object Filter: {filter.PropertyName} = {filter.Value} cannot be processed. ");
        }        
    }
}