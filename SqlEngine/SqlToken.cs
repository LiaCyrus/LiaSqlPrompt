namespace SqlEngine
{
  public enum SqlTokenType
  {
    Whitespace,
    Keyword,
    Identifier,
    StringLiteral,
    NumberLiteral,
    Comment,
    Symbol,
    Unknown
  }

  public sealed class SqlToken
  {
    public SqlTokenType Type { get; }
    public string Text { get; }
    public int StartIndex { get; }
    public int Length => Text.Length;

    public SqlToken(SqlTokenType type, string text, int startIndex)
    {
      Type = type;
      Text = text;
      StartIndex = startIndex;
    }

    public override string ToString()
    {
      return $"{Type}: \"{Text}\" at {StartIndex}";
    }
  }
}
