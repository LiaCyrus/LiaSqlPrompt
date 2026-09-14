using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace LiaSqlPrompt.Editor
{
  [Export(typeof(IWpfTextViewCreationListener))]
  [ContentType("text")]
  [TextViewRole(PredefinedTextViewRoles.Document)]
  public sealed class SqlEditorListener : IWpfTextViewCreationListener
  {
    [Import]
    internal IVsEditorAdaptersFactoryService EditorAdaptersFactory { get; set; } = null!;

    public void TextViewCreated(IWpfTextView textView)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      var controller = new SqlCompletionController(textView);

      IVsTextView? viewAdapter = EditorAdaptersFactory.GetViewAdapter(textView);

      if (viewAdapter == null) return;

      var filter = new SqlCommandFilter(controller);

      IOleCommandTarget next;

      viewAdapter.AddCommandFilter(filter, out next);

      filter.SetNextTarget(next);
    }
  }
}