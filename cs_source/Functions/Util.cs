using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenHeroSelectGUI.Functions
{
    public static class Util
    {
        /// <summary>
        /// Run an <paramref name="exe" /> through CMD, if it exists. Output is converted to boolean.
        /// </summary>
        /// <returns><see langword="True" />, if the <paramref name="exe" /> exists and if the output was returned (not <see langword="null"/>); otherwise <see langword="false"/>.</returns>
        public static bool RunExeInCmd(string exe, string args)
        {
            return File.Exists(exe.EndsWith(".exe") ? exe : $"{exe}.exe") && RunDosCommnand(exe, args) is not null;
        }
        /// <summary>
        /// Run a MS Dos Command <paramref name="cmd" /> <paramref name="vars" />
        /// </summary>
        /// <seealso cref="http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=457996"/>
        /// <param name="cmd">Executable to run (runs in console by default) - use "cmd" for command prompt and pass commands as arguments</param>
        /// <param name="vars">Arguments passed to the executable</param>
        /// <returns>The <see cref="Process.StandardOutput" />, if <see cref="Process" /> started, returned an output/error exit code, and didn't throw an exception; otherwise <see langword="null"/>.</returns>
        public static string? RunDosCommnand(string cmd, string vars)
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
            try { return p.Start() && p.StandardOutput.ReadToEnd() is string SO && p.ExitCode == 0 ? SO : null; }
            catch { return null; }
        }
        /// <summary>
        /// Run an elevated command <paramref name="ecmd" /> <paramref name="vars" /> in the current directory, for OHS.
        /// </summary>
        /// <returns>Exit code, if the command/process started; otherwise 5.</returns>
        public static int RunElevated(string ecmd, string vars)
        {
            string cmd = "cmd";
            string ev = $"/c \"set __COMPAT_LAYER=RUNASINVOKER && \"{ecmd} {vars}";
            ProcessStartInfo sinf = new(cmd, ev) { CreateNoWindow = true };
            Process p = new() { StartInfo = sinf };
            if (p.Start())
            {
                p.WaitForExit();
                return p.ExitCode;
            }
            return 5;
        }
        /// <summary>
        /// Use 7-zip to try to extract an <paramref name="Archive"/> (any file path). Currently hard coded to extract to <paramref name="ExtName"/> within the temp folder.
        /// </summary>
        /// <returns>The full path in the temp folder, if 7-zip was found and could extract the file as an archive; otherwise <see langword="null"/>.</returns>
        public static string? Run7z(string Archive, string ExtName)
        {
            string ExtPath = Path.Combine(OHSpath.Temp, ExtName);
            return RunExeInCmd(File.Exists("7z.exe") ? "7z" : Path.Combine(OHSpath.CD, "OHSGUI", "7z.exe"), $"x \"{Archive}\" -o\"{ExtPath}\" -y")
                ? ExtPath
                : null;
        }
        /// <summary>
        /// Run the game's .exe as defined in settings, using arguments as defined in settings. Note: May fail silently.
        /// </summary>
        public static void RunGame()
        {
            if (CfgSt.GUI.FreeSaves) { _ = OHSpath.BackupSaves(); }
            try
            {
                string Exe = OHSpath.GetStartExe;
                ProcessStartInfo Game = new(Exe, CfgSt.GUI.ExeArguments)
                {
                    WorkingDirectory = Path.GetDirectoryName(Exe)
                };
                _ = Process.Start(Game);
            }
            catch { } // Fail silently
        }
        /// <summary>
        /// The MD5 hash of 512 bytes at offset 0x3aea90, which seems to match most versions of game.exe.
        /// </summary>
        private static readonly byte[] GameExeMD5Hash = [0xD3, 0x02, 0x54, 0x8B, 0xCC, 0xE6, 0x32, 0x31, 0xA7, 0xEA, 0xC3, 0x43, 0x12, 0x98, 0xDD, 0x7E];
        /// <summary>
        /// Checks the <paramref name="FilePath"/> if it's a compatible Game.exe from MUA.
        /// </summary>
        /// <returns>The <see cref="FileStream"/> from <paramref name="FilePath"/>, if it's a compatible Game.exe; otherwise <see langword="null"/>.</returns>
        public static FileStream? GameExe(string? FilePath)
        {
            if (File.Exists(FilePath))
            {
                byte[] buffer = new byte[512];
                try
                {
                    FileStream fs = File.Open(FilePath, FileMode.Open);
                    fs.Position = 0x3aea90;
                    _ = fs.Read(buffer, 0, buffer.Length);
                    if (fs.Length > 4200000 && System.Security.Cryptography.MD5.HashData(buffer).SequenceEqual(GameExeMD5Hash))
                    {
                        return fs;
                    }
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
        /// <summary>
        /// Hex edit the file in <paramref name="FilePath"/> at <paramref name="Position"/> to <paramref name="NewValue"/>. Verifies .exe for known MD5 hash (section only).
        /// </summary>
        /// <remarks>Exceptions: File.IO (and more)</remarks>
        public static bool HexEdit(long Position, string NewValue, string FilePath)
        {
            if (GameExe(FilePath) is FileStream fs)
            {
                HexEdit(Position, NewValue, fs);
                fs.Close();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Hex edit the file stream <paramref name="fs"/> at <paramref name="Position"/> to <paramref name="NewValue"/>.
        /// </summary>
        /// <remarks>Exceptions: File.IO (and more)</remarks>
        public static void HexEdit(long Position, string NewValue, FileStream fs)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(NewValue);
            fs.Position = Position;
            fs.Write(bytes, 0, bytes.Length);
        }
        /// <summary>
        /// Hex edit the file in <paramref name="FilePath"/> and replace all occurrences of <paramref name="OldValues"/> with <paramref name="NewValue"/>, if any.
        /// If only one string is in <paramref name="OldValues"/>, only the first occurrence is replaced, unless specified with <paramref name="AllOccurrences"/>.
        /// </summary>
        /// <returns><see langword="True"/>, if hex-edited; otherwise <see langword="false"/>.</returns>
        public static bool HexEdit(string[] OldValues, string NewValue, string FilePath, bool AllOccurrences = false)
        {
            if (0 < NewValue.Length && NewValue.Length <= OldValues.Min(static v => v.Length))
            {
                if (OldValues.Length > 1) { AllOccurrences = true; }
                try
                {
                    byte[] New = Encoding.ASCII.GetBytes(NewValue);
                    byte[] ZeroPad = new byte[OldValues.Max(static v => v.Length) - New.Length];
                    Span<byte> Bytes = File.ReadAllBytes(FilePath).AsSpan();
                    using FileStream fs = new(FilePath, FileMode.Open);
                    for (int i = 0; i < OldValues.Length; i++)
                    {
                        Span<byte> ByteVal = Encoding.ASCII.GetBytes(OldValues[i]).AsSpan();
                        for (long p = 0; p <= Bytes.Length - ByteVal.Length; p++)
                        {
                            if (Bytes.Slice((int)p, ByteVal.Length).SequenceEqual(ByteVal))
                            {
                                fs.Position = p;
                                fs.Write(New, 0, New.Length);
                                fs.Write(ZeroPad, 0, ByteVal.Length - New.Length);
                                if (!AllOccurrences) { return true; }
                                p += ByteVal.Length - 1;
                            }
                        }
                    }
                    return true;
                }
                catch { return false; }
            }
            return false;
        }
    }
    /// <summary>
    /// Object extension class.
    /// </summary>
    public static class GUIObject
    {
        /// <summary>
        /// Copy properties from a defined <paramref name="fromObj"/> to another defined <paramref name="toObj"/>. Additionally, type must match. If any rule isn't followed, this will perform a crash.
        /// </summary>
        public static void Copy(object fromObj, object toObj)
        {
            if (fromObj.Equals(toObj) || fromObj is null) { return; }
            foreach (System.Reflection.PropertyInfo P in fromObj.GetType().GetProperties())
            {
                object? o = P.GetValue(fromObj);
                P.SetValue(toObj, o);
            }
        }
    }
    /// <summary>
    /// Other extensions
    /// </summary>
    public static class Extensions
    {
        public static void Sort<T>(this System.Collections.ObjectModel.ObservableCollection<T> collection, Func<IEnumerable<T>, IOrderedEnumerable<T>> sort)
        {
            T[] SC = [.. sort(collection)];
            for (int i = 0; i < SC.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(SC[i], collection[i])) { collection[i] = SC[i]; }
                // Replace is most efficient. To improve UI animations and keep selection, do following:
                // int o = collection.IndexOf(SC[i]); if (o != i) { collection.Move(o, i); }
            }
        }
        /// <summary>
        /// Determines whether the <paramref name="filestream"/> appears to contain XML content.
        /// </summary>
        /// <remarks>Reads from the current position of <paramref name="filestream"/> and advances the position to the index of the first non-whitespace character.</remarks>
        /// <returns><see langword="True"/>, if the first non-whitespace character is '&lt;'; otherwise <see langword="false"/>.</returns>
        public static bool IsXml(this FileStream filestream)
        {
            char Format;
            while (char.IsWhiteSpace(Format = (char)filestream.ReadByte())) { }
            return Format == '<';
        }
    }

    public static class GUIHelpers
    {
        /// <summary>
        /// Finds parent in visual tree
        /// </summary>
        /// <returns>The first item of type <see cref="{T}"/> found in the ancestor visual tree if any; otherwise <see langword="null"/>.</returns>
        public static T? FindFirstParent<T>(Microsoft.UI.Xaml.DependencyObject child) where T : Microsoft.UI.Xaml.DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) { return parent; }
                child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}
