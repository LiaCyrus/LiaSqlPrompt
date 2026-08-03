using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace LiaSqlPrompt.Editor
{
    public sealed class SqlCommandFilter : IOleCommandTarget
    {
        private IOleCommandTarget? _next;
        private readonly SqlCompletionController _controller;

        public SqlCommandFilter(SqlCompletionController controller)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _controller = controller;
        }

        public void SetNextTarget(IOleCommandTarget next)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _next = next;
        }

        public int QueryStatus(ref System.Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, System.IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return _next!.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref System.Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, System.IntPtr pvaIn, System.IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                bool handled = false;

                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.TYPECHAR:
                        if (pvaIn != System.IntPtr.Zero)
                        {
                            try
                            {
                                char ch = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

                                if (ch == ' ')
                                    handled = _controller.HandleSpace();
                                else
                                    handled = _controller.HandleTypeChar();
                            }
                            catch
                            {
                                handled = _controller.HandleTypeChar();
                            }
                        }

                        break;
                    case VSConstants.VSStd2KCmdID.BACKSPACE:
                        handled = _controller.HandleBackspace();
                        break;
                    case VSConstants.VSStd2KCmdID.TAB:
                        handled = _controller.HandleTab();
                        break;
                    case VSConstants.VSStd2KCmdID.RETURN:
                        handled = _controller.HandleReturn();
                        break;
                    case VSConstants.VSStd2KCmdID.CANCEL:
                        handled = _controller.HandleEscape();
                        break;
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        handled = _controller.HandleCompleteWord();
                        break;
                    case VSConstants.VSStd2KCmdID.UP:
                        handled = _controller.HandleUp();
                        break;
                    case VSConstants.VSStd2KCmdID.DOWN:
                        handled = _controller.HandleDown();
                        break;
                }

                if (handled)
                    return VSConstants.S_OK;
            }

            return _next!.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
    }
}