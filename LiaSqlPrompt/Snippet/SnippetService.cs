using System;
using System.Collections.Generic;

namespace LiaSqlPrompt.Snippet
{
  public sealed class SnippetService
  {
    private readonly SnippetRepository _repository;

    public SnippetService()
    {
      _repository = SnippetRepository.Instance;
    }

    public IEnumerable<SnippetDefinition> Search(string prefix)
    {
      foreach (var snippet in _repository.GetAll())
      {
        if (snippet.Shortcut.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
          yield return snippet;
        }
      }
    }
  }
}