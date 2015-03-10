using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using ICETeam.TestPackage.Extensions;
using ICETeam.TestPackage.NavigationLogic;
using ICETeam.TestPackage.ParseLogic;
using ICETeam.TestPackage.UI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using UIElement = Microsoft.Internal.VisualStudio.PlatformUI.UIElement;

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
    [ProvideToolWindow(typeof(TestPackageNavigationToolWindow))]
    [Guid(GuidList.guidTestPackagePkgString)]
    public sealed class TestPackagePackage : Package, IVsRunningDocTableEvents, IDisposable
    {
        private VisualStudioWorkspace _vsWorkspace;
        private Parser _parseLogic;
        private TestPackageControlViewModel _testPackageControlViewModel;
        private TestPackageNavigationControlViewModel _testPackageNavigationControlViewModel;
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
                var menuCommandId = new CommandID(GuidList.guidTestPackageCmdSet, (int) PkgCmdIDList.cmdidParse);
                var menuParse = new MenuCommand(ParseMenuSelected, menuCommandId);
                mcs.AddCommand(menuParse);

                var toolwndCommandId = new CommandID(GuidList.guidTestPackageCmdSet, (int)PkgCmdIDList.cmdidToolWindow);
                var menuParseToolWindow = new MenuCommand(ParseToolWindowSelected, toolwndCommandId);
                mcs.AddCommand(menuParseToolWindow);

                var navigationwndCommandId = new CommandID(GuidList.guidNavigationCmdSet, (int)PkgCmdIDList.cmdidNavigate);
                var menuNavigate = new OleMenuCommand(NavigationToolWindowSelected, navigationwndCommandId);
                menuNavigate.BeforeQueryStatus += BeforeNavigateToolWindowSelected;
                mcs.AddCommand(menuNavigate);
            }

            var componentModel = (IComponentModel)this.GetService(typeof(SComponentModel));
            _vsWorkspace = componentModel.GetService<VisualStudioWorkspace>();
        }

        private void BeforeNavigateToolWindowSelected(object sender, EventArgs e)
        {
            if (_parseLogic?.ParsedData == null) return;

            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            menuCommand.Enabled = false;

            var currentSnapshot = GetAndCheckCurrentSnapshot();
            if (currentSnapshot == null) return;

            var caretPosition = _activeWpfTextView.Selection.Start;
            var documentId = _vsWorkspace.GetDocumentIdInCurrentContext(_activeWpfTextView.TextBuffer.AsTextContainer());

            var isNavigationAllowed = Navigation.IsNavigationAllowed(_parseLogic.ParsedData, documentId, caretPosition.Position);

            menuCommand.Enabled = isNavigationAllowed;
        }

        private ITextSnapshot GetAndCheckCurrentSnapshot()
        {
            if (_activeWpfTextView == null) return null;

            var currentSnapshot = _activeWpfTextView.TextBuffer.CurrentSnapshot;
            var contentType = currentSnapshot.ContentType;
            if (!contentType.IsOfType("CSharp")) return currentSnapshot;

            return currentSnapshot;
        }

        private void NavigationToolWindowSelected(object sender, EventArgs e)
        {
            var currentSnapshot = GetAndCheckCurrentSnapshot();
            if (currentSnapshot == null) return;

            var window = this.FindToolWindow(typeof(TestPackageNavigationToolWindow), 0, true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("Not Found");
            }

            var windowFrame = PlaceWindowToCaretPosition(window);

            if (_parseLogic != null)
            {
                var caretPosition = _activeWpfTextView.Selection.Start;
                var documentId = _vsWorkspace.GetDocumentIdInCurrentContext(_activeWpfTextView.TextBuffer.AsTextContainer());

                _testPackageNavigationControlViewModel = (TestPackageNavigationControlViewModel)((TestPackageNavigationControl)window.Content).DataContext;
                _testPackageNavigationControlViewModel.RefreshData(_parseLogic.ParsedData, documentId, caretPosition.Position);

                _testPackageNavigationControlViewModel.SelectedItemChangedEvent += TestPackageNavigationControlViewModelOnSelectedItemChangedEvent;
            }

            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private IVsWindowFrame PlaceWindowToCaretPosition(ToolWindowPane window)
        {
            var caretPos = _activeWpfTextView.Caret.Position.BufferPosition;
            var charBounds = _activeWpfTextView.GetTextViewLineContainingBufferPosition(caretPos).GetCharacterBounds(caretPos);
            var textBottom = charBounds.Bottom;
            var textX = charBounds.Right;

            var windowFrame = (IVsWindowFrame) window.Frame;

            var textViewOrigin = (_activeWpfTextView as Visual).PointToScreen(new Point(0, 0));

            var guid = default(Guid);
            var newLeft = textViewOrigin.X + textX - _activeWpfTextView.ViewportLeft;
            var newTop = textViewOrigin.Y + textBottom - _activeWpfTextView.ViewportTop;
            windowFrame.SetFramePos(VSSETFRAMEPOS.SFP_fMove, ref guid, (int) newLeft, (int) newTop, 0, 0);
            return windowFrame;
        }

        private void ParseToolWindowSelected(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = this.FindToolWindow(typeof(TestPackageToolWindow), 0, true);
            if (window?.Frame == null)
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

        private void TestPackageNavigationControlViewModelOnSelectedItemChangedEvent(object sender, SelectedItemChangedEventArgs selectedItemChangedEventArgs)
        {
            _vsWorkspace.OpenDocument(selectedItemChangedEventArgs.SelectedItem.ContainingDocumentId);

            NavigateTo(selectedItemChangedEventArgs.SelectedItem.Node.Span);
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
        private void ParseMenuSelected(object sender, EventArgs e)
        {
            _parseLogic = new Parser();
            _parseLogic.ParseWorkSpace(_vsWorkspace);

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

            RunningDocumentTable?.AdviseRunningDocTableEvents(this, out _runningDocumentTableCookie);
        }

        private static IServiceProvider GlobalServiceProvider => _globalServiceProvider ??
                                                                 (_globalServiceProvider = (IServiceProvider) GetGlobalService(typeof (IServiceProvider)));

        private IVsRunningDocumentTable RunningDocumentTable => _runningDocumentTable ??
                                                                (_runningDocumentTable = GetService<IVsRunningDocumentTable, SVsRunningDocumentTable>(GlobalServiceProvider));

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
