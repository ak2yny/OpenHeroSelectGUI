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
        // Game path patterns:
        private static readonly string[] GameFolders =
        [
            "actors",
            "automaps",
            "conversations",
            "data",
            "dialogs",
            "effects",
            "hud",
            "maps",
            "models",
            "motionpaths",
            "movies",
            "packages",
            "plugins",
            "scripts",
            "shaders",
            "skybox",
            "sounds",
            "subtitles",
            "texs",
            "textures",
            "ui"
        ];
        public static string Packages(string path, string Pkg = "") => Path.Combine(path, "packages", "generated", "characters", Pkg);
        /// <summary>
        /// Get the current tab (game) using "mua" as fallback.
        /// </summary>
        public static string Game => CfgSt.GUI.Game == "" ? "mua" : CfgSt.GUI.Game;
        /// <summary>
        /// Get the default game exe name.
        /// </summary>
        public static string DefaultExe => Game == "xml2" ? "XMen2.exe" : "Game.exe";
        /// <summary>
        /// Get the saves folder for the current tab (game). Must be checked for existence.
        /// </summary>
        public static string SaveFolder => Path.Combine(Activision, Game == "xml2" ? "X-Men Legends 2" : "Marvel Ultimate Alliance");
        /// <summary>
        /// Get the installation folder for the current game, using the GUI path. Falls back to the OHS path (mod path), if the GUI path is empty or null.
        /// </summary>
        /// <returns>The game installation path.</returns>
        public static string GameInstallPath()
        {
            string GIP = Game == "xml2" ? CfgSt.GUI.Xml2InstallPath : CfgSt.GUI.GameInstallPath;
            return string.IsNullOrEmpty(GIP)
                ? CfgSt.OHS.GameInstallPath
                : GIP;
        }
        public static string Team_bonus => Path.Combine(CD, Game, "team_bonus.engb.xml");
        /// <summary>
        /// Get the OHS temp folder path as a <see cref="string"/>. Performs a crash if no permission.
        /// </summary>
        public static string Temp => Directory.CreateDirectory(Path.Combine(CD, "Temp")).FullName;
        /// <summary>
        /// Tries to construct the path to MUA's Game.exe. Doesn't check for a valid path or existence.
        /// </summary>
        public static string GameExe => string.IsNullOrEmpty(CfgSt.GUI.ActualGameExe) ? Path.Combine(GameInstallPath(), CfgSt.OHS.ExeName) : CfgSt.GUI.ActualGameExe;
        /// <summary>
        /// Construct a file name of a <paramref name="PathWithoutExt"/> file that doesn't exist. (Extension is optional.)
        /// </summary>
        /// <returns><paramref name="PathWithoutExt"/>(+<paramref name="Ext"/>) if it doesn't exist, otherwise <paramref name="PathWithoutExt"/>+<see cref="DateTime.Now"/>(+<paramref name="Ext"/>). (The latter is not verified for existence.)</returns>
        public static string GetVacant(string PathWithoutExt, string Ext = "") => File.Exists($"{PathWithoutExt}{Ext}")
                ? $"{PathWithoutExt}-{DateTime.Now:-yyMMdd-HHmmss}{Ext}"
                : $"{PathWithoutExt}{Ext}";
        /// <summary>
        /// Get the full path to the herostat folder.
        /// </summary>
        /// <returns>Full path to the existing herostat folder</returns>
        public static string HsFolder => GetHsFolder(CfgSt.OHS.HerostatFolder);
        /// <summary>
        /// Get the full path to an OHS <see cref="Game"/> sub <paramref name="FolderString"/>. Performs a crash if no permission.
        /// </summary>
        /// <returns>Full path to an OHS <see cref="Game"/> sub folder or the folder path (if path). Defaults to "xml", if the folder doesn't exist.</returns>
        public static string GetHsFolder(string FolderString)
        {
            string Folder = GetRooted(FolderString);
            return Directory.Exists(Folder)
                ? Folder
                : Directory.CreateDirectory(Path.Combine(CD, Game, "xml")).FullName;
        }
        /// <summary>
        /// Expand the <paramref name="Input"/> path to a full path within the OHS <see cref="Game"/> folder. <paramref name="Input"/> may be a full or relative path or a comma separated list.
        /// </summary>
        /// <returns>Full path to [<see cref="CD"/> = OHS]\<see cref="Game"/>\<paramref name="Input"/>, or <paramref name="Input"/> if rooted.</returns>
        public static string GetRooted(params string[] Input) => Path.Combine(CD, Game, Join(Input));
        /// <summary>
        /// Combine the <paramref name="Input"/> path(s) to a <see cref="string"/>, adding a <see cref="Path.DirectorySeparatorChar"/> between each member.
        /// </summary>
        /// <returns>A relative path from the first <paramref name="Input"/> member (which may be rooted).</returns>
        public static string Join(params string[] Input)
        {
            for (int i = 1; i < Input.Length; i++) { Input[i] = Input[i].Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
            return string.Join(Path.DirectorySeparatorChar, Input);
        }
        /// <summary>
        /// Get the folder name without the path to the OHS <see cref="Game"/> folder. E.g.: "C:\OHS\mua\herostats" to "herostats".
        /// </summary>
        /// <returns>The trimmed folder (path) or the same path, if it's not in the OHS <see cref="Game"/> folder</returns>
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
            string[] PkgSourceFolders = [CfgSt.OHS.GameInstallPath, GameInstallPath()];
            if (Directory.Exists(CfgSt.OHS.GameInstallPath)
                && Directory.GetParent(CfgSt.OHS.GameInstallPath) is DirectoryInfo MO2
                && MO2.Name == "mods")
            {
                PkgSourceFolders = [.. PkgSourceFolders, .. MO2.EnumerateDirectories().Select(d => d.FullName)];
            }
            return PkgSourceFolders.Where(f => Directory.Exists(Packages(f))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (from setting) using a <paramref name="RelativePath"/> and <paramref name="Source" /> and <paramref name="Target" /> filenames to rename simultaneously.
        /// </summary>
        public static void CopyToGameRel(string SourceFolder, string RelativePath, string Source, string Target) => CopyToGame(Path.Combine(SourceFolder, RelativePath), RelativePath, Source, Target);
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (from setting) using a <paramref name="RelativePath"/> (in both) and separate <paramref name="file" /> name.
        /// </summary>
        public static void CopyToGameRel(string SourceFolder, string RelativePath, string file) => CopyToGame(Path.Combine(SourceFolder, RelativePath), RelativePath, file, file);
        /// <summary>
        /// Copy <paramref name="SourceFile" /> (<see cref="FileInfo"/>) to game folder (<paramref name="RelativePath"/> inside game folder from setting).
        /// </summary>
        public static void CopyToGame(FileInfo SourceFile, string RelativePath) => CopyToGame(SourceFile, RelativePath, SourceFile.Name);
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (<paramref name="RelativePath"/> inside game folder from setting), using a separate <paramref name="file" /> name.
        /// </summary>
        public static void CopyToGame(string? SourceFolder, string RelativePath, string file) => CopyToGame(SourceFolder, RelativePath, file, file);
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (<paramref name="RelativePath"/> inside game folder from setting), using <paramref name="Source" /> and <paramref name="Target" /> filenames to rename simultaneously.
        /// </summary>
        public static void CopyToGame(string? SourceFolder, string RelativePath, string Source, string Target)
        {
            if (!string.IsNullOrWhiteSpace(SourceFolder)
                && !string.IsNullOrWhiteSpace(Source))
            {
                CopyToGame(new FileInfo(Path.Combine(SourceFolder, Source)), RelativePath, Target);
            }
        }
        /// <summary>
        /// Copy <paramref name="SourceFile" /> (<see cref="FileInfo"/>) to game folder (<paramref name="RelativePath"/> inside game folder from setting), using <paramref name="Target" /> filename to rename simultaneously. Doesn't copy, if fails or files don't exist.
        /// </summary>
        public static void CopyToGame(FileInfo SourceFile, string RelativePath, string Target)
        {
            if (Path.IsPathFullyQualified(CfgSt.OHS.GameInstallPath) // The settings currently allow relative paths containing a "data" folder. This is not consistent
                && SourceFile.Exists)
            {
                try { File.Copy(SourceFile.FullName, Path.Combine(Directory.CreateDirectory(Path.Combine(CfgSt.OHS.GameInstallPath, RelativePath)).FullName, Target), true); }
                catch { }
            }
        }
        /// <summary>
        /// Recursively copy a complete <paramref name="Source"/> folder with all contents to a <paramref name="Target"/> path (must be a string returned from path info). Existing files are replaced.
        /// </summary>
        /// <returns><see langword="True"/>, if no exceptions occur, otherwise <see langword="False"/>.</returns>
        public static bool CopyFilesRecursively(DirectoryInfo Source, string Target)
        {
            try
            {
                foreach (DirectoryInfo dirPath in Source.GetDirectories("*", SearchOption.AllDirectories))
                {
                    _ = Directory.CreateDirectory(dirPath.FullName.Replace(Source.FullName, Target));
                }
                foreach (FileInfo SourceFile in Source.GetFiles("*", SearchOption.AllDirectories))
                {
                    _ = SourceFile.CopyTo(SourceFile.FullName.Replace(Source.FullName, Target), true);
                }
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Find the actual folder containing the mod files in an existing <paramref name="ModPath"/> by checking the folder names.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{DirectoryInfo}"/> with all folders that match.</returns>
        public static System.Collections.Generic.IEnumerable<DirectoryInfo> GetModSource(string ModPath)
        {
            return new DirectoryInfo(ModPath).EnumerateDirectories("*", SearchOption.AllDirectories)
                .FirstOrDefault(g => GameFolders.Contains(g.Name)) is DirectoryInfo FGF
                ? Path.GetRelativePath(FGF.Parent!.FullName, ModPath) == "."
                    ? [FGF.Parent!]
                    : FGF.Parent!.Parent!.EnumerateDirectories("*").Where(f => f.EnumerateDirectories("*").Any(g => GameFolders.Contains(g.Name)))
                : [];
        }
        /// <summary>
        /// Detect if GameInstallPath is the game folder or a mod folder. If it's a mod folder, prepare a new mod in <paramref name="Source"/> with <paramref name="TargetName"/> and optional <paramref name="InstallationFile"/> info.
        /// </summary>
        /// <returns>The the target path either to game files or the new mod folder. Returns <see langword="null"/> if none detected.</returns>
        public static string? GetModTarget(DirectoryInfo Source, string TargetName, string InstallationFile = "")
        {
            string Target = CfgSt.OHS.GameInstallPath;
            bool IsGIP = Path.IsPathFullyQualified(Target) && File.Exists(Path.Combine(Target, DefaultExe));
            if (!IsGIP
                && !string.IsNullOrWhiteSpace(Target)
                && new DirectoryInfo(Target) is DirectoryInfo GIP
                && GIP.Exists
                && GIP.Parent is DirectoryInfo Mods) // assuming it's an MO2 mod folder
            {
                Target = GetVacant(Path.Combine(Mods.FullName, TargetName));
                string MetaP = Path.Combine(Source.FullName, "meta.ini");
                if (!File.Exists(MetaP))
                {
                    string[] meta =
                    [
                        "[General]",
                        $"gameName={CfgSt.GUI.Game.PadRight(4, '1')}",
                        "modid=0",
                        $"version=d{DateTime.Now:yyyy.M.d}",
                        "newestVersion=",
                        "category=0",
                        "nexusFileStatus=1",
                        $"installationFile={InstallationFile.Replace("\\", "/")}",
                        "repository=Nexus"
                    ];
                    File.WriteAllLines(MetaP, meta);
                }
            }
            return IsGIP || Target != CfgSt.OHS.GameInstallPath ? Target : null;
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
                _ = Herostat.CopyTo(Path.Combine(SaveFolder, $"{Date:yyMMdd-HHmmss}", Herostat.Name), true);
            }
        }

        /// <summary>
        /// Move folders in the game's save location by providing names.
        /// </summary>
        /// <returns><see langword="True"/>, if saves could be moved, otherwise <see langword="False"/>.</returns>
        public static bool MoveSaves(string From, string To)
        {
            DirectoryInfo Source = new(Path.Combine(SaveFolder, From));
            if (Source.Exists && Source.EnumerateFiles().Any())
            {
                try
                {
                    Source.MoveTo(Path.Combine(SaveFolder, To));
                    return true;
                }
                catch { return false; }
            }
            return false;
        }
    }
}
