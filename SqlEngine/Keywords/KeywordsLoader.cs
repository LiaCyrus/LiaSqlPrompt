using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using SqlEngine.Completion;

namespace SqlEngine
{
  /// <summary>
  /// Represents an individual typed keyword item declarable in JSON.
  /// </summary>
  [DataContract]
  public sealed class TypedSqlKeyword
  {
    [DataMember(Name = "keyword", EmitDefaultValue = false)]
    public string Keyword { get; set; } = string.Empty;

    [DataMember(Name = "type", EmitDefaultValue = false)]
    public string Type { get; set; } = string.Empty;

    public TypedSqlKeyword() { }

    public TypedSqlKeyword(string keyword, string type)
    {
      Keyword = keyword;
      Type = type;
    }
  }

  [DataContract]
  public sealed class KeywordsConfiguration
  {
    // Contextual categories
    [DataMember(Name = "statementStart", EmitDefaultValue = false)]
    public List<string> StatementStart { get; set; }

    [DataMember(Name = "queryClauses", EmitDefaultValue = false)]
    public List<string> QueryClauses { get; set; }

    [DataMember(Name = "whereExpressions", EmitDefaultValue = false)]
    public List<string> WhereExpressions { get; set; }

    [DataMember(Name = "joinConditions", EmitDefaultValue = false)]
    public List<string> JoinConditions { get; set; }

    [DataMember(Name = "orderByExpressions", EmitDefaultValue = false)]
    public List<string> OrderByExpressions { get; set; }

    [DataMember(Name = "selectExpressions", EmitDefaultValue = false)]
    public List<string> SelectExpressions { get; set; }

    [DataMember(Name = "insertInto", EmitDefaultValue = false)]
    public List<string> InsertInto { get; set; }

    [DataMember(Name = "insertValues", EmitDefaultValue = false)]
    public List<string> InsertValues { get; set; }

    [DataMember(Name = "updateSet", EmitDefaultValue = false)]
    public List<string> UpdateSet { get; set; }

    [DataMember(Name = "updateClauses", EmitDefaultValue = false)]
    public List<string> UpdateClauses { get; set; }

    [DataMember(Name = "deleteClauses", EmitDefaultValue = false)]
    public List<string> DeleteClauses { get; set; }

    [DataMember(Name = "mergeInto", EmitDefaultValue = false)]
    public List<string> MergeInto { get; set; }

    [DataMember(Name = "mergeUsing", EmitDefaultValue = false)]
    public List<string> MergeUsing { get; set; }

    [DataMember(Name = "mergeWhen", EmitDefaultValue = false)]
    public List<string> MergeWhen { get; set; }

    [DataMember(Name = "mergeActions", EmitDefaultValue = false)]
    public List<string> MergeActions { get; set; }

    [DataMember(Name = "createObjects", EmitDefaultValue = false)]
    public List<string> CreateObjects { get; set; }

    [DataMember(Name = "alterObjects", EmitDefaultValue = false)]
    public List<string> AlterObjects { get; set; }

    [DataMember(Name = "alterTableActions", EmitDefaultValue = false)]
    public List<string> AlterTableActions { get; set; }

    [DataMember(Name = "dropObjects", EmitDefaultValue = false)]
    public List<string> DropObjects { get; set; }

    [DataMember(Name = "dataTypes", EmitDefaultValue = false)]
    public List<string> DataTypes { get; set; }

    [DataMember(Name = "functions", EmitDefaultValue = false)]
    public List<string> Functions { get; set; }

    [DataMember(Name = "transactions", EmitDefaultValue = false)]
    public List<string> Transactions { get; set; }

    [DataMember(Name = "waitFor", EmitDefaultValue = false)]
    public List<string> WaitFor { get; set; }

    [DataMember(Name = "setOptions", EmitDefaultValue = false)]
    public List<string> SetOptions { get; set; }

    [DataMember(Name = "cte", EmitDefaultValue = false)]
    public List<string> Cte { get; set; }

    [DataMember(Name = "keywords", EmitDefaultValue = false)]
    public List<string> Keywords { get; set; }

    [DataMember(Name = "custom", EmitDefaultValue = false)]
    public List<string> Custom { get; set; }

    /// <summary>
    /// Explicit typed keyword items list: [{"keyword": "SELECT", "type": "StatementStart"}, ...]
    /// </summary>
    [DataMember(Name = "typedKeywords", EmitDefaultValue = false)]
    public List<TypedSqlKeyword> TypedKeywords { get; set; }

    public KeywordsConfiguration()
    {
      StatementStart = new List<string>();
      QueryClauses = new List<string>();
      WhereExpressions = new List<string>();
      JoinConditions = new List<string>();
      OrderByExpressions = new List<string>();
      SelectExpressions = new List<string>();
      InsertInto = new List<string>();
      InsertValues = new List<string>();
      UpdateSet = new List<string>();
      UpdateClauses = new List<string>();
      DeleteClauses = new List<string>();
      MergeInto = new List<string>();
      MergeUsing = new List<string>();
      MergeWhen = new List<string>();
      MergeActions = new List<string>();
      CreateObjects = new List<string>();
      AlterObjects = new List<string>();
      AlterTableActions = new List<string>();
      DropObjects = new List<string>();
      DataTypes = new List<string>();
      Functions = new List<string>();
      Transactions = new List<string>();
      WaitFor = new List<string>();
      SetOptions = new List<string>();
      Cte = new List<string>();
      Keywords = new List<string>();
      Custom = new List<string>();
      TypedKeywords = new List<TypedSqlKeyword>();
    }

