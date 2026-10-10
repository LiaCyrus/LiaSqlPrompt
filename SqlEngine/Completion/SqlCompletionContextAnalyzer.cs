using System;
using System.Collections.Generic;
using SqlEngine.Ast;
using SqlEngine.Parsing;

namespace SqlEngine.Completion
{
  public static class SqlCompletionContextAnalyzer
  {
    public static SqlCompletionContext Analyze(string sql, int cursorPosition)
    {
      if (sql == null) sql = string.Empty;
      cursorPosition = Math.Max(0, Math.Min(cursorPosition, sql.Length));

      // 1. Extract prefix and prefix token boundary
      var (prefix, prefixStart) = ExtractPrefix(sql, cursorPosition);

      // 2. Tokenize SQL text up to prefixStart
      int effectiveOffset = prefixStart;
      string contextSql = sql.Substring(0, effectiveOffset);

      var allTokens = SqlQueryParser.ParseTokens(contextSql);

      // Filter out whitespace, comments, and EOF for structural analysis
      var meaningfulTokens = new List<SqlParsedToken>();
      foreach (var t in allTokens)
      {
        if (t.TokenType != SqlParserTokenType.Whitespace &&
            t.TokenType != SqlParserTokenType.Comment &&
            t.TokenType != SqlParserTokenType.EndOfFile)
        {
          meaningfulTokens.Add(t);
        }
      }

      // 3. Dot qualifier check (e.g. c. or c.Na or o.)
      if (effectiveOffset > 0 && sql[effectiveOffset - 1] == '.')
      {
        string? tableAlias = GetDotQualifier(sql, effectiveOffset - 1);
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.ColumnReference,
          Prefix = prefix,
          TableAlias = tableAlias,
          CursorPosition = cursorPosition
        };
      }

      if (meaningfulTokens.Count > 0 && meaningfulTokens[meaningfulTokens.Count - 1].TokenType == SqlParserTokenType.Dot)
      {
        string? tableAlias = meaningfulTokens.Count >= 2 ? meaningfulTokens[meaningfulTokens.Count - 2].Text : null;
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.ColumnReference,
          Prefix = prefix,
          TableAlias = tableAlias,
          CursorPosition = cursorPosition
        };
      }

