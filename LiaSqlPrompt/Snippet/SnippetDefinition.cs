namespace LiaSqlPrompt.Snippet
{
  public sealed class SnippetDefinition
  {
    public string Title { get; }
    public string Shortcut { get; }
    public string Description { get; }
    public string Code { get; }
    public string? FilePath { get; }

    public SnippetDefinition(string title, string shortcut, string description, string code, string? filePath = null)
    {
      Title = title;
      Shortcut = shortcut;
      Description = description;
      Code = code;
      FilePath = filePath;
    }
  }
}