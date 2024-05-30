using CommunityToolkit.Mvvm.ComponentModel;
using OpenHeroSelectGUI.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.Pickers;

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
        private int showClashes;
        [ObservableProperty]
        private bool modPack;
        [ObservableProperty]
        private bool availChars;
        public List<string> StageFavourites = [];
        // MUA specific settings
        [ObservableProperty]
        private string gameInstallPath;
        [ObservableProperty]
        private string actualGameExe;
        [ObservableProperty]
        private string exeArguments;
        [ObservableProperty]
        private string teamBonusName;
        [ObservableProperty]
        private string layout;
        [ObservableProperty]
        private string model;
        [ObservableProperty]
        private int thumbnailWidth;
        [ObservableProperty]
        private bool stageFavouritesOn;
        [ObservableProperty]
        private bool rowLayout;
        [ObservableProperty]
        private bool layoutWidthUpscale;
        [ObservableProperty]
        private int layoutMaxWidth;
        [ObservableProperty]
        private bool hidableEffectsOnly;
        [ObservableProperty]
        private bool copyStage;
        // XML2 specific settings
        [ObservableProperty]
        private string xml2InstallPath;
        [ObservableProperty]
        private string xml2Arguments;
        [ObservableProperty]
        private string xml2BonusName;
        [ObservableProperty]
        private bool skinDetailsVisible;

        public GUIsettings()
        {
            versionDescription = GetVersionDescription();
            gitHub = "https://github.com/ak2yny/OpenHeroSelectGUI";
            home = 0;
            game = "mua";
            // Note: XML2 seems to have an issue when the settings.dat is removed.
            freeSaves = true;
            gameInstallPath = xml2InstallPath = actualGameExe = exeArguments = xml2Arguments = "";
            teamBonusName = xml2BonusName = "team_bonus";
            layout = "25 Default PC 2006";
            model = "Default";
            thumbnailWidth = 224;
            rowLayout = false;
            layoutWidthUpscale = false;
            layoutMaxWidth = 1000;
            hidableEffectsOnly = true;
            copyStage = true;
            skinDetailsVisible = false;
        }

        private static string GetVersionDescription()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version!;
            string PreRelease = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion is string PR
                                && PR.LastIndexOf('-') is int i
                                && i > 0 ? PR[i..] : "";
            return $"{version.Major}.{version.Minor}.{version.Build}{PreRelease}";
            // Don't use .{version.Revision}
        }
    }
    /// <summary>
    /// The main static configuration class. 
    /// </summary>
    public class CfgSt
    {
        public static OHSsettings OHS { get; set; } = new();
        public static MUAsettings MUA { get; set; } = new();
        public static XML2settings XML2 { get; set; } = new();
        public static GUIsettings GUI { get; set; } = new();
        public static VariableSettings Var { get; set; } = new();
        public static CharacterLists Roster { get; set; } = new();

    }
    /// <summary>
    /// The main non-static configuration class. 
    /// </summary>
    public class Cfg
    {
        public OHSsettings OHS { get; set; } = CfgSt.OHS;
        public MUAsettings MUA { get; set; } = CfgSt.MUA;
        public XML2settings XML2 { get; set; } = CfgSt.XML2;
        public GUIsettings GUI { get; set; } = CfgSt.GUI;
        public VariableSettings Var { get; set; } = CfgSt.Var;
        public CharacterLists Roster { get; set; } = CfgSt.Roster;

    }
    /// <summary>
    /// Functions related to settings (save and load)
    /// </summary>
    public class CfgCmd
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private static readonly JsonSerializerOptions JsonOptionsD = new() { PropertyNameCaseInsensitive = true };

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
        /// Load OHS settings in JSON &amp; GUI settings in XML by providing the path to the OHS .ini (<paramref name="Oini"/>) file. Assumes names for the GUI .xml file and team_bonus file as saved through the GUI.
        /// </summary>
        public static void LoadSettings(string Oini)
        {
            LoadGuiSettings($"{Oini.Remove(Oini.LastIndexOf('.'))}_GUI.xml");
            LoadOHSsettings(Oini);
            try
            {
                FileInfo TeamBonus = new($"{Oini.Remove(Oini.LastIndexOf('.'))}_team_bonus.xml");
                if (TeamBonus.Exists) { _ = TeamBonus.CopyTo(OHSpath.Team_bonus, true); }
            }
            catch 
            {
                // Skippin team bonus, if fails
            }
        }
        /// <summary>
        /// Load GUI settings from the default XML file.
        /// </summary>
        public static void LoadGuiSettings() => LoadGuiSettings(Path.Combine(OHSpath.CD, "config.xml"));
        /// <summary>
        /// Load GUI settings from an XML file (<paramref name="Gini"/>).
        /// </summary>
        private static void LoadGuiSettings(string Gini)
        {
            if (File.Exists(Gini))
            {
                XmlSerializer XS = new(typeof(GUIsettings));
                using FileStream fs = new(Gini, FileMode.Open, FileAccess.Read);
                if (XS.Deserialize(fs) is GUIsettings CfgGUI) { CfgSt.GUI = CfgGUI; }
            }
        }
        /// <summary>
        /// Load OHS JSON data by providing a path (<paramref name="Oini"/>) &amp; load the roster according to its settings. This assumes that the file is for the currently active game tab.
        /// </summary>
        public static void LoadOHSsettings(string Oini)
        {
            if (File.Exists(Oini))
            {
                try
                {
                    CfgSt.OHS = CfgSt.GUI.Game == "xml2"
                        ? (CfgSt.XML2 = JsonSerializer.Deserialize<XML2settings>(File.ReadAllText(Oini), JsonOptionsD)!)
                        : (CfgSt.MUA = JsonSerializer.Deserialize<MUAsettings>(File.ReadAllText(Oini), JsonOptionsD)!);
                    CfgSt.OHS.RosterValue = FilterDefaultRV(CfgSt.OHS.RosterValue);
                    CfgSt.MUA.MenulocationsValue = FilterDefaultMV(CfgSt.MUA.MenulocationsValue);
                }
                finally
                {
                    CharacterListCommands.LoadRosterVal();
                }
            }
        }
        /// <summary>
        /// Save OHS settings in JSON &amp; GUI settings in XML to the default location. Saves CFG files according to these settings. Use default names if mod pack setting is disabled.
        /// </summary>
        public static void SaveSettingsMP()
        {
            // ExeName not to default, team_bonus handled separately
            SaveIniXml(OHSpath.GetRooted("config.ini"), Path.Combine(OHSpath.CD, "config.xml"), CfgSt.GUI.ModPack
                ? CfgSt.GUI.Game == "xml2" ? CfgSt.XML2 : CfgSt.MUA
                : CfgSt.GUI.Game == "xml2"
                ?
                new XML2settings()
                {
                    RosterSize = CfgSt.XML2.RosterSize,
                    UnlockSkins = CfgSt.XML2.UnlockSkins,
                    RosterValue = CfgSt.OHS.RosterValue,
                    GameInstallPath = CfgSt.OHS.GameInstallPath,
                    ExeName = CfgSt.OHS.ExeName,
                    HerostatFolder = CfgSt.OHS.HerostatFolder,
                    Unlocker = CfgSt.OHS.Unlocker,
                    LaunchGame = CfgSt.OHS.LaunchGame,
                    SaveTempFiles = CfgSt.OHS.SaveTempFiles,
                    ShowProgress = CfgSt.OHS.ShowProgress,
                    DebugMode = CfgSt.OHS.DebugMode
                }
                :
                new MUAsettings()
                {
                    MenulocationsValue = CfgSt.MUA.MenulocationsValue,
                    RosterHack = CfgSt.MUA.RosterHack,
                    RosterValue = CfgSt.OHS.RosterValue,
                    GameInstallPath = CfgSt.OHS.GameInstallPath,
                    ExeName = CfgSt.OHS.ExeName,
                    HerostatFolder = CfgSt.OHS.HerostatFolder,
                    Unlocker = CfgSt.OHS.Unlocker,
                    LaunchGame = CfgSt.OHS.LaunchGame,
                    SaveTempFiles = CfgSt.OHS.SaveTempFiles,
                    ShowProgress = CfgSt.OHS.ShowProgress,
                    DebugMode = CfgSt.OHS.DebugMode
                });
            GenerateCfgFiles(CfgSt.OHS.RosterValue, CfgSt.MUA.MenulocationsValue);
        }
        /// <summary>
        /// Save OHS settings in JSON &amp; GUI settings in XML to the default location. Saves CFG files according to these settings.
        /// </summary>
        public static void SaveSettings()
        {
            SaveIniXml(OHSpath.GetRooted("config.ini"), Path.Combine(OHSpath.CD, "config.xml"));
            GenerateCfgFiles(CfgSt.OHS.RosterValue, CfgSt.MUA.MenulocationsValue);
        }
        /// <summary>
        /// Save OHS settings in JSON &amp; GUI settings in XML by providing a path (<paramref name="Oini"/>). Saves roster and menulocations to <paramref name="rv"/>.cfg files.
        /// </summary>
        public static void SaveSettings(string Oini, string rv)
        {
            SaveIniXml(Oini, $"{Oini.Remove(Oini.LastIndexOf('.'))}_GUI.xml");
            GenerateCfgFiles(rv, rv);
            _ = MarvelModsXML.TeamBonusSerializer($"{Oini.Remove(Oini.LastIndexOf('.'))}_team_bonus.xml");
        }
        /// <summary>
        /// Save OHS settings in JSON (<paramref name="Oini"/>) &amp; GUI settings in XML (<paramref name="Gini"/>) by providing both paths.
        /// </summary>
        private static void SaveIniXml(string Oini, string Gini) => SaveIniXml(Oini, Gini, (CfgSt.GUI.Game == "xml2") ? CfgSt.XML2 : CfgSt.MUA);
        /// <summary>
        /// Save OHS settings in JSON (<paramref name="Oini"/>) &amp; GUI settings in XML (<paramref name="Gini"/>) by providing both paths and the <paramref name="GameCfg"/> class.
        /// </summary>
        private static void SaveIniXml(string Oini, string Gini, object GameCfg)
        {
            // IMPORTANT: This makes the app crash, if the files can't be written. That's intentional to let the user know that something is wrong.

            // JSON
            string? Opath = Path.GetDirectoryName(Oini);
            if (string.IsNullOrEmpty(Opath)) return;
            _ = Directory.CreateDirectory(Opath);
            string JsonString = JsonSerializer.Serialize(GameCfg, JsonOptions);
            // Using the MVVM toolkit generates an IsActive property which gets serialized, unfortunately. I didn't find a solution, yet.
            string[] JsonSplit = JsonString.Split(Environment.NewLine).Where(l => !l.Contains("\"isActive\":")).ToArray();
            JsonSplit[^2] = JsonSplit[^2].TrimEnd().TrimEnd(',');
            File.WriteAllLines(Oini, JsonSplit);

            // XML
            XmlSerializer XS = new(typeof(GUIsettings));
            using FileStream fs = File.Open(Gini, FileMode.Create);
            XS.Serialize(fs, CfgSt.GUI);
        }
        /// <summary>
        /// Generate the roster <paramref name="rv"/>.cfg and menulocation <paramref name="mv"/>.cfg files in the OHS game folders.
        /// </summary>
        private static void GenerateCfgFiles(string rv, string mv)
        {
            if (CfgSt.Roster.Selected.Count > 0)
            {
                if (CfgSt.GUI.Game == "mua")
                {
                    WriteCfg(Path.Combine(OHSpath.CD, "mua", "menulocations", $"{mv}.cfg"), 0);
                }
                WriteCfg(OHSpath.GetRooted("rosters", $"{rv}.cfg"), 2);
            }
        }
        /// <summary>
        /// Write a OHS CFG file to <paramref name="path"/> from the <see cref="CfgSt.Roster.Selected"/> list. Performs a crash on write exceptions.
        /// </summary>
        private static void WriteCfg(string path, int p)
        {
            if (Path.GetDirectoryName(path) is string CFGpath && CFGpath != "")
            {
                _ = Directory.CreateDirectory(CFGpath);
                File.Create(path).Dispose();
                using StreamWriter sw = File.AppendText(path);
                for (int i = 0; i < CfgSt.Roster.Selected.Count; i++)
                {
                    SelectedCharacter c = CfgSt.Roster.Selected[i];
                    string? line = p == 0 ?
                        c.Loc :
                        c.Starter ? $"*{c.Path}" : c.Unlock ? $"?{c.Path}" : c.Path;
                    sw.WriteLine(line);
                }
            }
        }

        private static readonly string[] DefaultRV = [
            "36 Roster Hack Base Roster (Gold Edition Stage)",
            "36 Roster Hack Base Roster (v1.0 and 1.5)",
            "36 Roster Hack Base Roster (v2.0 and later)",
            "50 Roster Hack Base Roster",
            "Default 25 Character Roster",
            "Default 28 Character (PSP) Roster",
            "Default 33 Character (DLC, Gold) Roster",
            "Official Characters Pack Base Roster",
            "Default 18 Character (GC, PS2, Xbox) Roster",
            "Default 20 Character (PC) Roster",
            "Default 22 Character (PSP) Roster" ];

        private static readonly string[] DefaultMV = [
            "25 (Default Base Game)",
            "27 (Official Characters Pack)",
            "28 (Default PSP)",
            "28 (for 28 Roster Hack)",
            "33 (Default DLC, Gold Edition)",
            "33 (OCP 2 Gold Edition Stage)",
            "36 (for 36RH Gold Edition Stage)",
            "36 (for 36RH v1.0 or 1.5 Stage)",
            "36 (for 36RH v2.0 or later Stage)",
            "50 (for 50RH Stage)" ];

        private static string FilterDefaultRV(string RV)
        {
            return DefaultRV.Any(r => r.Equals(RV, StringComparison.OrdinalIgnoreCase))
                ? "temp.OHSGUI"
                : RV;
        }

        private static string FilterDefaultMV(string MV)
        {
            return DefaultMV.Any(m => m.Equals(MV, StringComparison.OrdinalIgnoreCase))
                ? "temp.OHSGUI"
                : MV;
        }
    }
}