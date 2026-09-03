namespace LiaSqlPrompt.Snippet
{
  public sealed class SnippetDefinition
  {
    public string Title { get; }
    public string Shortcut { get; }
    public string Description { get; }
    public string Code { get; }

    public SnippetDefinition(string title, string shortcut, string description, string code)
    {
      Title = title;
      Shortcut = shortcut;
      Description = description;
      Code = code;
    }
  }
}