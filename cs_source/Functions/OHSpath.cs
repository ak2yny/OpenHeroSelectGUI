using OpenHeroSelectGUI.Settings;
using System;
using System.IO;
using System.Linq;

namespace OpenHeroSelectGUI.Functions
{
    /// <summary>
    /// File and directory path related functions.
    /// </summary>
    internal static class OHSpath
    {
        /// <summary>
        /// Current directory (exe or 'start in')
        /// </summary>
        public static readonly string CD = Directory.GetCurrentDirectory();
        public static readonly string Model = Path.Combine(CD, "stages", ".models");
        public static readonly string Activision = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Activision");
        /// <summary>
        /// Get the current tab (game) using "mua" as fallback.
        /// </summary>
        public static string Game => CfgSt.GUI.Game == "" ? "mua" : CfgSt.GUI.Game;
        /// <summary>
        /// Get the saves folder for the current tab (game). Must be checked for existence.
        /// </summary>
        public static string SaveFolder => Path.Combine(Activision, Game == "xml2" ? "X-Men Legends 2" : "Marvel Ultimate Alliance");
        public static string Team_bonus => Path.Combine(CD, Game, "team_bonus.engb.xml");
        /// <summary>
        /// Get the full path to the herostat folder.
        /// </summary>
        /// <returns>Full path to the herostat folder</returns>
        public static string HsFolder => GetOHSFolder(CfgSt.OHS.HerostatFolder);
        /// <summary>
        /// Get the full path to an OHS game sub folder.
        /// </summary>
        /// <returns>Full path to an OHS game sub folder</returns>
        public static string GetOHSFolder(string FolderString)
        {
            string Folder = GetRooted(FolderString);
            return Directory.Exists(Folder)
                ? Folder
                : Directory.CreateDirectory(Path.Combine(CD, Game, "xml")).FullName;
        }
        /// <summary>
        /// Get the full path to an OHS game file or folder or original path if it's rooted.
        /// </summary>
        /// <returns>Full path to <paramref name="Input" /> prepending OHS game folder, or <paramref name="Input" /> if rooted</returns>
        public static string GetRooted(string Input)
        {
            return Path.IsPathRooted(Input)
                ? Input
                : Path.Combine(CD, Game, Input);
        }
        /// <summary>
        /// Get the folder name without the path to the OHS game folder. E.g.: "C:\OHS\mua\herostats" to "herostats".
        /// </summary>
        /// <returns>The trimmed folder (path) or the same path, if it's not in the OHS game folder</returns>
        public static string TrimGameFolder(string OldPath)
        {
            string G = Path.Combine(CD, Game);
            return OldPath.StartsWith(G) ?
                OldPath[(G.Length + 1)..] :
                OldPath;
        }
        /// <summary>
        /// Get folders with a package folder from the game folder and MO2 mod folders, according to the settings.
        /// </summary>
        /// <returns>Array with matching folder paths as strings or empty array</returns>
        public static string[] GetFoldersWpkg()
        {
            string[] PkgSourceFolders = [CfgSt.OHS.GameInstallPath, CfgSt.GUI.GameInstallPath];
            if (Directory.Exists(CfgSt.OHS.GameInstallPath)
                && Directory.GetParent(CfgSt.OHS.GameInstallPath) is DirectoryInfo MO2
                && MO2.Name == "mods")
            {
                PkgSourceFolders = [.. PkgSourceFolders, .. MO2.EnumerateDirectories().Select(d => d.FullName)];
            }
            return PkgSourceFolders.Where(f => Directory.Exists(Path.Combine(f, "packages", "generated", "characters"))).Distinct().ToArray();
        }
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (from setting) using a relative path and <paramref name="Source" /> and <paramref name="Target" /> filenames to rename simultaneously.
        /// </summary>
        public static void CopyToGameRel(string SourceFolder, string GameFolders, string Source, string Target) => CopyToGame(Path.Combine(SourceFolder, GameFolders), GameFolders, Source, Target);
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (from setting) using a relative path (in both) and separate <paramref name="file" /> name.
        /// </summary>
        public static void CopyToGameRel(string SourceFolder, string GameFolders, string file) => CopyToGame(Path.Combine(SourceFolder, GameFolders), GameFolders, file, file);
        /// <summary>
        /// Copy <paramref name="SourceFile" /> (<see cref="FileInfo"/>) to game folder (relative path inside game folder from setting).
        /// </summary>
        public static void CopyToGame(FileInfo SourceFile, string GameFolder) => CopyToGame(SourceFile, GameFolder, SourceFile.Name);
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (relative path inside game folder from setting), using a separate <paramref name="file" /> name.
        /// </summary>
        public static void CopyToGame(string? SourceFolder, string GameFolder, string file) => CopyToGame(SourceFolder, GameFolder, file, file);
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (relative path inside game folder from setting), using <paramref name="Source" /> and <paramref name="Target" /> filenames to rename simultaneously.
        /// </summary>
        public static void CopyToGame(string? SourceFolder, string GameFolder, string Source, string Target)
        {
            if (!string.IsNullOrWhiteSpace(SourceFolder)
                && !string.IsNullOrWhiteSpace(Source))
            {
                CopyToGame(new FileInfo(Path.Combine(SourceFolder, Source)), GameFolder, Target);
            }
        }
        /// <summary>
        /// Copy <paramref name="SourceFile" /> (<see cref="FileInfo"/>) to game folder (relative path inside game folder from setting), using <paramref name="Target" /> filename to rename simultaneously.
        /// </summary>
        public static void CopyToGame(FileInfo SourceFile, string GameFolder, string Target)
        {
            if (CfgSt.OHS.GameInstallPath != ""
                && SourceFile.Exists)
            {
                DirectoryInfo TP = Directory.CreateDirectory(Path.Combine(CfgSt.OHS.GameInstallPath, GameFolder));
                File.Copy(SourceFile.FullName, Path.Combine(TP.FullName, Target), true);
            }
        }
        /// <summary>
        /// Backup the "Save" folder in the game's save location.
        /// </summary>
        public static void BackupSaves()
        {
            FileInfo Herostat = new(Path.Combine(CfgSt.OHS.GameInstallPath, "data", CfgSt.OHS.HerostatName));
            DateTime Date = Herostat.Exists
                ? Herostat.LastWriteTime
                : DateTime.Now;
            if (Herostat.Exists && MoveSaves("Save", $"{Date:yyMMdd-HHmmss}"))
            {
                _ = Herostat.CopyTo(Path.Combine(SaveFolder, $"{Date:yyMMdd-HHmmss}", CfgSt.OHS.HerostatName));
            }
        }

        /// <summary>
        /// Move folders in the game's save location by providing names.
        /// </summary>
        public static bool MoveSaves(string From, string To)
        {
            DirectoryInfo Source = new(Path.Combine(SaveFolder, From));
            if (Source.Exists && Source.EnumerateFiles().Any())
            {
                Source.MoveTo(Path.Combine(SaveFolder, To));
                return true;
            }
            return false;
        }
    }
}
