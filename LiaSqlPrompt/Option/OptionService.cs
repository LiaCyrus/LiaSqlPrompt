using Microsoft.VisualStudio.Shell;

namespace LiaSqlPrompt.Option
{
  public static class OptionService
  {
    private static GeneralOption? _general;
    private static SnippetOption? _snippet;

    public static void Initialize(AsyncPackage package)
    {
      _general = (GeneralOption)package.GetDialogPage(typeof(GeneralOption));
      _snippet = (SnippetOption)package.GetDialogPage(typeof(SnippetOption));
    }

    public static GeneralOption General
    {
      get
      {
        return _general!;
      }
    }

    public static SnippetOption Snippet
    {
      get
      {
        return _snippet!;
      }
    }
  }
}