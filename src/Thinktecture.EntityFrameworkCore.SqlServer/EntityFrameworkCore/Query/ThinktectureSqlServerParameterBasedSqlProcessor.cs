using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqlServerParameterBasedSqlProcessor : SqlServerParameterBasedSqlProcessor
{
   private readonly RelationalOptimizingVisitor _relationalOptimizingVisitor;

   /// <inheritdoc />
   public ThinktectureSqlServerParameterBasedSqlProcessor(
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

      return new ThinktectureSqlServerSqlNullabilityProcessor(Dependencies, UseRelationalNulls).Process(queryExpression, parametersValues, out canCache);
   }

   /// <inheritdoc />
   public override Expression Optimize(Expression queryExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      queryExpression = base.Optimize(queryExpression, parametersValues, out canCache);

      return _relationalOptimizingVisitor.Process(queryExpression);
   }
}
