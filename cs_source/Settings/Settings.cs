using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.Pickers;
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI.Settings
{
    public partial class OHSsettings : ObservableRecipient
    {
        [ObservableProperty]
        private string rosterValue;
        [ObservableProperty]
        private string gameInstallPath;
        [ObservableProperty]
        private string exeName;
        [ObservableProperty]
        private string herostatName;
        [ObservableProperty]
        private string newGamePyName;
        [ObservableProperty]
        private string charactersHeadsPackageName;
        [ObservableProperty]
        private bool unlocker;
        [ObservableProperty]
        private bool launchGame;
        [ObservableProperty]
        private bool saveTempFiles;
        [ObservableProperty]
        private bool showProgress;
        [ObservableProperty]
        private bool debugMode;
        [ObservableProperty]
        private string herostatFolder;

        public static OHSsettings Instance { get; set; } = new();

        public OHSsettings()
        {
            rosterValue = "temp.OHSGUI";
            gameInstallPath = "";
            exeName = "";
            herostatName = "herostat.engb";
            newGamePyName = "new_game.py";
            charactersHeadsPackageName = "characters_heads.pkgb";
            unlocker = false;
            launchGame = false;
            saveTempFiles = false;
            showProgress = false;
            debugMode = false;
            herostatFolder = "xml";
        }
    }
    public partial class MUAsettings : OHSsettings
    {
        [ObservableProperty]
        private string menulocationsValue;
        [ObservableProperty]
        private bool rosterHack;
        // MUA mod pack settings
        [ObservableProperty]
        private string mannequinFolder;
        [ObservableProperty]
        private string charinfoName;

        public new static MUAsettings Instance { get; set; } = new();

        public MUAsettings()
        {
            menulocationsValue = "temp.OHSGUI";
            rosterHack = false;
            mannequinFolder = "mannequin";
            charinfoName = "charinfo.xmlb";
        }
    }
    public partial class XML2settings : OHSsettings
    {
        [ObservableProperty]
        private int rosterSize;
        [ObservableProperty]
        private bool unlockSkins;

        public new static XML2settings Instance { get; set; } = new();

        public XML2settings()
        {
            rosterSize = 21;
            unlockSkins = false;
        }
    }
    public partial class GUIsettings : ObservableRecipient
    {
        [ObservableProperty]
        private string gitHub;
        [ObservableProperty]
        private string versionDescription;
        [ObservableProperty]
        private int home;
        [ObservableProperty]
        private string game;
        [ObservableProperty]
        private bool freeSaves;
        [ObservableProperty]
        private string gameInstallPath;
        [ObservableProperty]
        private string exeArguments;
        [ObservableProperty]
        private int thumbnailWidth;
        // MUA specific settings
        [ObservableProperty]
        private string layout;
        [ObservableProperty]
        private string model;
        [ObservableProperty]
        private bool rowLayout;
        [ObservableProperty]
        private bool hidableEffectsOnly;
        [ObservableProperty]
        private bool copyStage;
        // XML2 specific settings
        [ObservableProperty]
        private bool skinDetailsVisible;

        public static GUIsettings Instance { get; set; } = new();

        public GUIsettings()
        {
            versionDescription = GetVersionDescription();
            gitHub = "https://github.com/ak2yny/OpenHeroSelectGUI";
            home = 0;
            game = "mua";
            // Note: XML2 seems to have an issue when the settings.dat is removed.
            freeSaves = false;
            gameInstallPath = "";
            exeArguments = "";
            thumbnailWidth = 224;
            layout = "25 Default PC 2006";
            model = "Default";
            rowLayout = false;
            hidableEffectsOnly = true;
            copyStage = true;
            skinDetailsVisible = false;
        }

        private static string GetVersionDescription()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version!;
            string? PreRelease = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
            PreRelease = PreRelease is string PR && PR.LastIndexOf('-') is int i && i > 0 ? PR[i..] : "";
            return $"{version.Major}.{version.Minor}.{version.Build}{PreRelease}";
            // Don't use .{version.Revision}
        }
    }
    /// <summary>
    /// The main configuration class. 
    /// </summary>
    public class Cfg
    {
        public OHSsettings OHS { get; set; } = OHSsettings.Instance;
        public MUAsettings MUA { get; set; } = MUAsettings.Instance;
        public XML2settings XML2 { get; set; } = XML2settings.Instance;
        public GUIsettings GUI { get; set; } = GUIsettings.Instance;
        public DynamicSettings Dynamic { get; set; } = DynamicSettings.Instance;
        public CharacterLists Roster { get; set; } = CharacterLists.Instance;
        public SkinData Skins { get; set; } = SkinData.Instance;

    }
    public class CfgCommands
    {
        /// <summary>
        /// Open file dialogue.
        /// </summary>
        public static async Task<string?> LoadDialogue(string format)
        {
            FileOpenPicker filePicker = new();
            filePicker.FileTypeFilter.Add(format);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
            StorageFile file = await filePicker.PickSingleFileAsync();
            return file?.Path;
        }
        /// <summary>
        /// Open folder dialogue.
        /// </summary>
        public static async Task<string> BrowseFolder()
        {
            FolderPicker folderPicker = new();
            folderPicker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            return folder is null ? "" : folder.Path;
        }
        /// <summary>
        /// Get the folder name without the path to the OHS game folder.
        /// </summary>
        /// <returns>The trimmed folder (path). Ex: "C:\OHS\mua\herostats" to "herostats"</returns>
        public static string TrimGameFolder(string OldPath)
        {
            string Game = Path.Combine(cdPath, GUIsettings.Instance.Game);
            return OldPath.StartsWith(Game) ?
                OldPath[(Game.Length + 1)..] :
                OldPath;
        }
        /// <summary>
        /// Load OHS settings in JSON & GUI settings in XML by providing the path to the OHS .ini file. Assumes the same name with a "_GUI" suffix for the GUI .xml file.
        /// </summary>
        public static void LoadSettings(string Oini)
        {
            LoadGuiSettings(Oini.Remove(Oini.LastIndexOf(".")) + "_GUI.xml");
            LoadOHSsettings(Oini);
        }
        /// <summary>
        /// Load GUI settings from the default XML file.
        /// </summary>
        public static void LoadGuiSettings() => LoadGuiSettings(Path.Combine(cdPath, "config.xml"));
        /// <summary>
        /// Load GUI settings from an XML file.
        /// </summary>
        private static void LoadGuiSettings(string Gini)
        {
            if (File.Exists(Gini))
            {
                XmlSerializer XS = new(typeof(GUIsettings));
                using FileStream fs = File.Open(Gini, FileMode.Open);
                GUIsettings.Instance = (GUIsettings)XS.Deserialize(fs);
            }
        }
        /// <summary>
        /// Load OHS JSON data by providing a path & load the roster according to its settings. This assumes that the file is for the currently active game tab.
        /// </summary>
        public static void LoadOHSsettings(string Oini)
        {
            if (File.Exists(Oini))
            {
                // Important note: Json deserialization doesn't activate the property changed event, which `OHSsettings.Instance.ExeName = "Game.exe";` does (for example). OHS settings (all 3) need to use the static instance therefore, except in Xaml, where the instances all are updated anyway.
                JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
                OHSsettings.Instance = GUIsettings.Instance.Game == "xml2"
                    ? (XML2settings.Instance = JsonSerializer.Deserialize<XML2settings>(File.ReadAllText(Oini), options)!)
                    : (MUAsettings.Instance = JsonSerializer.Deserialize<MUAsettings>(File.ReadAllText(Oini), options)!);
                OHSsettings.Instance.RosterValue = FilterDefaultRV(OHSsettings.Instance.RosterValue);
                MUAsettings.Instance.MenulocationsValue = FilterDefaultMV(MUAsettings.Instance.MenulocationsValue);
                CharacterListCommands.LoadRosterVal();
            }
        }
        /// <summary>
        /// Save OHS settings in JSON & GUI settings in XML to the default location. Saves CFG files according to these settings.
        /// </summary>
        public static void SaveSettings()
        {
            SaveIniXml(Path.Combine(cdPath, GUIsettings.Instance.Game, "config.ini"), Path.Combine(cdPath, "config.xml"));
            GenerateCfgFiles(OHSsettings.Instance.RosterValue, MUAsettings.Instance.MenulocationsValue);
        }

        /// <summary>
        /// Save OHS settings in JSON & GUI settings in XML by providing a path. Saves CFG files to the second argument value.
        /// </summary>
        public static void SaveSettings(string Oini, string rv)
        {
            SaveIniXml(Oini, Oini.Remove(Oini.LastIndexOf(".")) + "_GUI.xml");
            GenerateCfgFiles(rv, rv);
        }

        /// <summary>
        /// Save OHS settings in JSON & GUI settings in XML by providing both paths.
        /// </summary>
        private static void SaveIniXml(string Oini, string Gini)
        {
            // JSON
            string? Opath = Path.GetDirectoryName(Oini);
            if (string.IsNullOrEmpty(Opath)) return;
            _ = Directory.CreateDirectory(Opath);
            var JsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string JsonString = (GUIsettings.Instance.Game == "xml2") ?
                    JsonSerializer.Serialize(XML2settings.Instance, JsonOptions) :
                    JsonSerializer.Serialize(MUAsettings.Instance, JsonOptions);
            // Using the MVVM toolkit generates an IsActive property which gets serialized, unfortunately. I didn't find a solution, yet.
            string[] JsonSplit = JsonString.Split(Environment.NewLine).Where(l => !l.Contains("\"isActive\":")).ToArray();
            JsonSplit[^2] = JsonSplit[^2].TrimEnd().TrimEnd(',');
            File.WriteAllText(Oini, string.Join(Environment.NewLine, JsonSplit));

            // XML
            XmlSerializer XS = new(typeof(GUIsettings));
            using FileStream fs = File.Open(Gini, FileMode.Create);
            XS.Serialize(fs, GUIsettings.Instance);
        }
        /// <summary>
        /// Generate the cfg files providing independent menulocation and roster values (filenames).
        /// </summary>
        private static void GenerateCfgFiles(string rv, string mv)
        {
            if (CharacterLists.Instance.Selected.Count > 0)
            {
                if (GUIsettings.Instance.Game == "mua")
                {
                    WriteCfg(Path.Combine(cdPath, "mua", "menulocations", $"{mv}.cfg"), 0);
                }
                WriteCfg(Path.Combine(cdPath, GUIsettings.Instance.Game, "rosters", $"{rv}.cfg"), 2);
            }
        }
        /// <summary>
        /// Write the OHS cfg files from the selected characters list.
        /// </summary>
        private static void WriteCfg(string path, int p)
        {
            string? CFGpath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(CFGpath)) return;
            _ = Directory.CreateDirectory(CFGpath);
            File.Create(path).Dispose();
            using StreamWriter sw = File.AppendText(path);
            for (int i = 0; i < CharacterLists.Instance.Selected.Count; i++)
            {
                SelectedCharacter c = CharacterLists.Instance.Selected[i];
                string? line = (p == 0) ?
                    c.Loc :
                    c.Starter ? $"*{c.Path}" : c.Unlock ? $"?{c.Path}" : c.Path;
                sw.WriteLine(line);
            }
        }
        // Copy files to the game directory by specifying the game internal path and a source file. They all work similar, description WIP.
        public static void CopyGameFile(string folder, string GameFolder, string file) => CopyGameFile(folder, GameFolder, file, file);
        public static void CopyGameFile(string folder, string GameFolder, string Source, string Target)
        {
            string SF = Path.Combine(folder, Source);
            if (folder != "" && OHSsettings.Instance.GameInstallPath != "" && File.Exists(SF))
            {
                DirectoryInfo TP = Directory.CreateDirectory(Path.Combine(OHSsettings.Instance.GameInstallPath, GameFolder));
                File.Copy(SF, Path.Combine(TP.FullName, Target), true);
            }
        }
        public static void CopyGameFileWStrctr(string MainPath, string GameFolders, string Source, string Target) => CopyGameFile(Path.Combine(MainPath, GameFolders), GameFolders, Source, Target);
        public static void CopyGameFileWStrctr(string MainPath, string GameFolders, string file) => CopyGameFile(Path.Combine(MainPath, GameFolders), GameFolders, file, file);
        /// <summary>
        /// Browse for an IGB file and install it to the target path and (name without extension).
        /// </summary>
        /// <returns>True if a file was picked, false if there was no file picked and installed.</returns>
        public static async Task<bool> InstallIGBFiles(string GamePath, string TargetName)
        {
            string? IGB = await LoadDialogue(".igb");
            if (IGB is null) return false;
            CopyGameFile(Path.GetDirectoryName(IGB)!, GamePath, Path.GetFileName(IGB), $"{TargetName}.igb");
            return true;
        }
        private static string FilterDefaultRV(string RV)
        {
            return (new[] { "36 Roster Hack Base Roster (Gold Edition Stage)",
                            "36 Roster Hack Base Roster (v1.0 and 1.5)",
                            "36 Roster Hack Base Roster (v2.0 and later)",
                            "50 Roster Hack Base Roster",
                            "Default 25 Character Roster",
                            "Default 28 Character (PSP) Roster",
                            "Default 33 Character (DLC, Gold) Roster",
                            "Official Characters Pack Base Roster",
                            "Default 18 Character (GC, PS2, Xbox) Roster",
                            "Default 20 Character (PC) Roster",
                            "Default 22 Character (PSP) Roster" })
                .Any(r => r.Equals(RV, StringComparison.OrdinalIgnoreCase))
                ? "temp.OHSGUI"
                : RV;
        }
        private static string FilterDefaultMV(string MV)
        {
            return (new[] { "25 (Default Base Game)",
                            "27 (Official Characters Pack)",
                            "28 (Default PSP)", "28 (for 28 Roster Hack)",
                            "33 (Default DLC, Gold Edition)",
                            "33 (OCP 2 Gold Edition Stage)",
                            "36 (for 36RH Gold Edition Stage)",
                            "36 (for 36RH v1.0 or 1.5 Stage)",
                            "36 (for 36RH v2.0 or later Stage)",
                            "50 (for 50RH Stage)" })
                .Any(m => m.Equals(MV, StringComparison.OrdinalIgnoreCase))
                ? "temp.OHSGUI"
                : MV;
        }
        /// <summary>
        /// Get the saves folder for the current tab (game).
        /// </summary>
        /// <returns>Save folder in documents for the correct game.</returns>
        public static string GetSaveFolder()
        {
            string Game = (GUIsettings.Instance.Game == "xml2") ?
                "X-Men Legends 2" :
                "Marvel Ultimate Alliance";
            return Path.Combine(Activision, Game);
        }
        /// <summary>
        /// Move folders in the game's save location by providing names.
        /// </summary>
        public static void MoveSaves(string From, string To)
        {
            string SaveFolder = GetSaveFolder();
            DirectoryInfo Source = new(Path.Combine(SaveFolder, From));
            if (!Source.Exists || !Source.EnumerateFiles().Any()) return;
            string Target = Path.Combine(SaveFolder, To);
            Source.MoveTo(Target);
        }
        /// <summary>
        /// Run the exe of the provided name that's in the provided path, using the provided arguments.
        /// </summary>
        public static void RunGame(string GamePath, string Exe, string Args)
        {
            if (GUIsettings.Instance.FreeSaves) MoveSaves("Save", $"{DateTime.Now:yyMMdd-HHmmss}");
            ProcessStartInfo Game = new()
            {
                WorkingDirectory = Path.Combine(GamePath),
                Arguments = Args,
                FileName = Path.Combine(GamePath, Exe)
            };
            _ = Process.Start(Game);
        }
    }
}