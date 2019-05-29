using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Dzonny.VSLangProj
{

    /// <summary>Base class for Visual Studio async-load packages</summary>
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public abstract class AsyncProjectSystemPackage : AsyncPackage
    {
        /// <summary>Name of custom project system</summary>
        private readonly string cpsName;

        /// <summary>CTor - creates a new instance of the <see cref="AsyncProjectSystemPackage"/> class</summary>
        /// <param name="cpsName">Name of custom project system</param>
        /// <exception cref="ArgumentNullException"><paramref name="cpsName"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="cpsName"/> is an empty string</exception>
        protected AsyncProjectSystemPackage(string cpsName)
        {
            if (cpsName == null) throw new ArgumentException(nameof(cpsName));
            if (cpsName == string.Empty) throw new ArgumentException("Value cannot be an empty string", nameof(cpsName));
            this.cpsName = cpsName;
        }

        /// <summary>The async initialization portion of the package initialization process. This method is invoked from a background thread.</summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">Used to report progress loading</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var deploymentException = EnsureCustomProjectSystem(cpsName);

            if (deploymentException != null)
                await WriteErrorAsync(deploymentException);
        }

        /// <summary>In case there was error deploying local custom project system, reports the issue to user asynchronously</summary>
        /// <param name="deploymentException">Exception describing the issue which has happened</param>
        /// <returns>Task to await the async operation</returns>
        protected virtual async Task WriteErrorAsync(Exception deploymentException)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsOutputWindow outputWindow = await GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
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