using System;
using System.Linq.Expressions;

namespace Core.Filters.Handlers
{
    public class DateFilterHandler : CountableFilterHandler<DateTime>
    {
        private readonly DateFilter _filter;


        public DateFilterHandler(DateFilter filter) : base(filter) => _filter = filter;


        protected override Expression GetValue(DateTime value, Type propertyType)
        {
            // Fix for filtering on DateTimeOffset fields. See https://docs.microsoft.com/en-us/dotnet/standard/datetime/converting-between-datetime-and-offset for more details.
            return Expression.Convert(Expression.Constant(DateTime.SpecifyKind(value, DateTimeKind.Utc)), propertyType);
        }

        public override Expression<Func<TEntity, bool>> Handle<TEntity>()
        {
            /*
             * By default, an "Equals" match mode means that the value matches accurate to a day,
             * and the time value may seem redundant. But this does not work correctly given time zone offsets.
             * Therefore, we have to present the "day" as the time interval between specified value and plus 24 hours,
             * but not just cut the time part.
             */
            if (_filter.MatchMode == CountableFilterMatchMode.Equals)
            {
                var item = Expression.Parameter(typeof(TEntity), "item");
                var property = GetProperty(item, _filter.PropertyName);

                var fromValue = Expression.Convert(Expression.Constant(_filter.Value.ToUniversalTime()), property.Type);
                var toValue = Expression.Convert(Expression.Constant(_filter.Value.AddHours(24).ToUniversalTime()), property.Type);

                var fromExpr = Expression.GreaterThanOrEqual(property, fromValue);
                var toExpr = Expression.LessThan(property, toValue);

                var body = Expression.And(fromExpr, toExpr);

                return Expression.Lambda<Func<TEntity, bool>>(body, item);
            }

            return base.Handle<TEntity>();
        }
    }
}