      // If empty or no tokens before cursor
      if (meaningfulTokens.Count == 0)
      {
        return new SqlCompletionContext
        {
          StatementType = null,
          Kind = SqlCompletionContextKind.StatementStart,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 4. Multiple statements / block isolation:
      // Slice tokens to the current statement scope (after the last semicolon ';' or GO, or inside BEGIN...END/parentheses)
      var scopedTokens = GetCurrentStatementTokens(meaningfulTokens);

      // 5. Check if inside nested subquery parentheses: e.g. "WHERE Id IN ( SELECT | )"
      var (inSubquery, subqueryTokens) = ExtractInnermostSubquery(scopedTokens);
      if (inSubquery && subqueryTokens != null && subqueryTokens.Count > 0)
      {
        return AnalyzeTokens(subqueryTokens, prefix, cursorPosition);
      }

      // 6. Analyze current statement tokens
      return AnalyzeTokens(scopedTokens, prefix, cursorPosition);
    }

    private static List<SqlParsedToken> GetCurrentStatementTokens(List<SqlParsedToken> tokens)
    {
      if (tokens.Count == 0) return tokens;

      int start = 0;
      for (int i = 0; i < tokens.Count; i++)
      {
        string text = tokens[i].Text;
        if (text == ";" || text.Equals("GO", StringComparison.OrdinalIgnoreCase))
        {
          start = i + 1;
        }
      }

      if (start == 0)
      {
        return tokens;
      }

      var result = new List<SqlParsedToken>(tokens.Count - start);
      for (int i = start; i < tokens.Count; i++)
      {
        result.Add(tokens[i]);
      }
      return result;
    }

    private static (bool InSubquery, List<SqlParsedToken>? SubqueryTokens) ExtractInnermostSubquery(List<SqlParsedToken> tokens)
    {
      int parenDepth = 0;
      int lastOpenParenIndex = -1;

      for (int i = 0; i < tokens.Count; i++)
      {
        var t = tokens[i];
        if (t.TokenType == SqlParserTokenType.LeftParenthesis || t.Text == "(")
        {
          parenDepth++;
          lastOpenParenIndex = i;
        }
        else if (t.TokenType == SqlParserTokenType.RightParenthesis || t.Text == ")")
        {
          parenDepth--;
          if (parenDepth == 0)
          {
            lastOpenParenIndex = -1;
          }
        }
      }

      if (parenDepth > 0 && lastOpenParenIndex >= 0)
      {
        var inner = new List<SqlParsedToken>();
        for (int i = lastOpenParenIndex + 1; i < tokens.Count; i++)
        {
          inner.Add(tokens[i]);
        }

        // Check if inner tokens start with or are intended for a query
        if (inner.Count > 0)
        {
          string firstText = inner[0].Text;
          if (firstText.Equals("SELECT", StringComparison.OrdinalIgnoreCase))
          {
            return (true, inner);
          }
        }
        else
        {
          // e.g. "IN (" or "EXISTS (" -> next word can be SELECT
          if (lastOpenParenIndex > 0)
          {
            string prev = tokens[lastOpenParenIndex - 1].Text;
            if (prev.Equals("IN", StringComparison.OrdinalIgnoreCase) ||
                prev.Equals("EXISTS", StringComparison.OrdinalIgnoreCase) ||
                prev.Equals("AS", StringComparison.OrdinalIgnoreCase))
            {
              return (true, inner);
            }
          }
        }
      }

      return (false, null);
    }

    private static SqlCompletionContext AnalyzeTokens(
      List<SqlParsedToken> tokens,
      string prefix,
      int cursorPosition)
    {
      if (tokens.Count == 0)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.SelectExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // Check the leading statement keyword
      string firstWord = tokens[0].Text.ToUpperInvariant();

      // 1. CTE: "WITH ..."
      if (firstWord == "WITH")
      {
        return AnalyzeWithCteTokens(tokens, prefix, cursorPosition);
      }

      // 2. INSERT
      if (firstWord == "INSERT")
      {
        return AnalyzeInsertTokens(tokens, prefix, cursorPosition);
      }

      // 3. UPDATE
      if (firstWord == "UPDATE")
      {
        return AnalyzeUpdateTokens(tokens, prefix, cursorPosition);
      }

      // 4. DELETE
      if (firstWord == "DELETE")
      {
        return AnalyzeDeleteTokens(tokens, prefix, cursorPosition);
      }

      // 5. MERGE
      if (firstWord == "MERGE")
      {
        return AnalyzeMergeTokens(tokens, prefix, cursorPosition);
      }

      // 6. CREATE
      if (firstWord == "CREATE")
      {
        return AnalyzeCreateTokens(tokens, prefix, cursorPosition);
      }

      // 7. ALTER
      if (firstWord == "ALTER")
      {
        return AnalyzeAlterTokens(tokens, prefix, cursorPosition);
      }

      // 8. DROP
      if (firstWord == "DROP")
      {
        return AnalyzeDropTokens(tokens, prefix, cursorPosition);
      }

      // 9. EXEC / EXECUTE
      if (firstWord == "EXEC" || firstWord == "EXECUTE")
      {
        return AnalyzeExecuteTokens(tokens, prefix, cursorPosition);
      }

      // 10. DECLARE
      if (firstWord == "DECLARE")
      {
        return AnalyzeDeclareTokens(tokens, prefix, cursorPosition);
      }

      // 11. SET
      if (firstWord == "SET")
      {
        return AnalyzeSetTokens(tokens, prefix, cursorPosition);
      }

      // 12. IF
      if (firstWord == "IF")
      {
        return AnalyzeIfTokens(tokens, prefix, cursorPosition);
      }

      // 13. WHILE
      if (firstWord == "WHILE")
      {
        return AnalyzeWhileTokens(tokens, prefix, cursorPosition);
      }

      // 14. BEGIN
      if (firstWord == "BEGIN")
      {
        return AnalyzeBeginTokens(tokens, prefix, cursorPosition);
      }

      // 15. COMMIT / ROLLBACK / SAVE
      if (firstWord == "COMMIT" || firstWord == "ROLLBACK" || firstWord == "SAVE")
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.TransactionStatement,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 16. USE
      if (firstWord == "USE")
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.UseDatabase,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 17. PRINT
      if (firstWord == "PRINT")
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.PrintExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 18. WAITFOR
      if (firstWord == "WAITFOR")
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.WaitForStatement,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // Default: SELECT / Query flow
      return AnalyzeSelectTokens(tokens, prefix, cursorPosition);
    }

    #region Statement Analyzers

    private static SqlCompletionContext AnalyzeWithCteTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      int lastIndex = tokens.Count - 1;
      var lastToken = tokens[lastIndex];

      // "WITH |"
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.CteStart,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // Check if after CTE closing parenthesis, outer query starts:
      // "WITH Cte AS (...) SELECT |"
      int lastRightParen = -1;
      for (int i = tokens.Count - 1; i >= 0; i--)
      {
        if (tokens[i].TokenType == SqlParserTokenType.RightParenthesis || tokens[i].Text == ")")
        {
          lastRightParen = i;
          break;
        }
      }

      if (lastRightParen >= 0 && lastRightParen < tokens.Count - 1)
      {
        var afterParenTokens = new List<SqlParsedToken>();
        for (int i = lastRightParen + 1; i < tokens.Count; i++)
        {
          afterParenTokens.Add(tokens[i]);
        }
        return AnalyzeTokens(afterParenTokens, prefix, cursorPosition);
      }

      // "WITH CustomerOrders |"
      if (tokens.Count == 2)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.CteName,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // "WITH CustomerOrders AS ( |"
      if (lastToken.Text == "(")
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.SelectExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Select,
        Kind = SqlCompletionContextKind.CteQuery,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeInsertTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      int lastIndex = tokens.Count - 1;
      var lastToken = tokens[lastIndex];

      // "INSERT |"
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Insert,
          Kind = SqlCompletionContextKind.InsertInto,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // "INSERT INTO |"
      if (lastToken.Text.Equals("INTO", StringComparison.OrdinalIgnoreCase))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Insert,
          Kind = SqlCompletionContextKind.InsertTable,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // Check for VALUES keyword
      int valuesIndex = FindTokenIndex(tokens, "VALUES");
      if (valuesIndex >= 0)
      {
        // "INSERT INTO Customer VALUES ( |"
        if (lastToken.Text == "(")
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Insert,
            Kind = SqlCompletionContextKind.InsertValueExpression,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }

        return new SqlCompletionContext
        {
          StatementType = StatementType.Insert,
          Kind = SqlCompletionContextKind.InsertValueExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // Check for SELECT after INSERT: "INSERT INTO Customer SELECT |"
      int selectIndex = FindTokenIndex(tokens, "SELECT");
      if (selectIndex >= 0)
      {
        var selectTokens = tokens.GetRange(selectIndex, tokens.Count - selectIndex);
        return AnalyzeSelectTokens(selectTokens, prefix, cursorPosition);
      }

      // Column list: "INSERT INTO Customer ( |"
      if (lastToken.Text == "(")
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Insert,
          Kind = SqlCompletionContextKind.InsertColumnList,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // "INSERT INTO Customer |" or "INSERT INTO Customer (Col) |"
      return new SqlCompletionContext
      {
        StatementType = StatementType.Insert,
        Kind = SqlCompletionContextKind.InsertValues,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeUpdateTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      int lastIndex = tokens.Count - 1;
      var lastToken = tokens[lastIndex];

      // "UPDATE |"
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Update,
          Kind = SqlCompletionContextKind.UpdateTable,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      int setIndex = FindTokenIndex(tokens, "SET");
      int whereIndex = FindTokenIndex(tokens, "WHERE");
      int fromIndex = FindTokenIndex(tokens, "FROM");

      if (whereIndex >= 0)
      {
        if (lastToken.Text.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Update,
            Kind = SqlCompletionContextKind.WhereExpression,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
        return new SqlCompletionContext
        {
          StatementType = StatementType.Update,
          Kind = SqlCompletionContextKind.WhereExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      if (setIndex >= 0)
      {
        // "UPDATE Customer SET |"
        if (lastToken.Text.Equals("SET", StringComparison.OrdinalIgnoreCase))
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Update,
            Kind = SqlCompletionContextKind.UpdateColumn,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }

        // "UPDATE Customer SET Name = |"
        if (lastToken.Text == "=")
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Update,
            Kind = SqlCompletionContextKind.UpdateValue,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }

        // "UPDATE Customer SET Name = 'A' |"
        return new SqlCompletionContext
        {
          StatementType = StatementType.Update,
          Kind = SqlCompletionContextKind.UpdateFrom,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // "UPDATE Customer |"
      return new SqlCompletionContext
      {
        StatementType = StatementType.Update,
        Kind = SqlCompletionContextKind.UpdateSet,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeDeleteTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      int lastIndex = tokens.Count - 1;
      var lastToken = tokens[lastIndex];

      // "DELETE |"
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Delete,
          Kind = SqlCompletionContextKind.DeleteFrom,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // "DELETE FROM |"
      if (lastToken.Text.Equals("FROM", StringComparison.OrdinalIgnoreCase))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Delete,
          Kind = SqlCompletionContextKind.TableSource,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      int whereIndex = FindTokenIndex(tokens, "WHERE");
      if (whereIndex >= 0)
      {
        if (lastToken.Text.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Delete,
            Kind = SqlCompletionContextKind.WhereExpression,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
        return new SqlCompletionContext
        {
          StatementType = StatementType.Delete,
          Kind = SqlCompletionContextKind.WhereExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // "DELETE FROM Customer |" or "DELETE c |"
      return new SqlCompletionContext
      {
        StatementType = StatementType.Delete,
        Kind = SqlCompletionContextKind.DeleteWhere,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeMergeTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      int lastIndex = tokens.Count - 1;
      var lastToken = tokens[lastIndex];

      // "MERGE |"
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Merge,
          Kind = SqlCompletionContextKind.MergeInto,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      int intoIndex = FindTokenIndex(tokens, "INTO");
      int usingIndex = FindTokenIndex(tokens, "USING");
      int onIndex = FindTokenIndex(tokens, "ON");
      int whenIndex = FindLastTokenIndex(tokens, "WHEN");

      // "WHEN MATCHED THEN |" or "WHEN NOT MATCHED THEN |"
      int thenIndex = FindLastTokenIndex(tokens, "THEN");
      if (thenIndex >= 0 && thenIndex > whenIndex)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Merge,
          Kind = SqlCompletionContextKind.MergeAction,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // "... ON target.Id = source.Id WHEN |" or after ON condition
      if (whenIndex >= 0)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Merge,
          Kind = SqlCompletionContextKind.MergeWhen,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      if (onIndex >= 0)
      {
        if (lastToken.Text.Equals("ON", StringComparison.OrdinalIgnoreCase))
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Merge,
            Kind = SqlCompletionContextKind.JoinCondition,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
        return new SqlCompletionContext
        {
          StatementType = StatementType.Merge,
          Kind = SqlCompletionContextKind.MergeWhen,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      if (usingIndex >= 0)
      {
        if (lastToken.Text.Equals("USING", StringComparison.OrdinalIgnoreCase))
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Merge,
            Kind = SqlCompletionContextKind.MergeSource,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
        return new SqlCompletionContext
        {
          StatementType = StatementType.Merge,
          Kind = SqlCompletionContextKind.MergeOn,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      if (intoIndex >= 0)
      {
        if (lastToken.Text.Equals("INTO", StringComparison.OrdinalIgnoreCase))
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Merge,
            Kind = SqlCompletionContextKind.MergeTarget,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
        return new SqlCompletionContext
        {
          StatementType = StatementType.Merge,
          Kind = SqlCompletionContextKind.MergeUsing,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Merge,
        Kind = SqlCompletionContextKind.MergeInto,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeCreateTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      int lastIndex = tokens.Count - 1;
      var lastToken = tokens[lastIndex];

      // "CREATE |"
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.CreateStatement,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      string secondWord = tokens[1].Text.ToUpperInvariant();

      // Check if inside CREATE VIEW CustomerView AS SELECT | or CREATE PROCEDURE ... AS BEGIN SELECT |
      int asIndex = FindTokenIndex(tokens, "AS");
      if (asIndex >= 0)
      {
        // Check for body statements
        int selectIndex = FindLastTokenIndex(tokens, "SELECT");
        if (selectIndex >= 0)
        {
          var bodyTokens = tokens.GetRange(selectIndex, tokens.Count - selectIndex);
          return AnalyzeSelectTokens(bodyTokens, prefix, cursorPosition);
        }

        if (lastToken.Text.Equals("AS", StringComparison.OrdinalIgnoreCase) ||
            lastToken.Text.Equals("BEGIN", StringComparison.OrdinalIgnoreCase))
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Select,
            Kind = SqlCompletionContextKind.StatementStart,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
      }

      // "CREATE TABLE Customer ( |"
      if (secondWord == "TABLE")
      {
        int parenIndex = FindTokenIndex(tokens, "(");
        if (parenIndex >= 0)
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Unknown,
            Kind = SqlCompletionContextKind.CreateTableColumnDefinition,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }

        if (tokens.Count == 2)
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Unknown,
            Kind = SqlCompletionContextKind.CreateObjectName,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
      }

      // "CREATE VIEW |", "CREATE PROCEDURE |", etc.
      if (tokens.Count == 2)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.CreateObjectName,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Unknown,
        Kind = SqlCompletionContextKind.StatementStart,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeAlterTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.AlterStatement,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      string secondWord = tokens[1].Text.ToUpperInvariant();

      // "ALTER TABLE Customer |"
      if (secondWord == "TABLE")
      {
        if (tokens.Count == 2)
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Unknown,
            Kind = SqlCompletionContextKind.AlterTable,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }

        if (tokens.Count == 3)
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Unknown,
            Kind = SqlCompletionContextKind.AlterTableAction,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }

        if (tokens.Count > 3)
        {
          string action = tokens[3].Text.ToUpperInvariant();
          if (action == "ADD")
          {
            return new SqlCompletionContext
            {
              StatementType = StatementType.Unknown,
              Kind = SqlCompletionContextKind.CreateTableColumnDefinition,
              Prefix = prefix,
              CursorPosition = cursorPosition
            };
          }
        }
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Unknown,
        Kind = SqlCompletionContextKind.AlterStatement,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeDropTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.DropStatement,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Unknown,
        Kind = SqlCompletionContextKind.DropObject,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeExecuteTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Execute,
          Kind = SqlCompletionContextKind.ExecuteObject,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Execute,
        Kind = SqlCompletionContextKind.ExecuteParameter,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeDeclareTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      int lastIndex = tokens.Count - 1;
      var lastToken = tokens[lastIndex];

      // "DECLARE |"
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.Variable,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // "DECLARE @Id |"
      if (lastToken.Text.StartsWith("@"))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.DeclareVariable,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Unknown,
        Kind = SqlCompletionContextKind.DeclareVariable,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeSetTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.SetVariable,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Unknown,
        Kind = SqlCompletionContextKind.Expression,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeIfTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      int beginIndex = FindTokenIndex(tokens, "BEGIN");
      if (beginIndex >= 0)
      {
        var bodyTokens = tokens.GetRange(beginIndex + 1, tokens.Count - (beginIndex + 1));
        return AnalyzeTokens(bodyTokens, prefix, cursorPosition);
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Unknown,
        Kind = SqlCompletionContextKind.IfCondition,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeWhileTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      int beginIndex = FindTokenIndex(tokens, "BEGIN");
      if (beginIndex >= 0)
      {
        var bodyTokens = tokens.GetRange(beginIndex + 1, tokens.Count - (beginIndex + 1));
        return AnalyzeTokens(bodyTokens, prefix, cursorPosition);
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Unknown,
        Kind = SqlCompletionContextKind.WhileCondition,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    private static SqlCompletionContext AnalyzeBeginTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      // "BEGIN |"
      if (tokens.Count == 1)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.StatementStart,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      string second = tokens[1].Text.ToUpperInvariant();
      if (second == "TRANSACTION" || second == "TRAN")
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Unknown,
          Kind = SqlCompletionContextKind.TransactionStatement,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // Inside BEGIN ... END block: analyze inner statements
      var inner = tokens.GetRange(1, tokens.Count - 1);
      return AnalyzeTokens(inner, prefix, cursorPosition);
    }

    private static SqlCompletionContext AnalyzeSelectTokens(List<SqlParsedToken> tokens, string prefix, int cursorPosition)
    {
      int lastIndex = tokens.Count - 1;
      var lastToken = tokens[lastIndex];

      // 1. Right after ON -> JoinCondition
      if (lastToken.Text.Equals("ON", StringComparison.OrdinalIgnoreCase))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.JoinCondition,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 2. Right after ORDER BY -> OrderByExpression
      if (tokens.Count >= 2 &&
          tokens[lastIndex - 1].Text.Equals("ORDER", StringComparison.OrdinalIgnoreCase) &&
          lastToken.Text.Equals("BY", StringComparison.OrdinalIgnoreCase))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.OrderByExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 3. Right after GROUP BY -> GroupByExpression
      if (tokens.Count >= 2 &&
          tokens[lastIndex - 1].Text.Equals("GROUP", StringComparison.OrdinalIgnoreCase) &&
          lastToken.Text.Equals("BY", StringComparison.OrdinalIgnoreCase))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.GroupByExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 4. Right after HAVING -> HavingExpression
      if (lastToken.Text.Equals("HAVING", StringComparison.OrdinalIgnoreCase))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.HavingExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 5. Right after WHERE -> WhereExpression
      if (lastToken.Text.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.WhereExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 6. Right after FROM -> TableSource
      if (lastToken.Text.Equals("FROM", StringComparison.OrdinalIgnoreCase))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.TableSource,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 7. Right after JOIN or [TYPE] JOIN (e.g. INNER JOIN, LEFT JOIN) -> TableSource
      if (lastToken.Text.Equals("JOIN", StringComparison.OrdinalIgnoreCase))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.TableSource,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 8. Right after SELECT -> SelectExpression
      if (lastToken.Text.Equals("SELECT", StringComparison.OrdinalIgnoreCase))
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.SelectExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // 9. Scan backwards for clause indices
      int fromIndex = -1;
      int whereIndex = -1;
      int selectIndex = -1;
      int joinIndex = -1;
      int onIndex = -1;
      int orderByIndex = -1;

      for (int i = 0; i < tokens.Count; i++)
      {
        string text = tokens[i].Text;
        if (text.Equals("SELECT", StringComparison.OrdinalIgnoreCase)) selectIndex = i;
        else if (text.Equals("FROM", StringComparison.OrdinalIgnoreCase)) fromIndex = i;
        else if (text.Equals("JOIN", StringComparison.OrdinalIgnoreCase)) joinIndex = i;
        else if (text.Equals("ON", StringComparison.OrdinalIgnoreCase)) onIndex = i;
        else if (text.Equals("WHERE", StringComparison.OrdinalIgnoreCase)) whereIndex = i;
        else if (i + 1 < tokens.Count &&
                 text.Equals("ORDER", StringComparison.OrdinalIgnoreCase) &&
                 tokens[i + 1].Text.Equals("BY", StringComparison.OrdinalIgnoreCase))
        {
          orderByIndex = i;
        }
      }

      // If in ORDER BY clause after column name: "ORDER BY c.Name |" -> ASC, DESC or QueryClause
      if (orderByIndex >= 0 && orderByIndex > whereIndex && orderByIndex > fromIndex)
      {
        int tokensAfterOrderBy = (tokens.Count - 1) - (orderByIndex + 1);
        if (tokensAfterOrderBy >= 1)
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Select,
            Kind = SqlCompletionContextKind.OrderByExpression,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
      }

      // If we are after WHERE
      if (whereIndex >= 0 && whereIndex > fromIndex && whereIndex > joinIndex)
      {
        // If immediately after an operator or comparison symbol (e.g. '=', '<>', '<', '>', '+', '-', etc.),
        // this is an expression / column / value position, NOT a query clause continuation.
        if (tokens.Count > 0)
        {
          var lastNonTrivia = tokens[tokens.Count - 1];
          if (lastNonTrivia.TokenType == SqlParserTokenType.Symbol)
          {
            return new SqlCompletionContext
            {
              StatementType = StatementType.Select,
              Kind = SqlCompletionContextKind.ColumnReference,
              Prefix = prefix,
              CursorPosition = cursorPosition
            };
          }
        }

        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.QueryClause,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // If after JOIN ... ON ...
      if (joinIndex >= 0 && onIndex > joinIndex)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.QueryClause,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      // If after JOIN (without ON yet):
      // If tokensAfterJoin == 0: right after JOIN -> TableSource
      // If tokensAfterJoin >= 1: table name (and optional alias) already present -> QueryClause (e.g. ON, WHERE, JOIN)
      if (joinIndex >= 0 && joinIndex > fromIndex && onIndex < joinIndex)
      {
        int tokensAfterJoin = (tokens.Count - 1) - joinIndex;
        if (tokensAfterJoin >= 1)
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Select,
            Kind = SqlCompletionContextKind.QueryClause,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
        else
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Select,
            Kind = SqlCompletionContextKind.TableSource,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
      }

      // If after FROM:
      // If tokensAfterFrom == 0: right after FROM (e.g. "SELECT * FROM |" or "SELECT * FROM m52") -> TableSource
      // If tokensAfterFrom >= 1: table name already specified (e.g. "SELECT * FROM Customer |" or "SELECT * FROM Customer c |") -> QueryClause
      if (fromIndex >= 0)
      {
        int tokensAfterFrom = (tokens.Count - 1) - fromIndex;
        if (tokensAfterFrom >= 1)
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Select,
            Kind = SqlCompletionContextKind.QueryClause,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
        else
        {
          return new SqlCompletionContext
          {
            StatementType = StatementType.Select,
            Kind = SqlCompletionContextKind.TableSource,
            Prefix = prefix,
            CursorPosition = cursorPosition
          };
        }
      }

      // If after SELECT but before FROM
      if (selectIndex >= 0 && fromIndex < 0)
      {
        return new SqlCompletionContext
        {
          StatementType = StatementType.Select,
          Kind = SqlCompletionContextKind.SelectExpression,
          Prefix = prefix,
          CursorPosition = cursorPosition
        };
      }

      return new SqlCompletionContext
      {
        StatementType = StatementType.Select,
        Kind = SqlCompletionContextKind.Unknown,
        Prefix = prefix,
        CursorPosition = cursorPosition
      };
    }

    #endregion

    #region Helper Methods

    private static int FindTokenIndex(List<SqlParsedToken> tokens, string tokenText)
    {
      for (int i = 0; i < tokens.Count; i++)
      {
        if (tokens[i].Text.Equals(tokenText, StringComparison.OrdinalIgnoreCase))
          return i;
      }
      return -1;
    }

    private static int FindLastTokenIndex(List<SqlParsedToken> tokens, string tokenText)
    {
      for (int i = tokens.Count - 1; i >= 0; i--)
      {
        if (tokens[i].Text.Equals(tokenText, StringComparison.OrdinalIgnoreCase))
          return i;
      }
      return -1;
    }

    private static (string Prefix, int PrefixStart) ExtractPrefix(string sql, int cursorPosition)
    {
      int pos = cursorPosition - 1;
      if (pos < 0 || pos >= sql.Length)
      {
        return (string.Empty, cursorPosition);
      }

      while (pos >= 0 && (char.IsLetterOrDigit(sql[pos]) || sql[pos] == '_'))
      {
        pos--;
      }

      int start = pos + 1;
      string prefix = sql.Substring(start, cursorPosition - start);
      return (prefix, start);
    }

    private static string? GetDotQualifier(string sql, int dotIndex)
    {
      int pos = dotIndex - 1;
      while (pos >= 0 && char.IsWhiteSpace(sql[pos]))
      {
        pos--;
      }

      if (pos < 0) return null;

      int end = pos;
      while (pos >= 0 && (char.IsLetterOrDigit(sql[pos]) || sql[pos] == '_' || sql[pos] == ']' || sql[pos] == '['))
      {
        pos--;
      }

      string qualifier = sql.Substring(pos + 1, end - pos);
      return qualifier.Trim('[', ']', ' ');
    }

    #endregion
  }
}
