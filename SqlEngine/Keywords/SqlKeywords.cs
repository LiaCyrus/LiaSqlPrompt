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
      var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      // 1. Add SQL Server ScriptDom keywords
      try
      {
        var enumType = typeof(Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType);
        foreach (var name in Enum.GetNames(enumType))
        {
          var tokenType = (Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType)Enum.Parse(enumType, name);
          if (tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Identifier ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.QuotedIdentifier ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.WhiteSpace ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.SingleLineComment ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.MultilineComment ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.EndOfFile ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Integer ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Numeric ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Real ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Money ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.AsciiStringLiteral ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.UnicodeStringLiteral ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Variable ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Dot ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Comma ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Semicolon ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Colon ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.LeftParenthesis ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.RightParenthesis ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.LeftCurly ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.RightCurly ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.EqualsSign ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Plus ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Minus ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Star ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Divide ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Percent ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.LessThan ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.GreaterThan ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Bang ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Tilde ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Ampersand ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.Circumflex ||
              tokenType == Microsoft.SqlServer.TransactSql.ScriptDom.TSqlTokenType.None)
          {
            continue;
          }

          set.Add(name);
        }
      }
      catch
      {
      }

      // 2. Add keywords and built-in functions from sql-keywords.json (e.g. COUNT, GETDATE, SUM, etc.)
      try
      {
        using (var stream = GetDefaultKeywordsJsonStream())
        {
          if (stream != null)
          {
            var config = KeywordsLoader.LoadFromStream(stream);
            foreach (var item in config.GetAllItems())
            {
              set.Add(item);
            }
          }
        }
      }
      catch
      {
      }

      var list = new List<string>(set);
      list.Sort(StringComparer.OrdinalIgnoreCase);
      return list.ToArray();
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

    public static void SetKeywords(IEnumerable<string>? customKeywords)
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
