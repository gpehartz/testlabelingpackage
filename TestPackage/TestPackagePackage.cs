using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ICETeam.TestPackage.Extensions;
using ICETeam.TestPackage.ParseLogic;
using ICETeam.TestPackage.UI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using TestPackage.UI;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace ICETeam.TestPackage
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideToolWindow(typeof(TestPackageToolWindow))]
    [Guid(GuidList.guidTestPackagePkgString)]
    public sealed class TestPackagePackage : Package, IVsRunningDocTableEvents, IDisposable
    {
        private VisualStudioWorkspace _vsWorkspace;
        private Parser _parseLogic;
        private TestPackageControlViewModel _testPackageControlViewModel;
        private uint _runningDocumentTableCookie;
        private static IServiceProvider _globalServiceProvider;
        private IVsRunningDocumentTable _runningDocumentTable;
        private IWpfTextView _activeWpfTextView;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public TestPackagePackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            InitializeRunningDocumentTable();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                var menuCommandId = new CommandID(GuidList.guidTestPackageCmdSet, (int) PkgCmdIDList.cmdidNavigate);
                var menuItem = new MenuCommand(MenuItemCallback, menuCommandId);
                mcs.AddCommand(menuItem);

                var toolwndCommandId = new CommandID(GuidList.guidTestPackageCmdSet, (int)PkgCmdIDList.cmdidToolWindow);
                var menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandId);
                mcs.AddCommand(menuToolWin);
            }

            var componentModel = (IComponentModel)this.GetService(typeof(SComponentModel));
            _vsWorkspace = componentModel.GetService<VisualStudioWorkspace>();
        }

        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = this.FindToolWindow(typeof(TestPackageToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Not Found");
            }

            if(_parseLogic != null)
            {
                _testPackageControlViewModel = (TestPackageControlViewModel) ((TestPackageControl) window.Content).DataContext;
                _testPackageControlViewModel.RefreshData(_parseLogic.ParsedData);

                _testPackageControlViewModel.SelectedItemChangedEvent += TestPackageControlViewModelOnSelectedItemChangedEvent;
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void TestPackageControlViewModelOnSelectedItemChangedEvent(object sender, SelectedItemChangedEventArgs selectedItemChangedEventArgs)
        {
            _vsWorkspace.OpenDocument(selectedItemChangedEventArgs.SelectedItem.ContainingDocumentId);

            NavigateTo(selectedItemChangedEventArgs.SelectedItem.Node.Span);
        }

        private void OnWorkSpaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            var changedDocument = e.NewSolution.GetDocument(e.DocumentId);
            if (changedDocument == null) return;
            
            _parseLogic.ReparseItemsForDocument(_vsWorkspace, changedDocument);

            _testPackageControlViewModel?.RefreshData(_parseLogic.ParsedData);
        }

        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            _parseLogic = new Parser();
            _parseLogic.ParseWorkSpace(_vsWorkspace);

            //var sb = new StringBuilder();

            //sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()));
            //sb.AppendFormat("Parsed item count: {0}", _parseLogic.ParsedData.Count()).AppendLine();

            //foreach (var parsedData in _parseLogic.ParsedData)
            //{
            //    var node = parsedData.Node;
            //    var document = _vsWorkspace.CurrentSolution.GetDocument(parsedData.ContainingDocumentId);

            //    sb.AppendFormat("Found item in {0} on position {1} with content {2}", document.FilePath, node.SpanStart, node).AppendLine();
            //}

            //// Show a Message Box to prove we were here
            //IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            //Guid clsid = Guid.Empty;
            //int result;
            //ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(0,
            //    ref clsid,
            //    "TestPackage",
            //    sb.ToString(),
            //    string.Empty,
            //    0,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    0, // false
            //    out result));

            _vsWorkspace.WorkspaceChanged += OnWorkSpaceChanged;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            var wpfTextView = pFrame.ToWpfTextView();
            if (wpfTextView != null)
            {
                Clear();

                var contentType = wpfTextView.TextBuffer.ContentType;
                if (contentType.IsOfType("CSharp"))
                {
                    _activeWpfTextView = wpfTextView;
                }
            }

            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public void Dispose()
        {
            if ((int)_runningDocumentTableCookie == 0) return;

            _runningDocumentTable.UnadviseRunningDocTableEvents(_runningDocumentTableCookie);
            _runningDocumentTableCookie = 0U;
        }

        private void Clear()
        {
            if (_activeWpfTextView != null)
            {
                _activeWpfTextView = null;
            }
        }

        private void NavigateTo(TextSpan span)
        {
            var snapshotSpan = span.ToSnapshotSpan(_activeWpfTextView.TextBuffer.CurrentSnapshot);

            _activeWpfTextView.Selection.Select(snapshotSpan, false);
            _activeWpfTextView.ViewScroller.EnsureSpanVisible(snapshotSpan);
        }

        private void InitializeRunningDocumentTable()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering InitializeRunningDocumentTable() of: {0}", this.ToString()));

            if (RunningDocumentTable == null)
                return;
            RunningDocumentTable.AdviseRunningDocTableEvents(this, out _runningDocumentTableCookie);
        }

        private static IServiceProvider GlobalServiceProvider
        {
            get
            {
                if (_globalServiceProvider == null)
                    _globalServiceProvider = (IServiceProvider)Package.GetGlobalService(typeof(IServiceProvider));
                return _globalServiceProvider;
            }
        }

        private IVsRunningDocumentTable RunningDocumentTable
        {
            get
            {
                if (_runningDocumentTable == null)
                    _runningDocumentTable =
                        GetService<IVsRunningDocumentTable, SVsRunningDocumentTable>(GlobalServiceProvider);
                return _runningDocumentTable;
            }
        }

        private static TServiceInterface GetService<TServiceInterface, TService>(IServiceProvider serviceProvider)
            where TServiceInterface : class where TService : class
        {
            return (TServiceInterface)GetService(serviceProvider, typeof(TService).GUID, false);
        }

        private static object GetService(IServiceProvider serviceProvider, Guid guidService, bool unique)
        {
            Guid riid = (Guid)VSConstants.IID_IUnknown;
            IntPtr ppvObject = IntPtr.Zero;
            object obj = (object)null;
            if (serviceProvider.QueryService(ref guidService, ref riid, out ppvObject) == 0)
            {
                if (ppvObject != IntPtr.Zero)
                {
                    try
                    {
                        obj = !unique ? Marshal.GetObjectForIUnknown(ppvObject) : Marshal.GetUniqueObjectForIUnknown(ppvObject);
                    }
                    finally
                    {
                        Marshal.Release(ppvObject);
                    }
                }
            }
            return obj;
        }
    }
}
