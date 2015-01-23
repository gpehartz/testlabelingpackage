﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ICETeam.TestPackage.ParseLogic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
    [Guid(GuidList.guidTestPackagePkgString)]
    public sealed class TestPackagePackage : Package
    {
        private VisualStudioWorkspace _vsWorkspace;
        private Parser _parseLogic;

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

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                var menuCommandId = new CommandID(GuidList.guidTestPackageCmdSet, (int) PkgCmdIDList.cmdidNavigate);
                var menuItem = new MenuCommand(MenuItemCallback, menuCommandId);
                mcs.AddCommand(menuItem);
            }

            var componentModel = (IComponentModel)this.GetService(typeof(SComponentModel));
            _vsWorkspace = componentModel.GetService<VisualStudioWorkspace>();
        }

        private void OnWorkSpaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            var changedDocument = e.NewSolution.GetDocument(e.DocumentId);
            if (changedDocument == null) return;

            var oldDocument = e.OldSolution.GetDocument(e.DocumentId);
            if (oldDocument != null)
            {
                var oldSyntaxTree = oldDocument.GetSyntaxTreeAsync().Result;
                var changedSyntaxTree = changedDocument.GetSyntaxTreeAsync().Result;

                var changes = changedSyntaxTree.GetChanges(oldSyntaxTree);
            }
            
            _parseLogic.ReparseItemsForDocument(_vsWorkspace, changedDocument);
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

            var sb = new StringBuilder();

            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()));
            sb.AppendFormat("Parsed item count: {0}", _parseLogic.ParsedData.Count()).AppendLine();

            foreach (var parsedData in _parseLogic.ParsedData)
            {
                var node = parsedData.Node;
                var document = _vsWorkspace.CurrentSolution.GetDocument(parsedData.ContainingDocumentId);

                sb.AppendFormat("Found item in {0} on position {1} with content {2}", document.FilePath, node.SpanStart, node).AppendLine();
            }

            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(0,
                ref clsid,
                "TestPackage",
                sb.ToString(),
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO,
                0, // false
                out result));

            _vsWorkspace.WorkspaceChanged += OnWorkSpaceChanged;
        }
    }
}