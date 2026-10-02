using System;
using System.Collections.Generic;
using System.Text;

namespace SqlEngine
{
  public static class SqlTokenizer
  {
    public static List<SqlToken> Tokenize(string sql)
    {
      var tokens = new List<SqlToken>();

      if (string.IsNullOrEmpty(sql))
        return tokens;

      int length = sql.Length;
      int index = 0;

      while (index < length)
      {
        char ch = sql[index];

        // 1. Whitespace
        if (char.IsWhiteSpace(ch))
        {
          int start = index;
          while (index < length && char.IsWhiteSpace(sql[index]))
          {
            index++;
          }
          tokens.Add(new SqlToken(SqlTokenType.Whitespace, sql.Substring(start, index - start), start));
          continue;
        }

        // 2. Line comment (-- ...)
        if (ch == '-' && index + 1 < length && sql[index + 1] == '-')
        {
          int start = index;
          index += 2;
          while (index < length && sql[index] != '\r' && sql[index] != '\n')
          {
            index++;
          }
          tokens.Add(new SqlToken(SqlTokenType.Comment, sql.Substring(start, index - start), start));
          continue;
        }

        // 3. Block comment (/* ... */)
        if (ch == '/' && index + 1 < length && sql[index + 1] == '*')
        {
          int start = index;
          index += 2;
          while (index < length)
          {
            if (sql[index] == '*' && index + 1 < length && sql[index + 1] == '/')
            {
              index += 2;
              break;
            }
            index++;
          }
          tokens.Add(new SqlToken(SqlTokenType.Comment, sql.Substring(start, index - start), start));
          continue;
        }

        // 4. Bracketed identifier ([schema].[table])
        if (ch == '[')
        {
          int start = index;
          index++;
          while (index < length && sql[index] != ']')
          {
            index++;
          }
          if (index < length && sql[index] == ']')
          {
            index++;
          }
          tokens.Add(new SqlToken(SqlTokenType.Identifier, sql.Substring(start, index - start), start));
          continue;
        }

        // 5. String literal ('text' or N'unicode')
        if (ch == '\'' || ((ch == 'N' || ch == 'n') && index + 1 < length && sql[index + 1] == '\''))
        {
          int start = index;
          if (ch == 'N' || ch == 'n')
            index += 2;
          else
            index++;

          while (index < length)
          {
            if (sql[index] == '\'')
            {
              index++;
              if (index < length && sql[index] == '\'')
              {
                // Escaped quote ('')
                index++;
                continue;
              }
              break;
            }
            index++;
          }
          tokens.Add(new SqlToken(SqlTokenType.StringLiteral, sql.Substring(start, index - start), start));
          continue;
        }

        // 6. Number literal
        if (char.IsDigit(ch) || (ch == '.' && index + 1 < length && char.IsDigit(sql[index + 1])))
        {
          int start = index;
          bool hasDecimal = (ch == '.');
          index++;

          while (index < length)
          {
            char current = sql[index];
            if (char.IsDigit(current))
            {
              index++;
            }
            else if (current == '.' && !hasDecimal)
            {
              hasDecimal = true;
              index++;
            }
            else
            {
              break;
            }
          }
          tokens.Add(new SqlToken(SqlTokenType.NumberLiteral, sql.Substring(start, index - start), start));
          continue;
        }

        // 7. Word: Keyword, Identifier, or Variable (@var, #temp)
        if (char.IsLetter(ch) || ch == '_' || ch == '@' || ch == '#')
        {
          int start = index;
          index++;

          while (index < length)
          {
            char current = sql[index];
            if (char.IsLetterOrDigit(current) || current == '_' || current == '@' || current == '#')
            {
              index++;
            }
            else
            {
              break;
            }
          }

          string text = sql.Substring(start, index - start);
          SqlTokenType type;

          if (text.StartsWith("@") || text.StartsWith("#"))
          {
            type = SqlTokenType.Identifier;
          }
          else if (SqlKeywords.IsKeyword(text))
          {
            type = SqlTokenType.Keyword;
          }
          else
          {
            type = SqlTokenType.Identifier;
          }

          tokens.Add(new SqlToken(type, text, start));
          continue;
        }

        // 8. Symbols and Operators
        tokens.Add(new SqlToken(SqlTokenType.Symbol, ch.ToString(), index));
        index++;
      }

      return tokens;
    }
  }
}
