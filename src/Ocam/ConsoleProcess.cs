using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Ocam
{
    public class ConsoleProcess
    {
        /// <summary>
        /// Starts a process and captures stdout and stderr.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        /// <param name="wait">Amount of time, in milliseconds, to wait, or -1 to wait indefinitely.</param>
        /// <param name="exitCode">Process exit code, if exited within the wait timeout period.</param>
        /// <param name="stdout"></param>
        /// <param name="stderr"></param>
        /// <returns>Returns true if process exited, false if process duration exceeded wait milliseconds.</returns>
        /// <remarks>Ouput is not captured if process duration exceeds wait milliseconds.</remarks>
        public static bool Start(string fileName, string arguments, string input, int wait, out int exitCode, out string stdout, out string stderr)
        {
            exitCode = -1;

            var outbuf = new StringBuilder();
            var errbuf = new StringBuilder();
            var proc = new Process();
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.UseShellExecute = false;
            if (!String.IsNullOrEmpty(input))
            {
                proc.StartInfo.RedirectStandardInput = true;
            }
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.Arguments = arguments;
            //proc.OutputDataReceived += (sender, args) => outbuf.Append(args.Data + Environment.NewLine);
            proc.ErrorDataReceived += (sender, args) => errbuf.Append(args.Data + Environment.NewLine);

            proc.Start();
            //proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            if (!String.IsNullOrEmpty(input))
            {
                proc.StandardInput.Write(input);
                proc.StandardInput.Flush();
                proc.StandardInput.Close();
            }

            stdout = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(wait);
            if (proc.HasExited)
            {
                // stdout = outbuf.ToString();
                stderr = errbuf.ToString();
                exitCode = proc.ExitCode;
                return true;
            }
            else
            {
                stdout = null;
                stderr = null;
                return false;
            }
        }
    }
}
