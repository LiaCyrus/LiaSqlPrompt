using System.Collections.Generic;

namespace SqlEngine.Ast
{
  public abstract class SqlStatement : SqlAstNode
  {
    public abstract StatementType StatementType { get; }
  }

  public class SelectStatement : SqlStatement
  {
    public override StatementType StatementType => StatementType.Select;

    public List<CteDefinition> Ctes { get; set; } = new List<CteDefinition>();
    public SelectClause? Select { get; set; }
    public FromClause? From { get; set; }
    public WhereClause? Where { get; set; }
    public GroupByClause? GroupBy { get; set; }
    public HavingClause? Having { get; set; }
    public OrderByClause? OrderBy { get; set; }
  }

  public class InsertStatement : SqlStatement
  {
    public override StatementType StatementType => StatementType.Insert;

    public TableSource Target { get; set; } = null!;
    public List<ColumnReferenceExpression> Columns { get; set; } = new List<ColumnReferenceExpression>();
    public List<List<SqlExpression>> Values { get; set; } = new List<List<SqlExpression>>();
    public SelectStatement? SourceSelect { get; set; }
  }

  public class UpdateStatement : SqlStatement
  {
    public override StatementType StatementType => StatementType.Update;

    public TableSource Target { get; set; } = null!;
    public List<SetAssignment> SetAssignments { get; set; } = new List<SetAssignment>();
    public FromClause? From { get; set; }
    public WhereClause? Where { get; set; }
  }

  public class DeleteStatement : SqlStatement
  {
    public override StatementType StatementType => StatementType.Delete;

    public TableSource Target { get; set; } = null!;
    public FromClause? From { get; set; }
    public WhereClause? Where { get; set; }
  }

  public class MergeStatement : SqlStatement
  {
    public override StatementType StatementType => StatementType.Merge;

    public TableSource Target { get; set; } = null!;
    public TableSource Source { get; set; } = null!;
    public SqlExpression OnCondition { get; set; } = null!;
  }

  public class ExecuteStatement : SqlStatement
  {
    public override StatementType StatementType => StatementType.Execute;

    public string ProcedureName { get; set; } = string.Empty;
    public List<SqlExpression> Parameters { get; set; } = new List<SqlExpression>();
  }

  public class UnknownStatement : SqlStatement
  {
    public override StatementType StatementType => StatementType.Unknown;
    public string RawSql { get; set; } = string.Empty;
  }
}
