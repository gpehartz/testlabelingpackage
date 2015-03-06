using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace TestPackage.UI
{
    [Guid("f7163ab8-6330-4457-9b39-4c778ffaf17d")]
    public class TestPackageToolWindow : ToolWindowPane
    {
        private TestPackageControl _control;

        public TestPackageToolWindow() : base(null)
        {
            // Set the window title reading it from the resources.
            this.Caption = "TEST";
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

            _control = new TestPackageControl();

            base.Content = _control;
        }
    }
}
