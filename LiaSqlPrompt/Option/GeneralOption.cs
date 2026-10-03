using LiaSqlPrompt.Snippet;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace LiaSqlPrompt.Option
{
  public sealed class GeneralOption : DialogPage
  {
    [Category("Paths")]
    [DisplayName("Root Folder")]
    [Description("Root directory for LiaSqlPrompt configuration files (default: C:\\LiaSqlPrompt).")]
    public string RootFolder { get; set; } = @"C:\LiaSqlPrompt";

    [Category("Keywords")]
    [DisplayName("Capitalize Keywords")]
    [Description("When enabled, SQL keyword completions are inserted in UPPERCASE; otherwise in lowercase.")]
    public bool CapitalizeKeywords { get; set; } = true;

    [Category("Keywords")]
    [DisplayName("Keywords JSON File Path")]
    [Description("Optional custom path for keywords JSON. If empty or relative, it resolves inside the Root Folder as keywords.json.")]
    public string CustomKeywordsFilePath { get; set; } = "keywords.json";

    protected override void OnApply(PageApplyEventArgs e)
    {
      base.OnApply(e);

      KeywordsManager.Reload();
    }
  }
}