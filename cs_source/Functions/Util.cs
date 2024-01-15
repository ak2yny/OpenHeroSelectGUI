using OpenHeroSelectGUI.Settings;
using System.Diagnostics;
using System.IO;

namespace OpenHeroSelectGUI.Functions
{
    public class Util
    {
        /// <summary>
        /// Run an <paramref name="exe" /> through CMD, if it exists. Doesn't evaluate the arguments or return the output.
        /// </summary>
        /// <returns><see langword="True" />, if the <paramref name="exe" /> esits, <see langword="false" /> if it doesn't.</returns>
        public static bool RunExeInCmd(string exe, string args)
        {
            if (File.Exists(exe.EndsWith(".exe") ? exe : $"{exe}.exe"))
            {
                _ = RunDosCommnand(exe, args);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Run a MS Dos Command <paramref name="cmd" /> <paramref name="vars" />
        /// </summary>
        /// <seealso cref="http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=457996"/>
        /// <param name="cmd">Executable to run (runs in console by default) - use "cmd" for command prompt and pass commands as arguments</param>
        /// <param name="vars">Arguments passed to the executable</param>
        /// <returns>The <see cref="Process.StandardOutput" /> or empty string if <see cref="Process" /> couldn't srart</returns>
        public static string RunDosCommnand(string cmd, string vars)
        {
            ProcessStartInfo sinf = new(cmd, vars)
            {
                // The following commands are needed to redirect the standard output. This means that it will be redirected to the Process.StandardOutput StreamReader.
                RedirectStandardOutput = true,
                UseShellExecute = false,
                // Do not create that ugly black window, please...
                CreateNoWindow = true
            };
            // Now we create a process, assign its ProcessStartInfo
            Process p = new() { StartInfo = sinf };
            // We can now start the process and, if the process starts successfully, return the output string...
            return p.Start() ? p.StandardOutput.ReadToEnd() : "";
        }
        /// <summary>
        /// Run an elevated command <paramref name="ecmd" /> <paramref name="vars" /> for OHS. OHS uses the error.log...
        /// </summary>
        public static void RunElevated(string ecmd, string vars)
        {
            string cmd = "cmd";
            string ev = $"/c \"set __COMPAT_LAYER=RUNASINVOKER && \"echo e | {ecmd} {vars}";
            ProcessStartInfo sinf = new(cmd, ev) { CreateNoWindow = true };
            Process p = new() { StartInfo = sinf };
            _ = p.Start();
            p.WaitForExit();
            // We're not returning any result, instead we open explorer to the error.log at call.
        }
        /// <summary>
        /// Run the game's .exe as defined in settings, using arguments as defined in settings. Note: May fail silently.
        /// </summary>
        public static void RunGame()
        {
            if (CfgSt.GUI.FreeSaves) { OHSpath.BackupSaves(); }
            ProcessStartInfo Game = new(Path.Combine(CfgSt.GUI.GameInstallPath, CfgSt.OHS.ExeName), CfgSt.GUI.ExeArguments)
            {
                WorkingDirectory = CfgSt.GUI.GameInstallPath,
            };
            _ = Process.Start(Game);
        }
    }
}
