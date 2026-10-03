using LiaSqlPrompt.Option;
using SqlEngine;
using System;
using System.IO;

namespace LiaSqlPrompt
{
  public static class KeywordsManager
  {
    private static FileSystemWatcher? _watcher;
    private static System.Threading.Timer? _debounceTimer;
    private static readonly object _syncRoot = new object();
    private static string? _watchedFilePath;

    public static void Initialize()
    {
      Reload();
    }

    public static void Reload()
    {
      lock (_syncRoot)
      {
        string rootFolder = OptionService.General?.RootFolder?.Trim() ?? @"C:\LiaSqlPrompt";
        if (string.IsNullOrWhiteSpace(rootFolder))
        {
          rootFolder = @"C:\LiaSqlPrompt";
        }

        string customFile = OptionService.General?.CustomKeywordsFilePath?.Trim() ?? "keywords.json";
        if (string.IsNullOrWhiteSpace(customFile))
        {
          customFile = "keywords.json";
        }

        string filePath = Path.IsPathRooted(customFile)
            ? customFile
            : Path.Combine(rootFolder, customFile);

        try
        {
          // 1. If file does not exist, check/create directory and generate it from embedded default JSON
          if (!File.Exists(filePath))
          {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
              Directory.CreateDirectory(directory);
            }

            string defaultJson = SqlKeywords.GetDefaultKeywordsJsonString();
            if (!string.IsNullOrWhiteSpace(defaultJson))
            {
              File.WriteAllText(filePath, defaultJson, System.Text.Encoding.UTF8);
              Logger.Log($"Created default keywords file at: {filePath}");
            }
          }

          // 2. Load what is there
          if (File.Exists(filePath))
          {
            var config = KeywordsLoader.LoadFromFile(filePath);
            SqlKeywords.SetKeywords(config.GetAllItems());
            Logger.Log($"Loaded keywords/functions from {filePath}");
          }
          else
          {
            SqlKeywords.ResetToDefault();
            Logger.Log($"Keywords file not accessible at {filePath}. Using built-in defaults.");
          }
        }
        catch (Exception ex)
        {
          SqlKeywords.ResetToDefault();
          Logger.Log($"Error processing keywords file '{filePath}': {ex.Message}");
        }

        SetupFileWatcher(filePath);
      }
    }

    private static void SetupFileWatcher(string filePath)
    {
      try
      {
        if (string.Equals(_watchedFilePath, filePath, StringComparison.OrdinalIgnoreCase) && _watcher != null)
        {
          return;
        }

        _watcher?.Dispose();
        _watcher = null;
        _watchedFilePath = filePath;

        string directory = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileName(filePath);

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
        {
          return;
        }

        if (!Directory.Exists(directory))
        {
          try
          {
            Directory.CreateDirectory(directory);
          }
          catch
          {
            return;
          }
        }

        _watcher = new FileSystemWatcher(directory, fileName)
        {
          NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime,
          EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
      }
      catch (Exception ex)
      {
        Logger.Log($"Could not setup FileSystemWatcher for {filePath}: {ex.Message}");
      }
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
      lock (_syncRoot)
      {
        if (_debounceTimer == null)
        {
          _debounceTimer = new System.Threading.Timer(_ => Reload(), null, 300, System.Threading.Timeout.Infinite);
        }
        else
        {
          _debounceTimer.Change(300, System.Threading.Timeout.Infinite);
        }
      }
    }
  }
}
