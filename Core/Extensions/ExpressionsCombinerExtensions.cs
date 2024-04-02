using System;
using System.Linq;
using System.Linq.Expressions;

namespace Core.Extensions
{
    public static class ExpressionsCombinerExtensions
    {
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> exp, Expression<Func<T, bool>> newExp)
        {
            return Combine(exp, newExp, Expression.Or);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> exp, Expression<Func<T, bool>> newExp)
        {
            return Combine(exp, newExp, Expression.And);
        }

        private static Expression<Func<T, bool>> Combine<T>(Expression<Func<T, bool>> exp,
            Expression<Func<T, bool>> newExp, Func<Expression, Expression, BinaryExpression> combiner)
        {
            // get the visitor
            var visitor = new ParameterUpdateVisitor(newExp.Parameters.First(), exp.Parameters.First());
            // replace the parameter in the expression just created
            newExp = visitor.Visit(newExp) as Expression<Func<T, bool>>;

            // now you can and together the two expressions
            var binExp = combiner(exp.Body, newExp.Body);
            // and return a new lambda, that will do what you want. NOTE that the binExp has reference only to te newExp.Parameters[0] (there is only 1) parameter, and no other
            return Expression.Lambda<Func<T, bool>>(binExp, newExp.Parameters);
        }

        class ParameterUpdateVisitor : ExpressionVisitor
        {
            private ParameterExpression _oldParameter;
            private ParameterExpression _newParameter;

            public ParameterUpdateVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return ReferenceEquals(node, _oldParameter) ? _newParameter : base.VisitParameter(node);
            }
        }
    }
}