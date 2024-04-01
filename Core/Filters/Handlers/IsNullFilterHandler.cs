using System;
using System.Linq.Expressions;

namespace Core.Filters.Handlers
{
    internal class IsNullFilterHandler : FilterHandlerBase
    {
        private readonly IsNullFilter filter;

        public IsNullFilterHandler(IsNullFilter filter)
        {
            this.filter = filter;
        }

        public override Expression<Func<TEntity, bool>> Handle<TEntity>()
        {
            var item = Expression.Parameter(typeof(TEntity), "item");
            Expression property = GetProperty(item, filter.PropertyName);
            Expression body = null;
            if (property.Type == typeof(string))
            {
                var constant = Expression.Constant(null);
                var method = property.Type.GetMethod("Equals", new Type[] { typeof(string) });
                body = Expression.Call(property, method, constant);
            }

            if (property.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                body = Expression.Not(Expression.Property(property, "HasValue"));
            }

            if (!property.Type.IsValueType)
            {
                var constant = Expression.Constant(null);
                var method = property.Type.GetMethod("Equals", new Type[] { typeof(object) });
                body = Expression.Call(property, method, constant);
            }

            if (body == null)
            {
                throw new Exception($"IsNull Filter: {filter.PropertyName} should be DTO, string or Nullable<>. ");
            }

            return Expression.Lambda<Func<TEntity, bool>>(body, item);
        }
    }
}
