using System.Collections.Generic;

namespace SqlEngine.Ast
{
  public class SelectItem : SqlAstNode
  {
    public SqlExpression Expression { get; set; } = null!;
    public string? Alias { get; set; }

    public override string ToString() => string.IsNullOrEmpty(Alias) ? Expression.ToString() : $"{Expression} AS {Alias}";
  }

  public class SelectClause : SqlAstNode
  {
    public bool IsDistinct { get; set; }
    public SqlExpression? TopExpression { get; set; }
    public bool TopPercent { get; set; }
    public List<SelectItem> Items { get; set; } = new List<SelectItem>();
  }

  public abstract class TableSource : SqlAstNode
  {
    public string? Alias { get; set; }
  }

  public class NamedTableSource : TableSource
  {
    public string? Server { get; set; }
    public string? Database { get; set; }
    public string? Schema { get; set; }
    public string TableName { get; set; } = string.Empty;

    public override string ToString()
    {
      var parts = new List<string>();
      if (!string.IsNullOrEmpty(Server)) parts.Add(Server!);
      if (!string.IsNullOrEmpty(Database)) parts.Add(Database!);
      if (!string.IsNullOrEmpty(Schema)) parts.Add(Schema!);
      parts.Add(TableName);
      string name = string.Join(".", parts);
      return string.IsNullOrEmpty(Alias) ? name : $"{name} AS {Alias}";
    }
  }

  public class DerivedTableSource : TableSource
  {
    public SelectStatement Query { get; set; } = null!;
  }

  public class TableValuedFunctionSource : TableSource
  {
    public FunctionCallExpression FunctionCall { get; set; } = null!;
  }

  public class JoinClause : SqlAstNode
  {
    public JoinType JoinType { get; set; }
    public TableSource Source { get; set; } = null!;
    public SqlExpression? Condition { get; set; } // ON condition (null for CROSS / APPLY)
  }

  public class FromClause : SqlAstNode
  {
    public TableSource? PrimarySource { get; set; }
    public List<JoinClause> Joins { get; set; } = new List<JoinClause>();
  }

  public class WhereClause : SqlAstNode
  {
    public SqlExpression Condition { get; set; } = null!;
  }

  public class GroupByClause : SqlAstNode
  {
    public List<SqlExpression> GroupingExpressions { get; set; } = new List<SqlExpression>();
  }

  public class HavingClause : SqlAstNode
  {
    public SqlExpression Condition { get; set; } = null!;
  }

  public class OrderByElement : SqlAstNode
  {
    public SqlExpression Expression { get; set; } = null!;
    public OrderDirection Direction { get; set; } = OrderDirection.Ascending;
  }

  public class OrderByClause : SqlAstNode
  {
    public List<OrderByElement> Elements { get; set; } = new List<OrderByElement>();
  }

  public class CteDefinition : SqlAstNode
  {
    public string Name { get; set; } = string.Empty;
    public List<string> ColumnNames { get; set; } = new List<string>();
    public SelectStatement Query { get; set; } = null!;
  }

  public class SetAssignment : SqlAstNode
  {
    public ColumnReferenceExpression Column { get; set; } = null!;
    public SqlExpression Value { get; set; } = null!;
  }
}
