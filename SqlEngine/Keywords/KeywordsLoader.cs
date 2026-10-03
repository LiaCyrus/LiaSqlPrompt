using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SqlEngine
{
  [DataContract]
  public sealed class KeywordsConfiguration
  {
    [DataMember(Name = "keywords", EmitDefaultValue = false)]
    public List<string> Keywords { get; set; }

    [DataMember(Name = "functions", EmitDefaultValue = false)]
    public List<string> Functions { get; set; }

    [DataMember(Name = "dataTypes", EmitDefaultValue = false)]
    public List<string> DataTypes { get; set; }

    [DataMember(Name = "custom", EmitDefaultValue = false)]
    public List<string> Custom { get; set; }

    public KeywordsConfiguration()
    {
      Keywords = new List<string>();
      Functions = new List<string>();
      DataTypes = new List<string>();
      Custom = new List<string>();
    }

    public IEnumerable<string> GetAllItems()
    {
      var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      if (Keywords != null)
      {
        foreach (var item in Keywords)
        {
          if (!string.IsNullOrWhiteSpace(item))
            set.Add(item.Trim());
        }
      }

      if (Functions != null)
      {
        foreach (var item in Functions)
        {
          if (!string.IsNullOrWhiteSpace(item))
            set.Add(item.Trim());
        }
      }

      if (DataTypes != null)
      {
        foreach (var item in DataTypes)
        {
          if (!string.IsNullOrWhiteSpace(item))
            set.Add(item.Trim());
        }
      }

      if (Custom != null)
      {
        foreach (var item in Custom)
        {
          if (!string.IsNullOrWhiteSpace(item))
            set.Add(item.Trim());
        }
      }

      return set;
    }
  }

  public static class KeywordsLoader
  {
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
