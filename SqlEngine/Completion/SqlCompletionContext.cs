using SqlEngine.Ast;

namespace SqlEngine.Completion
{
  public sealed class SqlCompletionContext
  {
    public StatementType? StatementType { get; set; }

    public SqlCompletionContextKind Kind { get; set; }

    public string Prefix { get; set; } = string.Empty;

    public string? TableAlias { get; set; }

    public string? TableName { get; set; }

    public int CursorPosition { get; set; }
  }
}
