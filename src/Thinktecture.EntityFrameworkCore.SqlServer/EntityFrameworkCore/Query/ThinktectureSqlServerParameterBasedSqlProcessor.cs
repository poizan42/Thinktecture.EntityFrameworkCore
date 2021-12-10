using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqlServerParameterBasedSqlProcessor : SqlServerParameterBasedSqlProcessor
{
   /// <inheritdoc />
   public ThinktectureSqlServerParameterBasedSqlProcessor(RelationalParameterBasedSqlProcessorDependencies dependencies, bool useRelationalNulls)
      : base(dependencies, useRelationalNulls)
   {
   }

   /// <inheritdoc />
   protected override SelectExpression ProcessSqlNullability(SelectExpression selectExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      ArgumentNullException.ThrowIfNull(selectExpression);
      ArgumentNullException.ThrowIfNull(parametersValues);

      return new ThinktectureSqlNullabilityProcessor(Dependencies, UseRelationalNulls).Process(selectExpression, parametersValues, out canCache);
   }

   /// <inheritdoc />
   public override SelectExpression Optimize(SelectExpression selectExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      selectExpression = base.Optimize(selectExpression, parametersValues, out canCache);

      var hasTempTables = TempTableQueryContext.TryGetTempTableContexts(parametersValues, out var tempTableCtxs);
      var hasTableHints = TableHintContext.TryGetTableHintContext(parametersValues, out var tableHintCtxs);

      if (hasTempTables || hasTableHints)
      {
         selectExpression = new BulkOperationOptimizingVisitor(parametersValues,
                                                               tempTableCtxs ?? Array.Empty<TempTableQueryContext>(),
                                                               tableHintCtxs ?? Array.Empty<TableHintContext>()).Process(selectExpression);
      }

      return selectExpression;
   }
}