    /// <summary>
    /// Returns the keywords corresponding to a specific SqlCompletionContextKind.
    /// </summary>
    public IEnumerable<string> GetKeywordsForKind(SqlCompletionContextKind kind)
    {
      var items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      void AddList(List<string>? list)
      {
        if (list == null) return;
        foreach (var item in list)
        {
          if (!string.IsNullOrWhiteSpace(item))
            items.Add(item.Trim());
        }
      }

      switch (kind)
      {
        case SqlCompletionContextKind.StatementStart:
          AddList(StatementStart);
          break;

        case SqlCompletionContextKind.QueryClause:
          AddList(QueryClauses);
          break;

        case SqlCompletionContextKind.WhereClause:
        case SqlCompletionContextKind.WhereExpression:
          AddList(WhereExpressions);
          break;

        case SqlCompletionContextKind.JoinCondition:
          AddList(JoinConditions);
          break;

        case SqlCompletionContextKind.OrderByExpression:
          AddList(OrderByExpressions);
          break;

        case SqlCompletionContextKind.SelectExpression:
        case SqlCompletionContextKind.SelectClause:
          AddList(SelectExpressions);
          break;

        case SqlCompletionContextKind.InsertInto:
          AddList(InsertInto);
          break;

        case SqlCompletionContextKind.InsertValues:
          AddList(InsertValues);
          break;

        case SqlCompletionContextKind.UpdateSet:
          AddList(UpdateSet);
          break;

        case SqlCompletionContextKind.UpdateFrom:
          AddList(UpdateClauses);
          break;

        case SqlCompletionContextKind.DeleteFrom:
        case SqlCompletionContextKind.DeleteWhere:
          AddList(DeleteClauses);
          break;

        case SqlCompletionContextKind.MergeInto:
          AddList(MergeInto);
          break;

        case SqlCompletionContextKind.MergeUsing:
          AddList(MergeUsing);
          break;

        case SqlCompletionContextKind.MergeOn:
          items.Add("ON");
          break;

        case SqlCompletionContextKind.MergeWhen:
          AddList(MergeWhen);
          break;

        case SqlCompletionContextKind.MergeAction:
          AddList(MergeActions);
          break;

        case SqlCompletionContextKind.CreateStatement:
          AddList(CreateObjects);
          break;

        case SqlCompletionContextKind.AlterStatement:
          AddList(AlterObjects);
          break;

        case SqlCompletionContextKind.AlterTableAction:
          AddList(AlterTableActions);
          break;

        case SqlCompletionContextKind.DropStatement:
          AddList(DropObjects);
          break;

        case SqlCompletionContextKind.CreateTableColumnDefinition:
        case SqlCompletionContextKind.DeclareVariable:
          AddList(DataTypes);
          break;

        case SqlCompletionContextKind.TransactionStatement:
          AddList(Transactions);
          break;

        case SqlCompletionContextKind.WaitForStatement:
          AddList(WaitFor);
          break;

        case SqlCompletionContextKind.SetVariable:
          AddList(SetOptions);
          break;

        case SqlCompletionContextKind.CteStart:
        case SqlCompletionContextKind.CteName:
          AddList(Cte);
          break;

        case SqlCompletionContextKind.TableSource:
        case SqlCompletionContextKind.InsertTable:
        case SqlCompletionContextKind.UpdateTable:
        case SqlCompletionContextKind.DeleteTable:
        case SqlCompletionContextKind.MergeTarget:
        case SqlCompletionContextKind.MergeSource:
        case SqlCompletionContextKind.CreateObjectName:
        case SqlCompletionContextKind.DropObject:
        case SqlCompletionContextKind.ExecuteObject:
        case SqlCompletionContextKind.ColumnReference:
        case SqlCompletionContextKind.UseDatabase:
        case SqlCompletionContextKind.Unknown:
          return Array.Empty<string>();

        default:
          return Array.Empty<string>();
      }

      // Also incorporate any items declared in TypedKeywords matching this kind or category
      if (TypedKeywords != null && TypedKeywords.Count > 0)
      {
        string kindName = kind.ToString();
        foreach (var tk in TypedKeywords)
        {
          if (string.IsNullOrWhiteSpace(tk.Keyword) || string.IsNullOrWhiteSpace(tk.Type))
            continue;

          if (string.Equals(tk.Type, kindName, StringComparison.OrdinalIgnoreCase) ||
              IsMatchingCategory(tk.Type, kind))
          {
            items.Add(tk.Keyword.Trim());
          }
        }
      }

      return items;
    }

