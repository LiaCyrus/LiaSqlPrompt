using LiaSqlPrompt.Snippet;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace LiaSqlPrompt.Option
{
  public sealed class SnippetOption : DialogPage
  {
    [Category("General")]
    [DisplayName("Snippet Folder")]
    [Description("Folder containing .snippet.xml files.")]
    public string SnippetFolder { get; set; } = @"C:\LiaSqlPrompt\Snippet";

    protected override void OnApply(PageApplyEventArgs e)
    {
      base.OnApply(e);

      SnippetRepository.Instance.Reload();
    }
  }
}
