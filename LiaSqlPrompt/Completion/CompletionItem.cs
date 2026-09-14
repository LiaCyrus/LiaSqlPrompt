namespace LiaSqlPrompt.Completion
{
  public sealed class CompletionItem
  {
    public string Label { get; }
    public string InsertText { get; }
    public string Description { get; }

    public CompletionItem(string label, string insertText, string description)
    {
      Label = label;
      InsertText = insertText;
      Description = description;
    }
  }
}