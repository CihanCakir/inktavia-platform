using System.Linq.Expressions;
using System.Reflection;
using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Common.Abstraction.Util;


namespace Aizen.Core.Common.Abstraction.Util
{
    public static class AizenGlobalFilterHelper
    {
        public static Expression GenerateExpression(
            ParameterExpression parameter,
            PropertyInfo propertyInfo,
            AizenGlobalFilterExpressionType expressionType,
            object value)
        {
            return expressionType switch
            {
                AizenGlobalFilterExpressionType.Equal => Expression.Equal(Expression.Property(parameter, propertyInfo),
                    Expression.Constant(value)),
                AizenGlobalFilterExpressionType.GreaterThan => Expression.GreaterThan(Expression.Property(parameter, propertyInfo),
                    Expression.Constant(value)),
                _ => throw new AizenException($"GlobalExpressionBuilder is invalid. ExpressionType: { nameof(expressionType) }")
            };
        }
    }
}
