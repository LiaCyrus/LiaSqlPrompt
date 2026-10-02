using LiaSqlPrompt.Option;
using System;
using System.Collections.Generic;
using System.IO;

namespace LiaSqlPrompt.Snippet
{
  public sealed class SnippetRepository
  {
    private readonly List<SnippetDefinition> _snippets;

    public static SnippetRepository Instance { get; } = new SnippetRepository();

    private SnippetRepository()
    {
      _snippets = new List<SnippetDefinition>();

      Load();
    }

    public void Reload()
    {
      _snippets.Clear();

      Load();
    }

    private void Load()
    {
      string folder = OptionService.Snippet.SnippetFolder;

      if (!Directory.Exists(folder))
        return;

      var loader = new SnippetLoader();

      foreach (var file in Directory.GetFiles(folder, "*.snippet.xml"))
      {
        try
        {
          var snippet = loader.Load(file);

          _snippets.Add(snippet);
        }
        catch (Exception ex)
        {
          Logger.Log($"Failed loading {file}: {ex.Message}");
        }
      }
    }

    public IEnumerable<SnippetDefinition> GetAll()
    {
      return _snippets;
    }
  }
}