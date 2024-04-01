﻿using System;
using System.Linq.Expressions;

namespace Core.Filters.Handlers
{
    public class CountableBetweenFilterHandler<T> : FilterHandlerBase
    {
        private readonly CountableBetweenFilterBase<T> _filter;

        public CountableBetweenFilterHandler(CountableBetweenFilterBase<T> filter)
        {
            _filter = filter;
        }

        public override Expression<Func<TEntity, bool>> Handle<TEntity>()
        {
            var item = Expression.Parameter(typeof(TEntity), "item");
            var property = Expression.Property(item, _filter.PropertyName);

            var fromValue = Expression.Constant(_filter.From);
            var toValue = Expression.Constant(_filter.To);

            var fromExpr = Expression.GreaterThanOrEqual(property, fromValue);
            var toExpr = Expression.LessThanOrEqual(property, toValue);

            var body = Expression.And(fromExpr, toExpr);

            return Expression.Lambda<Func<TEntity, bool>>(body, item);
        }
    }
}