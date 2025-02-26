using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqliteQuerySqlGenerator : SqliteQuerySqlGenerator
{
   /// <inheritdoc />
   public ThinktectureSqliteQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies)
      : base(dependencies)
   {
   }

   /// <inheritdoc />
   protected override Expression VisitExtension(Expression expression)
   {
      switch (expression)
      {
         case TempTableExpression tempTableExpression:
            VisitTempTable(tempTableExpression);
            return expression;
         default:
            return base.VisitExtension(expression);
      }
   }

   private void VisitTempTable(TempTableExpression tempTableExpression)
   {
      Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tempTableExpression.Name))
         .Append(AliasSeparator)
         .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tempTableExpression.Alias));
   }

   /// <inheritdoc />
   protected override Expression VisitSelect(SelectExpression selectExpression)
   {
      if (selectExpression.TryGetDeleteExpression(out var tableToDeleteIn))
         return GenerateDeleteStatement(tableToDeleteIn, selectExpression);

      return base.VisitSelect(selectExpression);
   }

   private Expression GenerateDeleteStatement(TableExpressionBase tableToDeleteIn, SelectExpression selectExpression)
   {
      if (selectExpression.IsDistinct)
         throw new NotSupportedException("A DISTINCT clause is not supported in a DELETE statement.");

      if (selectExpression.Tables.Count == 0)
         throw new NotSupportedException("A DELETE statement without any tables is invalid.");

      if (selectExpression.Tables.Count != 1)
         throw new NotSupportedException("SQLite supports only 1 outermost table in a DELETE statement.");

      if (selectExpression.GroupBy.Count > 0)
         throw new NotSupportedException("A GROUP BY clause is not supported in a DELETE statement.");

      if (selectExpression.Having is not null)
         throw new NotSupportedException("A HAVING clause is not supported in a DELETE statement.");

      if (selectExpression.Offset is not null)
         throw new NotSupportedException("An OFFSET clause (i.e. Skip(x)) is not supported in a DELETE statement.");

      if (selectExpression.Limit is not null)
         throw new NotSupportedException("A TOP/LIMIT clause (i.e. Take(x)) is not supported in a DELETE statement.");

      if (selectExpression.Tables.Count != 1)
         throw new NotSupportedException($"A DELETE statement must reference exactly 1 table. Provided table references: [{String.Join(", ", selectExpression.Tables.Select(t => t.Alias))}]");

      Sql.Append("DELETE FROM ");

      Visit(tableToDeleteIn);

      if (selectExpression.Predicate is not null)
      {
         Sql.AppendLine().Append("WHERE ");
         Visit(selectExpression.Predicate);
      }

      Sql.Append(Dependencies.SqlGenerationHelper.StatementTerminator).AppendLine()
         .Append("SELECT CHANGES()").Append(Dependencies.SqlGenerationHelper.StatementTerminator);

      return selectExpression;
   }
}
