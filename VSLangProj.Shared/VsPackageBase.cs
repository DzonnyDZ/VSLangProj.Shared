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

        /// <summary>In case there was error deploying local custom project system, reports the issue to user asynchronously</summary>
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
        /// <returns>In case exception occurs during initialization returns the exception, otherwise null</returns>
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
    }
}