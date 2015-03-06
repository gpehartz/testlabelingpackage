using System;
using System.Collections.Generic;
using System.Linq;
using ICETeam.TestPackage.Domain;
using Microsoft.Practices.Prism.Mvvm;

namespace ICETeam.TestPackage.UI
{
    public class TestPackageControlViewModel : BindableBase
    {
        private NodeWithLabels _selectedItem;
        private List<NodeWithLabels> _itemsToShow;

        public event EventHandler<SelectedItemChangedEventArgs> SelectedItemChangedEvent;

        public List<NodeWithLabels> ItemsToShow
        {
            get { return _itemsToShow; }
            set { SetProperty(ref _itemsToShow, value); }
        }

        public NodeWithLabels SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetProperty(ref _selectedItem, value);
                OnSelectedItemChanged();
            }
        }

        public TestPackageControlViewModel()
        {
            ItemsToShow = new List<NodeWithLabels>();             
        }

        public void RefreshData(IEnumerable<NodeWithLabels> parsedData)
        {
            SelectedItem = null;
            ItemsToShow = parsedData.ToList();
        }

        private void OnSelectedItemChanged()
        {
            if (_selectedItem == null) return;

            SelectedItemChangedEvent?.Invoke(this, new SelectedItemChangedEventArgs
            {
                SelectedItem = _selectedItem
            });
        }
    }

    public class SelectedItemChangedEventArgs : EventArgs
    {
        public NodeWithLabels SelectedItem { get; set; }
    }
}
