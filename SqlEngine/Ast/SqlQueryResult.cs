using System.Collections.Generic;

namespace SqlEngine.Ast
{
  public class SqlParseError
  {
    public string Message { get; }
    public int Line { get; }
    public int Column { get; }
    public int Offset { get; }

    public SqlParseError(string message, int line, int column, int offset)
    {
      Message = message;
      Line = line;
      Column = column;
      Offset = offset;
    }

    public override string ToString() => $"({Line},{Column}): {Message}";
  }

  public class SqlQueryResult
  {
    public List<SqlStatement> Statements { get; set; } = new List<SqlStatement>();
    public List<SqlParseError> Errors { get; set; } = new List<SqlParseError>();
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Gets the primary statement if there is at least one, or null.
    /// </summary>
    public SqlStatement? FirstStatement => Statements.Count > 0 ? Statements[0] : null;
  }
}
