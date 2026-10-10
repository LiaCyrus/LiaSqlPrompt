using System;
using System.Collections.Generic;

namespace SqlEngine.Completion
{
  /// <summary>
  /// Provides contextual SQL keyword suggestions using KeywordsLoader as the single source of truth.
  /// </summary>
  public static class SqlCompletionKeywordProvider
  {
    public static IEnumerable<string> GetKeywords(SqlCompletionContext context)
    {
      if (context == null)
        return Array.Empty<string>();

      var keywords = KeywordsLoader.Current.GetKeywordsForKind(context.Kind);
      return Filter(keywords, context.Prefix);
    }

    private static IEnumerable<string> Filter(IEnumerable<string> items, string prefix)
    {
      if (string.IsNullOrEmpty(prefix))
      {
        return items;
      }

      var list = new List<string>();
      foreach (var item in items)
      {
        if (item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
          list.Add(item);
        }
      }
      return list;
    }
  }
}
