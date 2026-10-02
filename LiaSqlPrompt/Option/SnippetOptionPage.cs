using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace LiaSqlPrompt.Option
{
  [ClassInterface(ClassInterfaceType.AutoDual)]
  [Guid("3a35b1d4-897f-47cf-8987-a2ef39df8b41")]
  public class SnippetOptionPage : UIElementDialogPage
  {
    private SnippetOptionControl? _control;

    protected override UIElement Child
    {
      get
      {
        return _control ??= new SnippetOptionControl();
      }
    }
  }
}
