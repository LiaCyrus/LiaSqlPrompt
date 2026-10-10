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

    [Import]
    internal Microsoft.VisualStudio.Language.Intellisense.ICompletionBroker CompletionBroker { get; set; } = null!;

    public static SqlCompletionController? ActiveController { get; private set; }

    public void TextViewCreated(IWpfTextView textView)
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      var controller = new SqlCompletionController(textView, CompletionBroker);

      textView.GotAggregateFocus += (s, e) => ActiveController = controller;
      textView.Closed += (s, e) =>
      {
        if (ActiveController == controller)
          ActiveController = null;
      };

      if (textView.HasAggregateFocus)
      {
        ActiveController = controller;
      }

      IVsTextView? viewAdapter = EditorAdaptersFactory.GetViewAdapter(textView);

      if (viewAdapter == null) return;

      var filter = new SqlCommandFilter(controller);

      IOleCommandTarget next;

      viewAdapter.AddCommandFilter(filter, out next);

      filter.SetNextTarget(next);
    }
  }
}