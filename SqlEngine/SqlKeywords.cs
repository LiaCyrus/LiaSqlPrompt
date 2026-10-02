using System;
using System.Collections.Generic;

namespace SqlEngine
{
  public static class SqlKeywords
  {
    private static readonly HashSet<string> KeywordsSet;
    private static readonly string[] AllKeywordsArray;

    static SqlKeywords()
    {
      AllKeywordsArray = new[]
      {
        // Data Query & Manipulation
        "SELECT", "FROM", "WHERE", "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC",
        "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE", "MERGE", "MATCHED",
        "OUTPUT", "TOP", "DISTINCT", "AS", "TRUNCATE",

        // Joins & Set Operators
        "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS", "ON",
        "UNION", "ALL", "EXCEPT", "INTERSECT", "APPLY",

        // Logical & Comparison Predicates
        "AND", "OR", "NOT", "IN", "BETWEEN", "LIKE", "IS", "NULL", "EXISTS",
        "ANY", "SOME", "ALL", "CASE", "WHEN", "THEN", "ELSE", "END",

        // DDL & Object Management
        "CREATE", "ALTER", "DROP", "TABLE", "VIEW", "INDEX", "PROCEDURE", "PROC",
        "FUNCTION", "TRIGGER", "DATABASE", "SCHEMA", "SEQUENCE", "SYNONYM",
        "PRIMARY", "KEY", "FOREIGN", "REFERENCES", "CHECK", "UNIQUE", "DEFAULT",
        "CONSTRAINT", "CLUSTERED", "NONCLUSTERED", "COLUMN", "ADD",

        // Procedural & Flow Control
        "BEGIN", "TRAN", "TRANSACTION", "COMMIT", "ROLLBACK", "SAVE",
        "IF", "ELSE", "WHILE", "BREAK", "CONTINUE", "RETURN", "GOTO",
        "TRY", "CATCH", "THROW", "RAISERROR", "WAITFOR", "DELAY",

        // Execution & Dynamic SQL
        "EXEC", "EXECUTE", "WITH", "RECURSIVE", "CTE", "OVER", "PARTITION",

        // Permissions & Security
        "GRANT", "REVOKE", "DENY", "USER", "ROLE", "LOGIN",

        // Common Data Types
        "INT", "BIGINT", "SMALLINT", "TINYINT", "BIT", "DECIMAL", "NUMERIC",
        "MONEY", "SMALLMONEY", "FLOAT", "REAL", "DATE", "DATETIME", "DATETIME2",
        "SMALLDATETIME", "TIME", "DATETIMEOFFSET", "CHAR", "VARCHAR", "TEXT",
        "NCHAR", "NVARCHAR", "NTEXT", "BINARY", "VARBINARY", "IMAGE", "XML",
        "UNIQUEIDENTIFIER", "MAX",

        // Common Aggregate & Built-in functions / Keywords
        "COUNT", "SUM", "AVG", "MIN", "MAX", "COALESCE", "ISNULL", "NULLIF",
        "CAST", "CONVERT", "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE",
        "GETDATE", "GETUTCDATE", "SYSDATETIME", "DATEDIFF", "DATEADD", "DATEPART",
        "LEN", "DATALENGTH", "SUBSTRING", "CHARINDEX", "REPLACE", "UPPER", "LOWER", "LTRIM", "RTRIM", "TRIM"
      };

      KeywordsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var kw in AllKeywordsArray)
      {
        KeywordsSet.Add(kw);
      }
    }

    public static IReadOnlyCollection<string> All => AllKeywordsArray;

    public static bool IsKeyword(string word)
    {
      if (string.IsNullOrEmpty(word))
        return false;

      return KeywordsSet.Contains(word);
    }
  }
}
