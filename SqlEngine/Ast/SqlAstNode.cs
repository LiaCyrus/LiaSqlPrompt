namespace SqlEngine.Ast
{
  public struct SourceSpan
  {
    public int StartOffset { get; }
    public int Length { get; }
    public int StartLine { get; }
    public int StartColumn { get; }

    public int EndOffset => StartOffset + Length;

    public SourceSpan(int startOffset, int length, int startLine = 0, int startColumn = 0)
    {
      StartOffset = startOffset;
      Length = length;
      StartLine = startLine;
      StartColumn = startColumn;
    }

    public override string ToString()
    {
      return $"[{StartOffset}..{EndOffset}] (Line {StartLine}, Col {StartColumn})";
    }
  }

  public abstract class SqlAstNode
  {
    public SourceSpan Span { get; set; }
  }
}
