using System;
using System.Collections.Generic;
using System.Linq;
using ICETeam.TestPackage.Domain;
using ICETeam.TestPackage.NavigationLogic;
using Microsoft.CodeAnalysis;
using Microsoft.Practices.Prism.Mvvm;

namespace ICETeam.TestPackage.UI
{
    public class TestPackageNavigationControlViewModel : BindableBase
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

        public TestPackageNavigationControlViewModel()
        {
            ItemsToShow = new List<NodeWithLabels>();
        }

        public void RefreshData(IEnumerable<NodeWithLabels> parsedData, DocumentId documentId, int position)
        {
            var labels = Navigation.GetNavigationTargets(parsedData, documentId, position);

            ItemsToShow = labels.ToList();
            SelectedItem = null;
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
}
