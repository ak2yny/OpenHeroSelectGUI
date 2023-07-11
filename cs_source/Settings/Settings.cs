using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
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
        private bool freeSaves;
        // MUA specific settings
        [ObservableProperty]
        private string layout;
        [ObservableProperty]
        private string model;
        [ObservableProperty]
        private bool rowLayout;

        public static GUIsettings Instance { get; set; } = new();

        public GUIsettings()
        {
            versionDescription = GetVersionDescription();
            gitHub = "https://github.com/ak2yny/OpenHeroSelectGUI";
            home = 0;
            freeSaves = true;
            layout = "25 Default PC 2006";
            model = "Default";
            rowLayout = false;
        }

        private static string GetVersionDescription()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version!;
            string? PreRelease = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
            if (PreRelease is string PR && PR.LastIndexOf('-') is int i && i > 0)
            {
                PreRelease = PR[i..];
            }
            else
            {
                PreRelease = "";
            }
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
            Window window = new();
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, WinRT.Interop.WindowNative.GetWindowHandle(window));
            StorageFile file = await filePicker.PickSingleFileAsync();
            return file?.Path;
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
                // WIP: There might be a different home defined in the settings, but automatic navigation on load does not happen... intentionally.?
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
                OHSsettings.Instance = DynamicSettings.Instance.Game == "xml2"
                    ? (XML2settings.Instance = JsonSerializer.Deserialize<XML2settings>(File.ReadAllText(Oini), options)!)
                    : (MUAsettings.Instance = JsonSerializer.Deserialize<MUAsettings>(File.ReadAllText(Oini), options)!);
                CharacterListCommands.LoadRosterVal();
            }
        }
        /// <summary>
        /// Save OHS settings in JSON & GUI settings in XML to the default location. Saves CFG files according to these settings.
        /// </summary>
        public static void SaveSettings()
        {
            SaveIniXml(Path.Combine(cdPath, DynamicSettings.Instance.Game, "config.ini"), Path.Combine(cdPath, "config.xml"));
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
            Directory.CreateDirectory(Opath);
            var JsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string JsonString = (DynamicSettings.Instance.Game == "xml2") ?
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
            if (CharacterLists.Instance.Selected != null && CharacterLists.Instance.Selected.Count > 0)
            {
                if (DynamicSettings.Instance.Game == "mua" && !IsDefaultMV(mv))
                {
                    WriteCfg(Path.Combine(cdPath, "mua", "menulocations", $"{mv}.cfg"), 0);
                }
                if (!IsDefaultRV(rv))
                {
                    WriteCfg(Path.Combine(cdPath, DynamicSettings.Instance.Game, "rosters", $"{rv}.cfg"), 2);
                }
            }
        }
        /// <summary>
        /// Write the OHS cfg files from the selected characters list.
        /// </summary>
        private static void WriteCfg(string path, int p)
        {
            string? CFGpath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(CFGpath)) return;
            Directory.CreateDirectory(CFGpath);
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
        private static bool IsDefaultRV(string RV)
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
                .Any(r => r.Equals(RV, StringComparison.OrdinalIgnoreCase));
        }
        private static bool IsDefaultMV(string MV)
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
                .Any(m => m.Equals(MV, StringComparison.OrdinalIgnoreCase));
        }
    }
}