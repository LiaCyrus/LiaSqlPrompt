using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace LiaSqlPrompt.Option
{
  [ClassInterface(ClassInterfaceType.AutoDual)]
  [Guid("5e8e89f6-17b2-4d2b-8a8f-f1bbd21051fa")]
  public class GeneralOptionPage : UIElementDialogPage
  {
    private GeneralOptionControl? _control;

    protected override UIElement Child
    {
      get
      {
        return _control ??= new GeneralOptionControl();
      }
    }

    protected override void OnActivate(CancelEventArgs e)
    {
      base.OnActivate(e);
      _control?.LoadSettings();
    }

    protected override void OnApply(PageApplyEventArgs e)
    {
      base.OnApply(e);
      _control?.SaveSettings();
    }
  }
}
