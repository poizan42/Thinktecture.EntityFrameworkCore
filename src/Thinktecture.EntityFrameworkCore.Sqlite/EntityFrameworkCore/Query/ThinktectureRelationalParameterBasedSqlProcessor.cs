using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="RelationalParameterBasedSqlProcessor"/>.
/// </summary>
public class ThinktectureSqliteParameterBasedSqlProcessor : RelationalParameterBasedSqlProcessor
{
   private readonly RelationalOptimizingVisitor _relationalOptimizingVisitor;

   /// <inheritdoc />
   public ThinktectureSqliteParameterBasedSqlProcessor(
      RelationalOptimizingVisitor relationalOptimizingVisitor,
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      bool useRelationalNulls)
      : base(dependencies, useRelationalNulls)
   {
      _relationalOptimizingVisitor = relationalOptimizingVisitor;
   }

   /// <inheritdoc />
   protected override Expression ProcessSqlNullability(Expression queryExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      ArgumentNullException.ThrowIfNull(queryExpression);
      ArgumentNullException.ThrowIfNull(parametersValues);

      return new ThinktectureSqlNullabilityProcessor(Dependencies, UseRelationalNulls).Process(queryExpression, parametersValues, out canCache);
   }

   /// <inheritdoc />
   public override Expression Optimize(Expression queryExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      queryExpression = base.Optimize(queryExpression, parametersValues, out canCache);

      return _relationalOptimizingVisitor.Process(queryExpression);
   }
}
