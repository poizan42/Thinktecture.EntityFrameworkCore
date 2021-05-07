using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.Internal;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="IQueryable{T}"/>.
   /// </summary>
   public static class RelationalQueryableExtensions
   {
      private static readonly MethodInfo _asSubQuery = typeof(RelationalQueryableExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                            .Single(m => m.Name == nameof(AsSubQuery) && m.IsGenericMethod);

      private static readonly MethodInfo _withTableHints = typeof(RelationalQueryableExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                                .Single(m => m.Name == nameof(WithTableHints)
                                                                                                             && m.IsGenericMethod
                                                                                                             && m.GetParameters()[1].ParameterType == typeof(IReadOnlyList<ITableHint>));

      /// <summary>
      /// Adds table hints to a table specified in <paramref name="source"/>.
      /// </summary>
      /// <param name="source">Query using a table to apply table hints to.</param>
      /// <param name="hints">Table hints.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <returns>Query with table hints applied.</returns>
      public static IQueryable<T> WithTableHints<T>(this IQueryable<T> source, params ITableHint[] hints)
      {
         return source.WithTableHints((IReadOnlyList<ITableHint>)hints);
      }

      /// <summary>
      /// Adds table hints to a table specified in <paramref name="source"/>.
      /// </summary>
      /// <param name="source">Query using a table to apply table hints to.</param>
      /// <param name="hints">Table hints.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <returns>Query with table hints applied.</returns>
      public static IQueryable<T> WithTableHints<T>(this IQueryable<T> source, IReadOnlyList<ITableHint> hints)
      {
         if (source == null)
            throw new ArgumentNullException(nameof(source));
         if (hints == null)
            throw new ArgumentNullException(nameof(hints));

         var methodInfo = _withTableHints.MakeGenericMethod(typeof(T));
         var expression = Expression.Call(null, methodInfo, source.Expression, new NonEvaluatableConstantExpression(hints));
         return source.Provider.CreateQuery<T>(expression);
      }

      /// <summary>
      /// Performs a LEFT JOIN.
      /// </summary>
      /// <param name="left">Left side query.</param>
      /// <param name="right">Right side query.</param>
      /// <param name="leftKeySelector">JOIN key selector for the entity on the left.</param>
      /// <param name="rightKeySelector">JOIN key selector for the entity on the right.</param>
      /// <param name="resultSelector">
      /// Result selector.
      /// Please note that the <see cref="LeftJoinResult{TLeft,TRight}.Right"/> entity can be <c>null</c> when projecting non-nullable structs (int, bool, etc.).
      /// </param>
      /// <typeparam name="TLeft">Type of the entity on the left side.</typeparam>
      /// <typeparam name="TRight">Type of the entity on the right side.</typeparam>
      /// <typeparam name="TKey">Type of the JOIN key.</typeparam>
      /// <typeparam name="TResult">Type of the result.</typeparam>
      /// <returns>An <see cref="IQueryable{T}"/> with item type <typeparamref name="TResult"/>.</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="left"/> is <c>null</c>
      /// - or <paramref name="right"/> is <c>null</c>
      /// - or <paramref name="leftKeySelector"/> is <c>null</c>
      /// - or <paramref name="rightKeySelector"/> is <c>null</c>
      /// - or <paramref name="resultSelector"/> is <c>null</c>.
      /// </exception>
      public static IQueryable<TResult> LeftJoin<TLeft, TRight, TKey, TResult>(
         this IQueryable<TLeft> left,
         IEnumerable<TRight> right,
         Expression<Func<TLeft, TKey>> leftKeySelector,
         Expression<Func<TRight, TKey>> rightKeySelector,
         Expression<Func<LeftJoinResult<TLeft, TRight>, TResult>> resultSelector)
         where TLeft : notnull
         where TRight : notnull
      {
         if (left == null)
            throw new ArgumentNullException(nameof(left));
         if (right == null)
            throw new ArgumentNullException(nameof(right));
         if (leftKeySelector == null)
            throw new ArgumentNullException(nameof(leftKeySelector));
         if (rightKeySelector == null)
            throw new ArgumentNullException(nameof(rightKeySelector));
         if (resultSelector == null)
            throw new ArgumentNullException(nameof(resultSelector));

         return left
                .GroupJoin(right, leftKeySelector, rightKeySelector, (o, i) => new { Outer = o, Inner = i })
                .SelectMany(g => g.Inner.DefaultIfEmpty(), (o, i) => new LeftJoinResult<TLeft, TRight> { Left = o.Outer, Right = i })
                .Select(resultSelector);
      }

      /// <summary>
      /// Executes provided query as a sub query.
      /// </summary>
      /// <param name="source">Query to execute as as sub query.</param>
      /// <typeparam name="TEntity">Type of the entity.</typeparam>
      /// <returns>Query that will be executed as a sub query.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
      public static IQueryable<TEntity> AsSubQuery<TEntity>(this IQueryable<TEntity> source)
      {
         if (source == null)
            throw new ArgumentNullException(nameof(source));

         return source.Provider.CreateQuery<TEntity>(Expression.Call(null, _asSubQuery.MakeGenericMethod(typeof(TEntity)), source.Expression));
      }
   }
}
