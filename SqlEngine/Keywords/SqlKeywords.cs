using System;
using System.Collections.Generic;
using System.IO;

namespace SqlEngine
{
  public static class SqlKeywords
  {
    private static readonly HashSet<string> BuiltInKeywordsSet;
    private static readonly string[] DefaultKeywordsArray;
    private static readonly HashSet<string> ActiveKeywordsSet;
    private static string[] ActiveKeywordsArray;
    private static readonly object SyncLock = new object();

    static SqlKeywords()
    {
      DefaultKeywordsArray = LoadDefaultKeywordsFromJson();
      BuiltInKeywordsSet = new HashSet<string>(DefaultKeywordsArray, StringComparer.OrdinalIgnoreCase);
      ActiveKeywordsSet = new HashSet<string>(DefaultKeywordsArray, StringComparer.OrdinalIgnoreCase);
      ActiveKeywordsArray = (string[])DefaultKeywordsArray.Clone();
    }

    public static Stream GetDefaultKeywordsJsonStream()
    {
      var assembly = typeof(SqlKeywords).Assembly;
      return assembly.GetManifestResourceStream("SqlEngine.Keywords.sql-keywords.json")
          ?? assembly.GetManifestResourceStream("SqlEngine.sql-keywords.json");
    }

    public static string GetDefaultKeywordsJsonString()
    {
      using (var stream = GetDefaultKeywordsJsonStream())
      {
        if (stream == null)
          return string.Empty;

        using (var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
        {
          return reader.ReadToEnd();
        }
      }
    }

    private static string[] LoadDefaultKeywordsFromJson()
    {
      try
      {
        using (var stream = GetDefaultKeywordsJsonStream())
        {
          if (stream != null)
          {
            var config = KeywordsLoader.LoadFromStream(stream);
            var list = new List<string>(config.GetAllItems());
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list.ToArray();
          }
        }
      }
      catch
      {
      }

      return new string[0];
    }

    public static IReadOnlyCollection<string> All
    {
      get
      {
        lock (SyncLock)
        {
          return ActiveKeywordsArray;
        }
      }
    }

    public static IReadOnlyCollection<string> BuiltIn => DefaultKeywordsArray;

    public static bool IsKeyword(string word)
    {
      if (string.IsNullOrEmpty(word))
        return false;

      lock (SyncLock)
      {
        return ActiveKeywordsSet.Contains(word);
      }
    }

    public static void SetKeywords(IEnumerable<string> customKeywords)
    {
      lock (SyncLock)
      {
        ActiveKeywordsSet.Clear();

        // Always keep built-in keywords as fallback/foundation
        foreach (var kw in DefaultKeywordsArray)
        {
          ActiveKeywordsSet.Add(kw);
        }

        if (customKeywords != null)
        {
          foreach (var kw in customKeywords)
          {
            if (!string.IsNullOrWhiteSpace(kw))
            {
              ActiveKeywordsSet.Add(kw.Trim());
            }
          }
        }

        var list = new List<string>(ActiveKeywordsSet);
        list.Sort(StringComparer.OrdinalIgnoreCase);
        ActiveKeywordsArray = list.ToArray();
      }
    }

    public static void ResetToDefault()
    {
      SetKeywords(null);
    }
  }
}
