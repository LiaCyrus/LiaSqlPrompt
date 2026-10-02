using LiaSqlPrompt.Snippet;
using SqlEngine;
using System;
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

      if (string.IsNullOrWhiteSpace(prefix))
        return items;

      // 1. Snippet suggestions
      foreach (var snippet in _snippetService.Search(prefix))
      {
        items.Add(new CompletionItem(snippet.Shortcut, snippet.Code, snippet.Description));
      }

      // 2. SQL Keyword suggestions from SqlEngine
      bool capitalize = Option.OptionService.General?.CapitalizeKeywords ?? true;

      foreach (var keyword in SqlKeywords.All)
      {
        if (keyword.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
          string formattedKeyword = capitalize ? keyword.ToUpperInvariant() : keyword.ToLowerInvariant();
          items.Add(new CompletionItem(formattedKeyword, formattedKeyword, "SQL Keyword"));
        }
      }

      // Sort with exact prefix matches or snippet priority, case-insensitively
      return items.OrderBy(i => i.Label, StringComparer.OrdinalIgnoreCase).ToList();
    }
  }
}