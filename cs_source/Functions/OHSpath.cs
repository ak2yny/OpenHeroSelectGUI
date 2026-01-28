using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
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
        public static readonly string Activision = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Activision");
        // MUA only (stages):
        public static readonly string StagesDir = Path.Combine(CD, "stages");
        public static readonly string Model = Path.Combine(StagesDir, ".models");
        public static readonly string NoPreview = Path.Combine(Model, ".NoPreview.png");
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
        public static readonly Func<string, int> IndexOfFileName = Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar
            ? static path => path.LastIndexOf(Path.DirectorySeparatorChar) + 1
            : static path => path.LastIndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + 1;
        /// <summary>
        /// Expands the base <paramref name="path"/> to "<paramref name="path"/>/packages/generated/characters/<paramref name="Pkg"/>".
        /// </summary>
        /// <returns>The combined file system path (or <paramref name="Pkg"/> if <paramref name="Pkg"/>'s rooted).</returns>
        public static string Packages(string path, string Pkg = "") => Path.Combine(path, "packages", "generated", "characters", Pkg);
        /// <summary>
        /// Get the current tab (game) for the OHS game path, using "mua" as fallback.
        /// </summary>
        public static string Game { get; set; } = "mua";
        /// <summary>
        /// Get the default game exe name.
        /// </summary>
        public static string DefaultExe => CfgSt.GUI.IsMua ? "Game.exe" : "XMen2.exe";
        /// <summary>
        /// Get the saves folder for the current tab/game (folder with 'Save' in it). Must be checked for existence.
        /// </summary>
        public static string SaveFolder => Path.Combine(Activision, CfgSt.GUI.IsMua ? "Marvel Ultimate Alliance" : "X-Men Legends 2");
        /// <summary>
        /// Tries to construct the path to the Game Install Path .exe. Falls back to <see cref="OHSsettings.GameInstallPath"/> + <see cref="OHSsettings.ExeName"/>. Doesn't check the latter.
        /// </summary>
        /// <returns>The full path to the .exe or an invalid path/string if settings are wrong.</returns>
        public static string GetActualGamePath => CfgSt.GUI.IsMua && Path.GetDirectoryName(CfgSt.GUI.ActualGameExe) is string GP && GP != "" ? GP
                                                      : CfgSt.GUI.GameInstallPath != "" ? CfgSt.GUI.GameInstallPath : CfgSt.OHS.GameInstallPath;
        /// <summary>
        /// Tries to construct the path to the Game Install Path .exe. Falls back to <see cref="OHSsettings.GameInstallPath"/> + <see cref="OHSsettings.ExeName"/>. Doesn't check the latter.
        /// </summary>
        /// <returns>The full path to the .exe or an invalid path/string if settings are wrong.</returns>
        public static string GetStartExe => Path.Combine(CfgSt.GUI.GameInstallPath != "" ? CfgSt.GUI.GameInstallPath : CfgSt.OHS.GameInstallPath, CfgSt.OHS.ExeName);
        /// <summary>
        /// Get the team bonus file path for the game.
        /// </summary>
        /// <returns>The full path to the game's hardcoded team bonus file.</returns>
        public static string Team_bonus => GetRooted("team_bonus.engb.xml");
        /// <summary>
        /// Get the x_voice directory for the game.
        /// </summary>
        /// <returns>The full path to the game's hardcoded x_voice directory ("x_voicePackage").</returns>
        public static string XvoiceDir => GetRooted("x_voicePackage");
        /// <summary>
        /// Get the "[OHS]/Temp" folder path. Creates the folder if missing.
        /// </summary>
        /// <remarks>Exceptions: System.IO exceptions.</remarks>
        public static string Temp
        {
            get
            {
                if (field is null || !Directory.Exists(field))
                {
                    field = Directory.CreateDirectory($"{CD}/Temp").FullName;
                }
                return field;
            }
        }
        /// <summary>
        /// Construct a full file name of a <paramref name="PathWithoutExt"/> file that doesn't exist. (<paramref name="Ext"/>ension is optional and not added if not specified.)
        /// </summary>
        /// <returns><paramref name="PathWithoutExt"/>(+<paramref name="Ext"/>) if it doesn't exist, otherwise <paramref name="PathWithoutExt"/>+<see cref="DateTime.Now"/>(+<paramref name="Ext"/>).</returns>
        public static string GetVacant(string PathWithoutExt, string Ext = "", int i = 0) =>
            (Ext == "" ? Directory.Exists(PathWithoutExt) : File.Exists($"{PathWithoutExt}{Ext}"))
                ? i > 0
                ? GetVacant($"{(i == 1 ? PathWithoutExt : PathWithoutExt[..^2])}-{i}", Ext, i + 1)
                : GetVacant($"{PathWithoutExt}-{DateTime.Now:yyMMdd-HHmmss}", Ext, i + 1)
                : $"{PathWithoutExt}{Ext}";
        /// <summary>
        /// Get the full path to the herostat file, providing the <paramref name="HsPath"/> relative in the herostat folder.
        /// </summary>
        /// <remarks>Exceptions can be: No file found; <paramref name="HsPath"/> contains invalid characters;
        /// creation of <see cref="HsFolder"/> fails (these IO exceptions are not mentioned, as the create fallback should never happen).</remarks>
        /// <returns>Full normalized path of the first herostat found.</returns>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="System.Security.SecurityException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="PathTooLongException"/>
        public static string GetHsFile(string HsPath)
        {
            return Directory.EnumerateFiles(GetParent(Path.Combine(HsFolder, HsPath))!,
                $"{HsPath[IndexOfFileName(HsPath)..]}.??????????").First();
        }
        /// <summary>
        /// Find herostats in a <paramref name="ModFolder"/>.
        /// </summary>
        /// <returns><see cref="IEnumerable{FileInfo}"/> of all <see cref="FileInfo"/> that match a herostat name or content.</returns>
        private static IEnumerable<string> GetFile(string ModFolder)
        {
            return Directory.EnumerateFiles(ModFolder, "*.txt").Concat(Directory.EnumerateFiles(ModFolder, "*.xml")).Concat(Directory.EnumerateFiles(ModFolder, "*.json"))
                .Where(static h => Path.GetFileName(h).Contains("herostat", StringComparison.OrdinalIgnoreCase)
                    || File.ReadAllText(h).Contains("stats", StringComparison.OrdinalIgnoreCase));
        }
        /// <summary>
        /// Find herostats in a <paramref name="ModFolder"/>.
        /// </summary>
        /// <returns><see cref="IEnumerable{FileInfo}"/> of all <see cref="FileInfo"/> that match a herostat name or content.</returns>
        public static IEnumerable<string> GetHsFiles(string ModFolder)
        {
            return GetFile(ModFolder) is IEnumerable<string> Hs && Hs.Any()
                ? Hs
                : Directory.EnumerateDirectories(ModFolder, "data").FirstOrDefault() is string D
                    && GetFile(D) is IEnumerable<string> HsD && HsD.Any()
                ? HsD
                : GetParent(ModFolder) is string P
                ? GetFile(P)
                : [];
        }
        /// <summary>
        /// Gets the cached <see cref="hsFolder"/> (full herostat folder).
        /// </summary>
        /// <returns>The un-normalized full herostat folder; representing "[OHS]\<see cref="Game"/>\<see cref="OHSsettings.HerostatFolder"/>", or <see cref="OHSsettings.HerostatFolder"/> if it's a rooted path. Defaults to normalized "[OHS]\<see cref="Game"/>\xml", if the folder doesn't exist.</returns>
        public static string HsFolder { get; set; } = Directory.CreateDirectory(GetRooted("xml")).FullName;
        /// <summary>
        /// Sets the path to the Herostat folder, creating a default "xml" folder if <see cref="OHSsettings.HerostatFolder"/> does not exist.
        /// </summary>
        /// <remarks>Exceptions: IO exceptions, if "xml" needs to be created (should never happen).</remarks>
        public static void SetHsFolder(string HerostatFolder)
        {
            string Folder = GetRooted(HerostatFolder);
            HsFolder = Directory.Exists(Folder) ? Folder : Directory.CreateDirectory(GetRooted("xml")).FullName;
            // CreateDirectory: fallback for the case that the user setting is invalid or the folder was removed (overhead)
        }
        /// <summary>
        /// Expand the <paramref name="Input"/> path to a full path within the OHS <see cref="Game"/> folder. <paramref name="Input"/> may be a full or relative path.
        /// </summary>
        /// <returns>Full normalized path to "[<see cref="CD"/> = OHS]\<see cref="Game"/>\<paramref name="Input"/>", or <paramref name="Input"/> if rooted.</returns>
        public static string GetRooted(string Input) => Path.Combine(CD, Game, Input);
        /// <summary>
        /// Expand the <paramref name="Input"/> paths to a full path within the OHS <see cref="Game"/> folder. <paramref name="Input"/> may be a full or relative path as a comma separated list.
        /// </summary>
        /// <returns>Full path to "[<see cref="CD"/> = OHS]\<see cref="Game"/>\<paramref name="Input"/>", or <paramref name="Input"/> if rooted.</returns>
        //public static string GetRooted(params IEnumerable<string> Input) => GetRooted(Join(Input));
        /// <summary>
        /// Gets the name of the specified <paramref name="path"/> string (incl. extension if it's a file path with extension).
        /// </summary>
        /// <returns>The characters after the last non-trailing directory separator character (or from the beginning) in <paramref name="path"/>. Trailing directory separator characters are trimmed.</returns>
        public static string GetName(string path) => Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        /// <summary>
        /// Get the parent directory information for the specified <paramref name="path"/>.
        /// </summary>
        /// <remarks>Effectively removes the last segment (the characters from the last non-trailing directory separator).</remarks>
        /// <returns>Normalized parent directory of <paramref name="path"/>, or <see langword="null"/> if <paramref name="path"/> denotes a root directory or is <see langword="null"/>.
        /// Returns <see cref="string.Empty"/> if <paramref name="path"/> does not contain directory information.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="PathTooLongException"/>
        public static string? GetParent(string? path)
        {
            return string.IsNullOrEmpty(path) ? null
                : Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        /// <summary>
        /// Combine the <paramref name="Input"/> path(s) to a <see cref="string"/>, adding a <see cref="Path.DirectorySeparatorChar"/> between each member.
        /// </summary>
        /// <returns>A relative path starting with the first <paramref name="Input"/> member (which may be rooted).</returns>
        //public static string Join(params IEnumerable<string> Input) => string.Join(Path.DirectorySeparatorChar Input
        //    .Select(static s => s.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        /// <summary>
        /// Get the folder name without the path to the OHS <see cref="Game"/> folder. E.g.: "C:\OHS\mua\herostats" to "herostats".
        /// </summary>
        /// <returns>The trimmed folder (path) or the same path, if it's not in the OHS <see cref="Game"/> folder</returns>
        public static string TrimGameFolder(string OldPath)
        {
            string G = Path.Combine(CD, Game);
            return OldPath.StartsWith(G, StringComparison.OrdinalIgnoreCase) ?
                OldPath[(G.Length + 1)..] :
                OldPath;
        }
        /// <summary>
        /// Gets the existing MO2 "mods" folder or mods folder of other organizers.
        /// </summary>
        /// <returns>Parent of <see cref="OHSsettings.GameInstallPath"/>, if it's not the game folder and exists and is named "mods" or <see cref="OHSsettings.ExeName"/> is an oranizer .exe or <see cref="GUIsettings.IsMo2"/>; otherwise <see langword="null"/>.</returns>
        public static string? ModsFolder => !File.Exists($"{CfgSt.OHS.GameInstallPath}/{DefaultExe}")
            && Directory.Exists(CfgSt.OHS.GameInstallPath)
            && GetParent(CfgSt.OHS.GameInstallPath) is string Mods
            && (InternalSettings.KnownModOrganizerExes.Contains(CfgSt.OHS.ExeName, StringComparer.OrdinalIgnoreCase)
            || Path.GetFileName(Mods) == "mods" || CfgSt.GUI.IsMo2)
            ? Mods
            : null;
        /// <summary>
        /// Get mod folders.
        /// </summary>
        /// <returns>Matching folders, if <see cref="ModsFolder"/> returns a valid organizer folder; otherwise an empty enumerable.</returns>
        /// <exception cref="IOException"/>
        /// <exception cref="UnauthorizedAccessException"/>
        /// <exception cref="System.Security.SecurityException"/>
        /// <exception cref="PathTooLongException"/> // also argument exceptions, if invalid characters, etc.
        public static IEnumerable<string> ModFolders => ModsFolder is string MO2 ? Directory.EnumerateDirectories(MO2) : [];
        /// <summary>
        /// Enumerates all directories with a package folder from the game folder and MO2 mod folders, according to the settings.
        /// </summary>
        /// <remarks>Note: <see cref="OHSsettings.GameInstallPath"/> can have a duplicate in <see cref="ModFolders"/>, but Distinct is not used to keep it lazy.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> with matching directories (can be empty).</returns>
        public static IEnumerable<string> FoldersWpkg => ((IEnumerable<string>)[CfgSt.OHS.GameInstallPath, GetActualGamePath])
            .Concat(ModFolders).Where(static d => Directory.Exists(Packages(d)));
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (from setting) using a <paramref name="RelativePath"/> (for both) and separate <paramref name="file" /> name. May fail silently.
        /// </summary>
        public static void CopyToGameRel(string SourceFolder, string RelativePath, string file) => CopyToGameRel(SourceFolder, RelativePath, file, file);
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (from setting) using a <paramref name="RelativePath"/> (for both) and <paramref name="Source" /> and <paramref name="Target" /> filenames to rename simultaneously. May fail silently.
        /// </summary>
        public static void CopyToGameRel(string SourceFolder, string RelativePath, string Source, string Target) => CopyFileToGame(Path.Combine(SourceFolder, RelativePath, Source), RelativePath, Target);
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (<paramref name="RelativePath"/> inside game folder from setting), using a separate <paramref name="file" /> name. May fail silently.
        /// </summary>
        public static void CopyToGame(string? SourceFolder, string RelativePath, string file)
        {
            if (!string.IsNullOrWhiteSpace(SourceFolder))
            {
                CopyFileToGame(Path.Combine(SourceFolder, file), RelativePath, file);
            }
        }
        /// <summary>
        /// Copy <paramref name="SourceFile" /> (full path) to game folder (<paramref name="RelativePath"/> inside game folder from setting), using a separate <paramref name="Target" /> filename to rename simultaneously. May fail silently.
        /// </summary>
        public static void CopyFileToGame(string SourceFile, string RelativePath, string Target)
        {
            try { File.Copy(SourceFile, Path.Combine(Directory.CreateDirectory($"{CfgSt.OHS.GameInstallPath}/{RelativePath}").FullName, Target), true); }
            catch { }
        }
        /// <summary>
        /// Recursively copy a complete <paramref name="Source"/> folder with all contents to a <paramref name="Target"/> path (paths must be normalized). Existing files are replaced.
        /// </summary>
        /// <returns><see langword="True"/>, if no exceptions occur; otherwise <see langword="false"/>.</returns>
        public static bool CopyFilesRecursively(string Source, string Target)
        {
            try
            {
                foreach (string dirPath in Directory.EnumerateDirectories(Source, "*", SearchOption.AllDirectories))
                {
                    _ = Directory.CreateDirectory(dirPath.Replace(Source, Target));
                }
                foreach (string SourceFile in Directory.EnumerateFiles(Source, "*", SearchOption.AllDirectories))
                {
                    File.Copy(SourceFile, SourceFile.Replace(Source, Target), true);
                }
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Find the actual folder containing the mod files in an existing <paramref name="ModPath"/> by checking the folder names.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{DirectoryInfo}"/> with all folders that match.</returns>
        public static IEnumerable<string> GetModSource(string ModPath)
        {
            return Path.GetDirectoryName(Directory.EnumerateDirectories(ModPath, "*", SearchOption.AllDirectories)
                .FirstOrDefault(static g => GameFolders.Contains(Path.GetFileName(g).ToLower()))) is string Mod
                ? Path.GetRelativePath(Mod, ModPath) == "."
                    ? [Mod]
                    : Directory.EnumerateDirectories(Path.GetDirectoryName(Mod)!)
                        .Where(static f => Directory.EnumerateDirectories(f).Any(static g => GameFolders.Contains(Path.GetFileName(g).ToLower())))
                : [];
        }
        /// <summary>
        /// Detect if GameInstallPath is the game folder or a mod folder. If it's a mod folder, prepare a new mod in <paramref name="Source"/> with <paramref name="TargetName"/> and optional <paramref name="InstallationFile"/> info.
        /// </summary>
        /// <remarks>Exceptions: System.IO exceptions (only if it's an MO2 mod folder without meta file).</remarks>
        /// <returns>The the target path either to game files or the new mod folder. Returns <see langword="null"/> if none detected.</returns>
        public static string? GetModTarget(string Source, string TargetName, string InstallationFile = "")
        {
            string Target = CfgSt.OHS.GameInstallPath;
            bool IsGIP = Path.IsPathFullyQualified(Target) && File.Exists($"{Target}/{DefaultExe}");
            if (!IsGIP && ModsFolder is string Mods)
            {
                Target = GetVacant(Path.Combine(Mods, TargetName));
                string MetaP = Path.Combine(Source, "meta.ini");
                if (!File.Exists(MetaP))
                {
                    string[] meta =
                    [
                        "[General]",
                        $"gameName={Game.PadRight(4, '1')}",
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
        /// Backup the "Save" folder in the game's save location. Performs a crash if Save folder can't be created.
        /// </summary>
        /// <returns><see langword="True"/>, if saves could be backed up, otherwise <see langword="false"/>.</returns>
        public static bool BackupSaves()
        {
            FileInfo Herostat = new(Path.Combine(CfgSt.OHS.GameInstallPath, "data", CfgSt.OHS.HerostatName));
            DateTime Date = Herostat.Exists
                ? Herostat.LastWriteTime
                : DateTime.Now;
            try
            {
                if (MoveSaves("Save", $"{Date:yyMMdd-HHmmss}") && Herostat.Exists)
                {
                    _ = Herostat.CopyTo(Path.Combine(SaveFolder, $"{Date:yyMMdd-HHmmss}", Herostat.Name), true);
                }
                _ = Directory.CreateDirectory($"{SaveFolder}/Save");
                return true;
            }
            catch { return false; }
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
