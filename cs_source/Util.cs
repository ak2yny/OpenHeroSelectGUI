using System;
using System.Diagnostics;

namespace OpenHeroSelectGUI
{
    public class Util
    {
        /// <summary>
        /// Run a MS Dos Command (used to compile/decompile MUA files)
        /// </summary>
        /// <seealso cref="http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=457996&SiteID=1"/>
        /// <param name="cmd"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        public static string RunDosCommnand(string cmd, string vars)
        {
            ProcessStartInfo sinf = new ProcessStartInfo(cmd, vars)
            {
                // The following commands are needed to redirect the standard output. This means that it will be redirected to the Process.StandardOutput StreamReader.
                RedirectStandardOutput = true,
                UseShellExecute = false,
                // Do not create that ugly black window, please...
                CreateNoWindow = true
            };
            // Now we create a process, assign its ProcessStartInfo and start it
            Process p = new Process { StartInfo = sinf };
            p.Start(); // well, we should check the return value here...
            // We can now capture the output into a string...
            string res = p.StandardOutput.ReadToEnd();
            // And do whatever we want with that.
            Console.WriteLine(res);
            return res;
        }

        /// <summary>
        /// Run an elevated command for OHS. OHS uses the error.log...
        /// </summary>
        public static string RunElevated(string ecmd, string vars)
        {
            string cmd = "cmd";
            string ev = "/c \"set __COMPAT_LAYER=RUNASINVOKER && \"" + ecmd + " " + vars;
            ProcessStartInfo sinf = new ProcessStartInfo(cmd, ev) { CreateNoWindow = true };
            Process p = new Process { StartInfo = sinf };
            p.Start();
            p.WaitForExit();
            return "OHS has finished.";
        }

    }
}
