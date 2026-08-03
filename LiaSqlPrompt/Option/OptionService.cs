using Microsoft.VisualStudio.Shell;

namespace LiaSqlPrompt.Option
{
    public static class OptionService
    {
        private static GeneralOption? _general;

        public static void Initialize(AsyncPackage package)
        {
            _general = (GeneralOption)package.GetDialogPage(typeof(GeneralOption));
        }

        public static GeneralOption General
        {
            get
            {
                return _general!;
            }
        }
    }
}