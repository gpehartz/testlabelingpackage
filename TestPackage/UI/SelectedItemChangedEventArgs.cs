using System;
using ICETeam.TestPackage.Domain;

namespace ICETeam.TestPackage.UI
{
    public class SelectedItemChangedEventArgs : EventArgs
    {
        public NodeWithLabels SelectedItem { get; set; }
    }
}