using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="RelationalQueryableMethodTranslatingExpressionVisitor"/>.
   /// </summary>
   public static class RelationalQueryableMethodTranslatingExpressionVisitorExtensions
   {
      /// <summary>
      /// Translates custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
      /// </summary>
      /// <param name="visitor">The visitor.</param>
      /// <param name="methodCallExpression">Method call to translate.</param>
      /// <returns>Translated method call if a custom method is found; otherwise <c>null</c>.</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="visitor"/> or <paramref name="methodCallExpression"/> is <c>null</c>.
      /// </exception>
      public static ShapedQueryExpression? TranslateRelationalMethods(this RelationalQueryableMethodTranslatingExpressionVisitor visitor, MethodCallExpression methodCallExpression)
      {
         if (visitor == null)
            throw new ArgumentNullException(nameof(visitor));
         if (methodCallExpression == null)
            throw new ArgumentNullException(nameof(methodCallExpression));

         if (methodCallExpression.Method.DeclaringType == typeof(RelationalQueryableExtensions)
             && methodCallExpression.Method.Name == nameof(RelationalQueryableExtensions.AsSubQuery))
         {
            var expression = visitor.Visit(methodCallExpression.Arguments[0]);

            if (expression is ShapedQueryExpression shapedQueryExpression)
            {
               ((SelectExpression)shapedQueryExpression.QueryExpression).PushdownIntoSubquery();
               return shapedQueryExpression;
            }
         }

         return null;
      }
   }
}
