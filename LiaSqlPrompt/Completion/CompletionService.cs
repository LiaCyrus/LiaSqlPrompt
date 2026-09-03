using LiaSqlPrompt.Snippet;
using System.Collections.Generic;
using System.Linq;

namespace LiaSqlPrompt.Completion
{
  public sealed class CompletionService
  {
    private readonly SnippetService _snippetService;

    public CompletionService()
    {
      _snippetService = new SnippetService();
    }

    public List<CompletionItem> Search(string prefix)
    {
      var items = new List<CompletionItem>();

      foreach (var snippet in _snippetService.Search(prefix))
      {
        items.Add(new CompletionItem(snippet.Shortcut, snippet.Code, snippet.Description));
      }

      return items;
    }
  }
}