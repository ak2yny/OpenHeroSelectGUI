﻿using OpenHeroSelectGUI.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace OpenHeroSelectGUI.Functions
{
    public class Util
    {
        /// <summary>
        /// Run an <paramref name="exe" /> through CMD, if it exists. Output is converted to boolean.
        /// </summary>
        /// <returns><see langword="True" />, if the <paramref name="exe" /> exists and if the output was returned (not <see langword="null"/>), otherwise <see langword="false"/>.</returns>
        public static bool RunExeInCmd(string exe, string args)
        {
            if (File.Exists(exe.EndsWith(".exe") ? exe : $"{exe}.exe"))
            {
                return RunDosCommnand(exe, args) is not null;
            }
            return false;
        }
        /// <summary>
        /// Run a MS Dos Command <paramref name="cmd" /> <paramref name="vars" />
        /// </summary>
        /// <seealso cref="http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=457996"/>
        /// <param name="cmd">Executable to run (runs in console by default) - use "cmd" for command prompt and pass commands as arguments</param>
        /// <param name="vars">Arguments passed to the executable</param>
        /// <returns>The <see cref="Process.StandardOutput" /> or <see langword="null"/> if <see cref="Process" /> couldn't srart, returned no output/error exit code, or threw an exception.</returns>
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
        /// <returns>Exit code or 5 if the command/process couldn't be started.</returns>
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
        /// <returns>The full path in the temp folder, if 7-zip was found and could extract the file as an archive, otherwise <see langword="null"/>.</returns>
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
                string Exe = OHSpath.StartExe();
                ProcessStartInfo Game = new(Exe, CfgSt.GUI.ExeArguments)
                {
                    WorkingDirectory = Path.GetDirectoryName(Exe)
                };
                _ = Process.Start(Game);
            }
            catch { } // Fail silently
        }
        /// <summary>
        /// Checks the <paramref name="FilePath"/> if it's a compatible Game.exe from MUA.
        /// </summary>
        /// <returns>The <see cref="FileStream"/> from <paramref name="FilePath"/>, if it's a compatible Game.exe, otherwise <see langword="null"/>.</returns>
        public static FileStream? GameExe(string FilePath)
        {
            if (File.Exists(FilePath))
            {
                byte[] buffer = new byte[512];
                try
                {
                    FileStream fs = File.Open(FilePath, FileMode.Open);
                    fs.Position = 0x3aea90;
                    _ = fs.Read(buffer, 0, buffer.Length);
                    if (fs.Length > 4200000 && "D3-02-54-8B-CC-E6-32-31-A7-EA-C3-43-12-98-DD-7E" == BitConverter.ToString(System.Security.Cryptography.MD5.HashData(buffer)))
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
        public static bool HexEdit(long Position, string NewValue, string FilePath)
        {
            if (GameExe(FilePath) is FileStream fs)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(NewValue);
                fs.Position = Position;
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
                return true;
            }
            return false;
        }
        //public static void HexEdit(long Position, string NewValue, string FilePath) => HexEdit(Position, Encoding.ASCII.GetBytes(NewValue), FilePath);
        /// <summary>
        /// Hex edit the file in <paramref name="FilePath"/> at <paramref name="Position"/> to <paramref name="NewValue"/>.
        /// </summary>
        /// <returns><see langword="True"/>, if no exceptions occur, otherwise <see langword="False"/>.</returns>
        public static bool HexEdit(long Position, byte[] NewValue, string FilePath)
        {
            try
            {
                using FileStream fs = new(FilePath, FileMode.Open);
                fs.Position = Position;
                fs.Write(NewValue, 0, NewValue.Length);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Hex edit the file in <paramref name="FilePath"/> at its first occurrence of <paramref name="OldValue"/> to <paramref name="NewValue"/>, if any.
        /// </summary>
        /// <returns><see langword="True"/>, if hex-edited, otherwise <see langword="False"/>.</returns>
        public static bool HexEdit(string OldValue, string NewValue, string FilePath)
        {
            if (NewValue.Length <= OldValue.Length)
            {
                try
                {
                    byte[] Bytes = File.ReadAllBytes(FilePath);
                    byte[] ByteVal = Encoding.ASCII.GetBytes(OldValue);
                    long Pos = Bytes.AsSpan().IndexOf(ByteVal);
                    if (Pos > -1)
                    {
                        byte[] New = Encoding.ASCII.GetBytes(NewValue);
                        for (int i = 0; i < ByteVal.Length; i++) { ByteVal[i] = i < New.Length ? New[i] : new byte(); }
                        return HexEdit(Pos, ByteVal, FilePath);
                    }
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
}
