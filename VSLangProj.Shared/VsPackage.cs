/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Dzonny.VSLangProj
{

    /// <summary>Base class for Visual Studio packages</summary>
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    public abstract class VsPackageBase : Package
    {

        /// <summary>CTor - creates a new instance of the <see cref="VsPackageBase"/> class</summary>
        /// <param name="cpsName">Name of custom project system</param>
        /// <exception cref="ArgumentNullException"><paramref name="cpsName"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="cpsName"/> is an empty string</exception>
        protected VsPackageBase(string cpsName)
        {
            if (cpsName == null) throw new ArgumentException(nameof(cpsName));
            if (cpsName == string.Empty) throw new ArgumentException("Value cannot be an empty string", nameof(cpsName));
            deploymentException = EnsureCustomProjectSystem(cpsName);
        }

        /// <summary>In case deployment of custom project system failed, contains the exception</summary>
        private readonly Exception deploymentException;

        /// <summary>Called when the VSPackage is loaded by Visual Studio.</summary>
        protected override void Initialize()
        {
            if (deploymentException != null)
                WriteError();
        }

        /// <summary>In case there was error deployng local custom project system, reports the issue to user assynchronously</summary>
        /// <returns>Task to await the async operation</returns>
        protected virtual async Task WriteError()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsOutputWindow outputWindow = GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow != null)
            {
                Guid guidGeneral = VSConstants.GUID_OutWindowGeneralPane;
                IVsOutputWindowPane windowPane;
                if (ErrorHandler.Failed(outputWindow.GetPane(ref guidGeneral, out windowPane)) && (ErrorHandler.Succeeded(outputWindow.CreatePane(ref guidGeneral, null, 1, 1))))
                    outputWindow.GetPane(ref guidGeneral, out windowPane);

                string desc = ((DescriptionAttribute)GetType().GetCustomAttributes(typeof(DescriptionAttribute), true).First()).Description;

                if (windowPane != null)
                    windowPane.OutputString($"Error when deploying custom project system {desc}:\r\n{deploymentException.GetType().Name}\r\n{deploymentException.Message}\r\n{deploymentException.StackTrace}");
            }
        }

        /// <summary>Ensures that up-to-date version for custom project system is installed</summary>
        /// <param name="cpsName">Name of custom project system</param>
        /// <returns>In case exception occurs during inituialization returns the exception, ontherwise null</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cpsName"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="cpsName"/> is an empty string</exception>
        protected virtual Exception EnsureCustomProjectSystem(string cpsName)
        {
            if (cpsName == null) throw new ArgumentException(nameof(cpsName));
            if (cpsName == string.Empty) throw new ArgumentException("Value cannot be an empty string", nameof(cpsName));
            var installer = new CustomProjectSystemInstaller(GetType(), cpsName);
            try
            {
                if (installer.NeedsDeployment()) installer.Deploy();
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        /// <summary>The GUID for this package.</summary>
        public const string PackageGuid = "b584f11e-5e77-40e8-bbfd-f70b550504bb";

        /// <summary>The GUID for this project type.  It is unique with the project file extension and appears under the VS registry hive's Projects key.</summary>
        public const string ProjectTypeGuid = "e452ebf3-3bbb-4a96-b835-ae6ecaeab85a";

        /// <summary>The file extension of this project type.  No preceding period.</summary>
        public const string ProjectExtension = "ilproj";

        /// <summary>The default namespace this project compiles with, so that manifest resource names can be calculated for embedded resources.</summary>
        internal const string DefaultNamespace = "Dzonny.ILProj";
    }
}