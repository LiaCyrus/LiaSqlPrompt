namespace SqlEngine.Parsing
{
  public enum SqlParserTokenType
  {
    Whitespace,
    Keyword,
    Identifier,
    StringLiteral,
    NumberLiteral,
    Comment,
    Symbol,
    Dot,
    LeftParenthesis,
    RightParenthesis,
    EndOfFile,
    Other
  }

  /// <summary>
  /// Represents a token produced by the SQL parser token stream.
  /// Decouples external consumers from Microsoft.SqlServer.TransactSql.ScriptDom types.
  /// </summary>
  public sealed class SqlParsedToken
  {
    public SqlParserTokenType TokenType { get; }
    public string Text { get; }
    public int Offset { get; }
    public int Length => Text.Length;
    public bool IsKeyword { get; }

    public SqlParsedToken(SqlParserTokenType tokenType, string text, int offset, bool isKeyword)
    {
      TokenType = tokenType;
      Text = text;
      Offset = offset;
      IsKeyword = isKeyword;
    }

    public override string ToString()
    {
      return $"{TokenType} (Keyword={IsKeyword}): '{Text}' at {Offset}";
    }
  }
}
