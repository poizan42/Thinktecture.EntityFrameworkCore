using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="SqlNullabilityProcessor"/>.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqlServerSqlNullabilityProcessor : SqlNullabilityProcessor
{
   /// <inheritdoc />
   public ThinktectureSqlServerSqlNullabilityProcessor(RelationalParameterBasedSqlProcessorDependencies dependencies, bool useRelationalNulls)
      : base(dependencies, useRelationalNulls)
   {
   }

   /// <inheritdoc />
   protected override TableExpressionBase Visit(TableExpressionBase tableExpressionBase)
   {
      if (tableExpressionBase is INotNullableSqlExpression)
         return tableExpressionBase;

      return base.Visit(tableExpressionBase);
   }

   /// <inheritdoc />
   protected override SqlExpression VisitCustomSqlExpression(SqlExpression sqlExpression, bool allowOptimizedExpansion, out bool nullable)
   {
      if (sqlExpression is INotNullableSqlExpression)
      {
         nullable = false;
         return sqlExpression;
      }

      return base.VisitCustomSqlExpression(sqlExpression, allowOptimizedExpansion, out nullable);
   }
}
