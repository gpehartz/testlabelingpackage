using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICETeam.TestPackage.UI;
using Microsoft.VisualStudio.LanguageServices;

namespace TestPackage.UI
{
    /// <summary>
    /// Interaction logic for TestPackageControl.xaml
    /// </summary>
    public partial class TestPackageControl : UserControl
    {
        public TestPackageControl()
        {
            InitializeComponent();

            DataContext = new TestPackageControlViewModel();
        }
    }
}
