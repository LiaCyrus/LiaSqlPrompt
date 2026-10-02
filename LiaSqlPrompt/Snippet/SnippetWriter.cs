using System;
using System.IO;
using System.Xml.Linq;

namespace LiaSqlPrompt.Snippet
{
  public sealed class SnippetWriter
  {
    public void Save(string folderPath, SnippetDefinition snippet, bool overwrite = false)
    {
      if (string.IsNullOrWhiteSpace(folderPath))
        throw new ArgumentException("Folder path cannot be empty.", nameof(folderPath));

      if (snippet == null)
        throw new ArgumentNullException(nameof(snippet));

      if (string.IsNullOrWhiteSpace(snippet.Shortcut))
        throw new ArgumentException("Snippet shortcut cannot be empty.");

      if (!Directory.Exists(folderPath))
      {
        Directory.CreateDirectory(folderPath);
      }

      string safeShortcut = GetSafeFileName(snippet.Shortcut.Trim());
      string filePath = Path.Combine(folderPath, $"{safeShortcut}.snippet.xml");

      if (File.Exists(filePath) && !overwrite)
      {
        throw new InvalidOperationException($"Snippet file '{Path.GetFileName(filePath)}' already exists.");
      }

      var document = new XDocument(
          new XDeclaration("1.0", "utf-8", "yes"),
          new XElement("Snippet",
              new XElement("Header",
                  new XElement("Title", snippet.Title ?? string.Empty),
                  new XElement("Shortcut", snippet.Shortcut.Trim()),
                  new XElement("Description", snippet.Description ?? string.Empty)
              ),
              new XElement("Code",
                  new XCData("\n" + (snippet.Code ?? string.Empty).Replace("\r\n", "\n").Trim('\r', '\n') + "\n")
              )
          )
      );

      document.Save(filePath);
    }

    public void Delete(string filePath)
    {
      if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentException("File path cannot be empty.", nameof(filePath));

      if (File.Exists(filePath))
      {
        File.Delete(filePath);
      }
    }

    private static string GetSafeFileName(string name)
    {
      foreach (char c in Path.GetInvalidFileNameChars())
      {
        name = name.Replace(c, '_');
      }
      return name;
    }
  }
}
