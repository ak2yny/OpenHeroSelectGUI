using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WFA_MUA
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
        public static string runDosCommnand(string cmd, string vars)
        {
            System.Diagnostics.ProcessStartInfo sinf = new System.Diagnostics.ProcessStartInfo(cmd, vars);
            // The following commands are needed to redirect the standard output. This means that it will be redirected to the Process.StandardOutput StreamReader.
            sinf.RedirectStandardOutput = true;
            sinf.UseShellExecute = false;
            // Do not create that ugly black window, please...
            sinf.CreateNoWindow = true;
            // Now we create a process, assign its ProcessStartInfo and start it
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = sinf;
            p.Start(); // well, we should check the return value here...
            // We can now capture the output into a string...
            string res = p.StandardOutput.ReadToEnd();
            // And do whatever we want with that.
            Console.WriteLine(res);
            return res;
        }

    }
}
