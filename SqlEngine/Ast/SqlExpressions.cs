using System.Collections.Generic;

namespace SqlEngine.Ast
{
  public abstract class SqlExpression : SqlAstNode
  {
  }

  /// <summary>
  /// Column reference representing Database.Schema.TableOrAlias.Column.
  /// </summary>
  public class ColumnReferenceExpression : SqlExpression
  {
    public string? Database { get; set; }
    public string? Schema { get; set; }
    public string? TableOrAlias { get; set; }
    public string ColumnName { get; set; } = string.Empty;

    public override string ToString()
    {
      var parts = new List<string>();
      if (!string.IsNullOrEmpty(Database)) parts.Add(Database!);
      if (!string.IsNullOrEmpty(Schema)) parts.Add(Schema!);
      if (!string.IsNullOrEmpty(TableOrAlias)) parts.Add(TableOrAlias!);
      parts.Add(ColumnName);
      return string.Join(".", parts);
    }
  }

  public class LiteralExpression : SqlExpression
  {
    public string Value { get; set; } = string.Empty;
    public bool IsString { get; set; }
    public bool IsNumeric { get; set; }
    public bool IsNull { get; set; }

    public override string ToString() => Value;
  }

  public class VariableExpression : SqlExpression
  {
    public string Name { get; set; } = string.Empty; // e.g. "@CustomerId"
    public override string ToString() => Name;
  }

  public class FunctionCallExpression : SqlExpression
  {
    public string Name { get; set; } = string.Empty;
    public bool IsDistinct { get; set; }
    public List<SqlExpression> Arguments { get; set; } = new List<SqlExpression>();

    public override string ToString()
    {
      string distinctStr = IsDistinct ? "DISTINCT " : "";
      return $"{Name}({distinctStr}{string.Join(", ", Arguments)})";
    }
  }

  public class BinaryExpression : SqlExpression
  {
    public SqlExpression Left { get; set; } = null!;
    public BinaryOperator Operator { get; set; }
    public SqlExpression Right { get; set; } = null!;

    public override string ToString() => $"({Left} {Operator} {Right})";
  }

  public class UnaryExpression : SqlExpression
  {
    public UnaryOperator Operator { get; set; }
    public SqlExpression Operand { get; set; } = null!;

    public override string ToString() => $"({Operator} {Operand})";
  }

  public class WhenClause : SqlAstNode
  {
    public SqlExpression Condition { get; set; } = null!;
    public SqlExpression Result { get; set; } = null!;
  }

  public class CaseExpression : SqlExpression
  {
    public SqlExpression? InputExpression { get; set; } // simple CASE when non-null
    public List<WhenClause> WhenClauses { get; set; } = new List<WhenClause>();
    public SqlExpression? ElseExpression { get; set; }
  }

  public class CastExpression : SqlExpression
  {
    public SqlExpression Expression { get; set; } = null!;
    public string TargetDataType { get; set; } = string.Empty;
    public bool IsTryCast { get; set; }
  }

  public class ConvertExpression : SqlExpression
  {
    public string TargetDataType { get; set; } = string.Empty;
    public SqlExpression Expression { get; set; } = null!;
    public SqlExpression? Style { get; set; }
    public bool IsTryConvert { get; set; }
  }

  public class SubqueryExpression : SqlExpression
  {
    public SelectStatement Subquery { get; set; } = null!;
    public override string ToString() => "(Subquery)";
  }

  public class InExpression : SqlExpression
  {
    public SqlExpression Expression { get; set; } = null!;
    public bool IsNotIn { get; set; }
    public List<SqlExpression> Values { get; set; } = new List<SqlExpression>();
    public SubqueryExpression? Subquery { get; set; }
  }

  public class BetweenExpression : SqlExpression
  {
    public SqlExpression Expression { get; set; } = null!;
    public bool IsNotBetween { get; set; }
    public SqlExpression Lower { get; set; } = null!;
    public SqlExpression Upper { get; set; } = null!;
  }

  public class LikeExpression : SqlExpression
  {
    public SqlExpression Expression { get; set; } = null!;
    public bool IsNotLike { get; set; }
    public SqlExpression Pattern { get; set; } = null!;
    public SqlExpression? Escape { get; set; }
  }

  public class ExistsExpression : SqlExpression
  {
    public bool IsNotExists { get; set; }
    public SubqueryExpression Subquery { get; set; } = null!;
  }

  public class IsNullExpression : SqlExpression
  {
    public SqlExpression Expression { get; set; } = null!;
    public bool IsNotNull { get; set; }
  }

  public class StarExpression : SqlExpression
  {
    public string? TableOrAlias { get; set; } // e.g. "c.*" or null for "*"
    public override string ToString() => string.IsNullOrEmpty(TableOrAlias) ? "*" : $"{TableOrAlias}.*";
  }

  public class RawTextExpression : SqlExpression
  {
    public string Text { get; set; } = string.Empty;
    public override string ToString() => Text;
  }
}
