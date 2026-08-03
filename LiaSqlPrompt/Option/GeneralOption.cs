using LiaSqlPrompt.Snippet;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace LiaSqlPrompt.Option
{
    public sealed class GeneralOption : DialogPage
    {
        [Category("Snippet")]
        [DisplayName("Snippet Folder")]
        [Description("Folder containing .snippet files.")]
        public string SnippetFolder { get; set; } = @"C:\LiaSqlPrompt\Snippet";

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            SnippetRepository.Instance.Reload();
        }
    }
}