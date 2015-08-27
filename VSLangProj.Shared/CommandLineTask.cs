using System;
using System.Diagnostics;
using Microsoft.Build.Utilities;

namespace Dzonny.VSLangProj
{
    /// <summary>Base MSBuild task that executes a command line</summary>
    public abstract class CommandLineTask : Task
    {
        /// <summary>When overriden in derived class gets path to EXE file to launch</summary>
        protected abstract string Exe { get; }

        /// <summary>When overriden in derived class gets command line for the process</summary>
        /// <returns>Command line arguments</returns>
        protected abstract string GetCommandLine();

        /// <summary>Processes message form console error output of program being executed</summary>
        /// <param name="text">The message</param>
        /// <remarks>This implementation always reports the message as error</remarks>
        protected void OnConsoleError(string text) { Log.LogError(text); }

        /// <summary>Processes standard oputput message of program being executed</summary>
        /// <param name="text">The message</param>
        /// <remarks>This implementation, if the message contains substring "error" reports it as error, if it contains substring "waening" reports it as a warning, otherwise as message.</remarks>
        protected void OnConsoleOutput(string text)
        {
            if (text.IndexOf("error", StringComparison.InvariantCultureIgnoreCase) >= 0)
                Log.LogError(text);
            else if (text.IndexOf("warning", StringComparison.InvariantCultureIgnoreCase) >= 0)
                Log.LogWarning(text);
            else Log.LogMessage(text);
        }


        /// <summary>Executes a task.</summary>
        /// <returns>True if the task executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
            using (var ilasm = new Process())
            {
                ilasm.StartInfo.UseShellExecute = false;
                ilasm.StartInfo.FileName = Exe;


                ilasm.StartInfo.Arguments = GetCommandLine();

                Log.LogCommandLine(ilasm.StartInfo.FileName + " " + ilasm.StartInfo.Arguments);
                Log.LogMessage($"Running {ilasm.StartInfo.FileName} {ilasm.StartInfo.Arguments}");

                ilasm.StartInfo.RedirectStandardError = true;
                ilasm.StartInfo.RedirectStandardOutput = true;
                ilasm.ErrorDataReceived += (sender, e) => { if (e.Data != null) OnConsoleError(e.Data); };
                ilasm.OutputDataReceived += (sender, e) => { if (e.Data != null) OnConsoleOutput(e.Data); };
                ilasm.Start();
                ilasm.BeginErrorReadLine();
                ilasm.BeginOutputReadLine();
                ilasm.WaitForExit();
                if (ilasm.ExitCode != 0)
                    Log.LogError($"Process {System.IO.Path.GetFileName(ilasm.StartInfo.FileName)} {ilasm.StartInfo.Arguments} exited with code {ilasm.ExitCode}");
                else
                    Log.LogMessage($"Process {System.IO.Path.GetFileName(ilasm.StartInfo.FileName)} {ilasm.StartInfo.Arguments} exited with code {ilasm.ExitCode}");
                return ilasm.ExitCode == 0;
            }
        }
    }
}