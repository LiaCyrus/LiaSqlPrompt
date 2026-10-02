using LiaSqlPrompt.Snippet;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace LiaSqlPrompt.Option
{
  public sealed class GeneralOption : DialogPage
  {
    [Category("Keywords")]
    [DisplayName("Capitalize Keywords")]
    [Description("When enabled, SQL keyword completions are inserted in UPPERCASE; otherwise in lowercase.")]
    public bool CapitalizeKeywords { get; set; } = true;
  }
}