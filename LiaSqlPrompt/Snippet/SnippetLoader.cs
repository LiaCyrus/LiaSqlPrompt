using System;
using System.Xml.Linq;

namespace LiaSqlPrompt.Snippet
{
    public sealed class SnippetLoader
    {
        public SnippetDefinition Load(string filePath)
        {
            var document = XDocument.Load(filePath);

            var root = document.Element("Snippet");

            if (root == null) throw new InvalidOperationException("Invalid snippet file.");

            var header = root.Element("Header");

            if (header == null) throw new InvalidOperationException("Missing Header section.");

            string code = root.Element("Code")?.Value ?? "";
            code = NormalizeCode(code);

            return new SnippetDefinition(
                header.Element("Title")?.Value ?? "",
                header.Element("Shortcut")?.Value ?? "",
                header.Element("Description")?.Value ?? "",
                code
            );
        }

        private static string NormalizeCode(string code)
        {
            code = code.Replace("\r\n", "\n");
            code = code.Replace('\r', '\n');

            if (code.StartsWith("\n"))
                code = code.Substring(1);

            if (code.EndsWith("\n"))
                code = code.Substring(0, code.Length - 1);

            code = code.Replace("\n", "\r\n");

            return code;
        }
    }
}