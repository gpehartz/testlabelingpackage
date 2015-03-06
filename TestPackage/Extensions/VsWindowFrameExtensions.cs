using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace ICETeam.TestPackage.Extensions
{
    internal static class VsWindowFrameExtensions
    {
        internal static IWpfTextView ToWpfTextView(this IVsWindowFrame vsWindowFrame)
        {
            var wpfTextView = (IWpfTextView)null;
            var textView = VsShellUtilities.GetTextView(vsWindowFrame);
            if (textView != null)
            {
                var riidKey = DefGuidList.guidIWpfTextViewHost;
                object pvtData;
                if (((IVsUserData)textView).GetData(ref riidKey, out pvtData) == 0 && pvtData != null)
                    wpfTextView = ((IWpfTextViewHost)pvtData).TextView;
            }
            return wpfTextView;
        }
    }
}
