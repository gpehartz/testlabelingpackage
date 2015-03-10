using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace ICETeam.TestPackage.UI
{
    [Guid("3792CE65-706B-45C2-8F30-AA684A71CA36")]
    public class TestPackageNavigationToolWindow : ToolWindowPane
    {
        public TestPackageNavigationToolWindow() : base(null)
        {
            // Set the window title reading it from the resources.
            this.Caption = "NAVIGATION";
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

            var control = new TestPackageNavigationControl();

            base.Content = control;
        }
    }
}
