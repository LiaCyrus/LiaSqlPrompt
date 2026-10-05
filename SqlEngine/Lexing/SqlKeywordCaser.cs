using System;
using System.Collections.Generic;
using SqlEngine.Parsing;

namespace SqlEngine
{
  /// <summary>
  /// Handles SQL keyword capitalization and formatting using the SQL parser / tokenizer.
  /// Accurately differentiates keywords from identifiers, table/column qualifiers (e.g. x.i),
  /// quoted identifiers, literals, comments, and clause positions (e.g. table name expected after FROM/JOIN).
  /// </summary>
  public static class SqlKeywordCaser
  {
    private static readonly HashSet<string> TableSourceKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "FROM", "JOIN", "INTO", "UPDATE", "TABLE"
    };

    /// <summary>
    /// Checks whether the token at or ending at offset is preceded by a dot '.' (ignoring whitespace),
    /// indicating member access such as table.column, schema.table, or alias.field (e.g. x.i).
    /// </summary>
    public static bool IsPrecededByDot(string sqlText, int offset)
    {
      if (string.IsNullOrEmpty(sqlText) || offset <= 0)
        return false;

      int scanOffset = Math.Min(offset, sqlText.Length);
      var tokens = SqlQueryParser.ParseTokens(sqlText);
      if (tokens.Count == 0)
        return false;

      // Find the token at or ending at scanOffset
      int tokenIndex = -1;
      for (int i = 0; i < tokens.Count; i++)
      {
        var t = tokens[i];
        if (t.Offset + t.Length == scanOffset)
        {
          tokenIndex = i;
          break;
        }
        if (t.Offset <= scanOffset && scanOffset <= t.Offset + t.Length)
        {
          tokenIndex = i;
        }
      }

      if (tokenIndex < 0)
        return false;

      for (int prev = tokenIndex - 1; prev >= 0; prev--)
      {
        var prevToken = tokens[prev];
        if (prevToken.TokenType == SqlParserTokenType.Whitespace ||
            prevToken.TokenType == SqlParserTokenType.Comment)
        {
          continue;
        }

        if (prevToken.TokenType == SqlParserTokenType.Dot)
          return true;

        break;
      }

      return false;
    }

    /// <summary>
    /// Checks whether the token at or ending at offset is in a position where a table name,
    /// target object, or alias is expected rather than a SQL statement keyword.
    /// For example: immediately following FROM, JOIN, INTO, UPDATE, TABLE
    /// without an intervening left parenthesis '(' indicating a subquery (e.g. "FROM (SELECT ...").
    /// </summary>
    public static bool IsInTableSourceContext(string sqlText, int offset)
    {
      if (string.IsNullOrEmpty(sqlText) || offset <= 0)
        return false;

      int scanOffset = Math.Min(offset, sqlText.Length);
      var tokens = SqlQueryParser.ParseTokens(sqlText);
      if (tokens.Count == 0)
        return false;

      int tokenIndex = -1;
      for (int i = 0; i < tokens.Count; i++)
      {
        var t = tokens[i];
        if (t.Offset + t.Length == scanOffset)
        {
          tokenIndex = i;
          break;
        }
        if (t.Offset <= scanOffset && scanOffset <= t.Offset + t.Length)
        {
          tokenIndex = i;
        }
      }

      if (tokenIndex < 0)
        return false;

      // Scan backwards skipping whitespace and comments
      for (int prev = tokenIndex - 1; prev >= 0; prev--)
      {
        var prevToken = tokens[prev];
        if (prevToken.TokenType == SqlParserTokenType.Whitespace ||
            prevToken.TokenType == SqlParserTokenType.Comment)
        {
          continue;
        }

        // If preceded by '(', it's a subquery like "FROM (SELECT", so SELECT is valid
        if (prevToken.TokenType == SqlParserTokenType.LeftParenthesis)
        {
          return false;
        }

        // If immediately preceded by FROM, JOIN, INTO, UPDATE, TABLE
        if (prevToken.IsKeyword && TableSourceKeywords.Contains(prevToken.Text))
        {
          return true;
        }

        break;
      }

      return false;
    }

    /// <summary>
    /// Checks whether the word immediately preceding caretOffset
    /// is a SQL keyword or function that should be formatted according to the given casing preference.
    ///
    /// It ensures that:
    /// 1. Only actual Keyword tokens are converted (excluding bracketed identifiers, strings, comments, variables, etc.).
    /// 2. If the keyword is immediately preceded by a dot '.' (e.g. table.select or schema.table.order), it is treated as a column or object name and NOT formatted.
    /// 3. If the keyword is immediately preceded by clause headers like FROM, JOIN, INTO, UPDATE, TABLE without '(', it is treated as a table/object name and NOT formatted.
    /// </summary>
    /// <param name="sqlText">The SQL text (for example the entire document or line/statement text).</param>
    /// <param name="caretOffset">The position where Space (or delimiter) was typed (or where the word ends).</param>
    /// <param name="casing">The target casing (Uppercase, Lowercase, or Disabled).</param>
    /// <param name="targetSpan">Outputs the start index and length of the keyword within sqlText if formatting is needed.</param>
    /// <param name="formattedWord">Outputs the formatted keyword text.</param>
    /// <returns>True if a keyword needs to be replaced, false otherwise.</returns>
    public static bool TryGetKeywordFormatReplacement(
      string sqlText,
      int caretOffset,
      KeywordCasing casing,
      out (int Start, int Length) targetSpan,
      out string formattedWord)
    {
      targetSpan = (0, 0);
      formattedWord = string.Empty;

      if (casing == KeywordCasing.Disabled || string.IsNullOrEmpty(sqlText) || caretOffset <= 0)
      {
        return false;
      }

      int scanOffset = Math.Min(caretOffset, sqlText.Length);
      var tokens = SqlQueryParser.ParseTokens(sqlText);
      if (tokens.Count == 0)
      {
        return false;
      }

      // Find token that ends exactly at or immediately before scanOffset
      int tokenIndex = -1;
      for (int i = 0; i < tokens.Count; i++)
      {
        var t = tokens[i];
        if (t.Offset + t.Length == scanOffset)
        {
          tokenIndex = i;
          break;
        }
      }

      if (tokenIndex < 0)
      {
        return false;
      }

      var token = tokens[tokenIndex];

      // Must be a recognized Keyword token
      if (!token.IsKeyword)
      {
        return false;
      }

      // Check preceding non-trivia token
      for (int prev = tokenIndex - 1; prev >= 0; prev--)
      {
        var prevToken = tokens[prev];
        if (prevToken.TokenType == SqlParserTokenType.Whitespace ||
            prevToken.TokenType == SqlParserTokenType.Comment)
        {
          continue;
        }

        // 1. Preceded by dot '.' (e.g. tbl.select) -> member access, do NOT format
        if (prevToken.TokenType == SqlParserTokenType.Dot)
        {
          return false;
        }

        // 2. Preceded by clause headers like FROM, JOIN, INTO, UPDATE, TABLE (without '(')
        // e.g. "select * from select" -> table name position, do NOT format
        if (prevToken.IsKeyword && TableSourceKeywords.Contains(prevToken.Text))
        {
          return false;
        }

        break;
      }

      string originalText = token.Text;
      string newText = (casing == KeywordCasing.Uppercase)
        ? originalText.ToUpperInvariant()
        : originalText.ToLowerInvariant();

      // If already matching the desired casing, no replacement needed
      if (string.Equals(originalText, newText, StringComparison.Ordinal))
      {
        return false;
      }

      targetSpan = (token.Offset, token.Length);
      formattedWord = newText;
      return true;
    }
  }
}
