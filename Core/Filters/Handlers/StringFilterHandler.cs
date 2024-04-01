using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Core.Filters.Handlers
{
    public class StringFilterHandler : FilterHandlerBase
    {
        private readonly StringFilter _filter;

        public StringFilterHandler(StringFilter filter)
        {
            _filter = filter;
        }

        public override Expression<Func<TEntity, bool>> Handle<TEntity>()
        {
            var item = Expression.Parameter(typeof(TEntity), "item");
            var property = Expression.Call(GetProperty(item, _filter.PropertyName), "ToUpper", null);
            var value = Expression.Constant(_filter.Value.ToUpperInvariant());

            // We have made the decision that BBWT3 will, by default, convert all searches to be case-insensitive.
            // We are not currently supporting the option for case-sensitive searches, but that feature could be added at a later point.
            string methodName;
            switch (_filter.MatchMode)
            {
                case StringFilterMatchMode.Contains:
                    var isNotNullExpression = Expression.NotEqual(property, Expression.Constant(null));
                    var checkContainsExpression = Expression.Call(property, "Contains", null, value);
                    var notNullAndContainsExpression = Expression.AndAlso(isNotNullExpression, checkContainsExpression);
                    return Expression.Lambda<Func<TEntity, bool>>(notNullAndContainsExpression, item);

                case StringFilterMatchMode.StartsWith:
                    methodName = "StartsWith";
                    break;
                case StringFilterMatchMode.EndsWith:
                    methodName = "EndsWith";
                    break;
                default:
                    methodName = "Equals";
                    break;
            }
            var method = typeof(string).GetMethod(methodName, new Type[] { typeof(string) });
            var body = Expression.Call(property, method, value);
            return Expression.Lambda<Func<TEntity, bool>>(body, item);
        }

    }
}