    private static bool IsMatchingCategory(string typeName, SqlCompletionContextKind kind)
    {
      if (string.Equals(typeName, "StatementStart", StringComparison.OrdinalIgnoreCase) && kind == SqlCompletionContextKind.StatementStart) return true;
      if (string.Equals(typeName, "QueryClause", StringComparison.OrdinalIgnoreCase) && kind == SqlCompletionContextKind.QueryClause) return true;
      if (string.Equals(typeName, "WhereExpression", StringComparison.OrdinalIgnoreCase) && (kind == SqlCompletionContextKind.WhereExpression || kind == SqlCompletionContextKind.WhereClause)) return true;
      if (string.Equals(typeName, "DataType", StringComparison.OrdinalIgnoreCase) && (kind == SqlCompletionContextKind.CreateTableColumnDefinition || kind == SqlCompletionContextKind.DeclareVariable)) return true;
      if (string.Equals(typeName, "Function", StringComparison.OrdinalIgnoreCase) && (kind == SqlCompletionContextKind.SelectExpression || kind == SqlCompletionContextKind.WhereExpression)) return true;
      return false;
    }

    /// <summary>
    /// Gets all unique keywords across all categories for global search, parsing, or casing.
    /// </summary>
    public IEnumerable<string> GetAllItems()
    {
      var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      void AddList(List<string>? list)
      {
        if (list == null) return;
        foreach (var item in list)
        {
          if (!string.IsNullOrWhiteSpace(item))
            set.Add(item.Trim());
        }
      }

      AddList(StatementStart);
      AddList(QueryClauses);
      AddList(WhereExpressions);
      AddList(JoinConditions);
      AddList(OrderByExpressions);
      AddList(SelectExpressions);
      AddList(InsertInto);
      AddList(InsertValues);
      AddList(UpdateSet);
      AddList(UpdateClauses);
      AddList(DeleteClauses);
      AddList(MergeInto);
      AddList(MergeUsing);
      AddList(MergeWhen);
      AddList(MergeActions);
      AddList(CreateObjects);
      AddList(AlterObjects);
      AddList(AlterTableActions);
      AddList(DropObjects);
      AddList(DataTypes);
      AddList(Functions);
      AddList(Transactions);
      AddList(WaitFor);
      AddList(SetOptions);
      AddList(Cte);
      AddList(Keywords);
      AddList(Custom);

      if (TypedKeywords != null)
      {
        foreach (var tk in TypedKeywords)
        {
          if (!string.IsNullOrWhiteSpace(tk.Keyword))
            set.Add(tk.Keyword.Trim());
        }
      }

      return set;
    }
  }

  public static class KeywordsLoader
  {
    private static KeywordsConfiguration _current = LoadDefault();
    private static readonly object _lock = new object();

    public static KeywordsConfiguration Current
    {
      get
      {
        lock (_lock)
        {
          return _current;
        }
      }
      set
      {
        lock (_lock)
        {
          _current = value ?? new KeywordsConfiguration();
        }
      }
    }

    public static KeywordsConfiguration LoadDefault()
    {
      using (var stream = SqlKeywords.GetDefaultKeywordsJsonStream())
      {
        if (stream != null)
        {
          return LoadFromStream(stream);
        }
      }
      return new KeywordsConfiguration();
    }

    public static KeywordsConfiguration LoadFromFile(string filePath)
    {
      if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
      {
        return new KeywordsConfiguration();
      }

      string content = File.ReadAllText(filePath, Encoding.UTF8);
      return LoadFromJson(content);
    }

    public static KeywordsConfiguration LoadFromStream(Stream stream)
    {
      if (stream == null)
      {
        return new KeywordsConfiguration();
      }

      using (var reader = new StreamReader(stream, Encoding.UTF8))
      {
        string json = reader.ReadToEnd();
        return LoadFromJson(json);
      }
    }

    public static KeywordsConfiguration LoadFromJson(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
      {
        return new KeywordsConfiguration();
      }

      byte[] bytes = Encoding.UTF8.GetBytes(json.Trim());

      // Try 1: Deserialize as categorized KeywordsConfiguration object
      try
      {
        using (var stream = new MemoryStream(bytes))
        {
          var serializer = new DataContractJsonSerializer(typeof(KeywordsConfiguration));
          var config = serializer.ReadObject(stream) as KeywordsConfiguration;
          if (config != null)
          {
            return config;
          }
        }
      }
      catch
      {
        // Fallback to array format
      }

      // Try 2: Deserialize as plain string array: ["SELECT", "FROM", "MY_FUNC"]
      try
      {
        using (var stream = new MemoryStream(bytes))
        {
          var arraySerializer = new DataContractJsonSerializer(typeof(List<string>));
          var list = arraySerializer.ReadObject(stream) as List<string>;
          if (list != null)
          {
            var config = new KeywordsConfiguration();
            config.Custom.AddRange(list);
            return config;
          }
        }
      }
      catch
      {
      }

      return new KeywordsConfiguration();
    }
  }
}
