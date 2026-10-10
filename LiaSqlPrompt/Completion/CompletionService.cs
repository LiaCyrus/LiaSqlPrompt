using LiaSqlPrompt.Snippet;
using SqlEngine;
using SqlEngine.Completion;
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

    public List<CompletionItem> Search(SqlCompletionContext context)
    {
      var items = new List<CompletionItem>();
      if (context == null)
        return items;

      string prefix = context.Prefix ?? string.Empty;

      // 1. Snippet suggestions (only when typing a non-empty prefix and not in column-reference context)
      if (!string.IsNullOrWhiteSpace(prefix) && context.Kind != SqlEngine.Completion.SqlCompletionContextKind.ColumnReference)
      {
        foreach (var snippet in _snippetService.Search(prefix))
        {
          items.Add(new CompletionItem(snippet.Shortcut, snippet.Code, snippet.Description));
        }
      }

      // 2. Contextual SQL Keyword suggestions
      bool capitalize = Option.OptionService.General?.CapitalizeKeywords ?? true;
      var contextualKeywords = SqlEngine.Completion.SqlCompletionKeywordProvider.GetKeywords(context);

      foreach (var keyword in contextualKeywords)
      {
        string formattedKeyword = capitalize ? keyword.ToUpperInvariant() : keyword.ToLowerInvariant();
        items.Add(new CompletionItem(formattedKeyword, formattedKeyword, "SQL Keyword"));
      }

      // Sort with exact prefix matches or snippet priority, case-insensitively
      return items.OrderBy(i => i.Label, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public List<CompletionItem> Search(string prefix)
    {
      if (string.IsNullOrWhiteSpace(prefix))
        return new List<CompletionItem>();

      return Search(new SqlCompletionContext
      {
        Kind = SqlCompletionContextKind.Unknown,
        Prefix = prefix
      });
    }
  }
}