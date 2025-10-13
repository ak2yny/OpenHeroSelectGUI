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
using Zsnd_UI.lib;

namespace OpenHeroSelectGUI.Settings
{
    public partial class OHSsettings : ObservableObject
    {
        [ObservableProperty]
        public partial string RosterValue { get; set; }
        [ObservableProperty]
        public partial string GameInstallPath { get; set; }
        [ObservableProperty]
        public partial string ExeName { get; set; }
        [ObservableProperty]
        public partial string HerostatName { get; set; }
        [ObservableProperty]
        public partial string NewGamePyName { get; set; }
        [ObservableProperty]
        public partial string CharactersHeadsPackageName { get; set; }
        [ObservableProperty]
        public partial bool Unlocker { get; set; }
        [ObservableProperty]
        public partial bool LaunchGame { get; set; }
        [ObservableProperty]
        public partial bool SaveTempFiles { get; set; }
        [ObservableProperty]
        public partial bool ShowProgress { get; set; }
        [ObservableProperty]
        public partial bool DebugMode { get; set; }
        [ObservableProperty]
        public partial string HerostatFolder { get; set; }

        public OHSsettings()
        {
            RosterValue = "temp.OHSGUI";
            GameInstallPath = ExeName = "";
            HerostatName = "herostat.engb";
            NewGamePyName = "new_game.py";
            CharactersHeadsPackageName = "characters_heads.pkgb";
            Unlocker = LaunchGame = SaveTempFiles = ShowProgress = DebugMode = false;
            HerostatFolder = "xml";
        }
    }
    public partial class MUAsettings : OHSsettings
    {
        [ObservableProperty]
        public partial string MenulocationsValue { get; set; }
        [ObservableProperty]
        public partial bool RosterHack { get; set; }
        // MUA mod pack settings
        [ObservableProperty]
        public partial string MannequinFolder { get; set; }
        [ObservableProperty]
        public partial string CharinfoName { get; set; }

        public MUAsettings()
        {
            MenulocationsValue = "temp.OHSGUI";
            RosterHack = false;
            MannequinFolder = "mannequin";
            CharinfoName = "charinfo.xmlb";
        }
    }
    public partial class XML2settings : OHSsettings
    {
        [ObservableProperty]
        public partial int RosterSize { get; set; }
        [ObservableProperty]
        public partial bool UnlockSkins { get; set; }

        public XML2settings()
        {
            RosterSize = 21;
            UnlockSkins = false;
        }
    }
    public partial class GUIsettings : ObservableObject
    {
        [ObservableProperty]
        public partial string GitHub { get; set; }
        [ObservableProperty]
        public partial string VersionDescription { get; set; }
        [ObservableProperty]
        public partial string Home { get; set; }
        [ObservableProperty]
        public partial string Game { get; set; }
        [ObservableProperty]
        public partial bool IsMo2 { get; set; }
        [ObservableProperty]
        public partial bool AvailChars { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        public partial string GameInstallPath { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        public partial string ExeArguments { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        public partial bool FreeSaves { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        public partial bool ShowClashes { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        public partial bool ModPack { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        public partial string TeamBonusName { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        public partial string SkinModName { get; set; }
        // MUA specific settings
        public string MuaInstallPath;
        public string MuaArguments;
        public bool MuaFreeSaves;
        public bool MuaShowClashes;
        public bool MuaModPack;
        public string MuaTeamBonusName;
        public string MuaSkinModName;
        [ObservableProperty]
        public partial string ActualGameExe { get; set; }
        [ObservableProperty]
        public partial string Layout { get; set; }
        [ObservableProperty]
        public partial string Model { get; set; }
        [ObservableProperty]
        public partial int ThumbnailWidth { get; set; }
        [ObservableProperty]
        public partial bool StageInfoTransparency { get; set; }
        [ObservableProperty]
        public partial bool StageFavouritesOn { get; set; }
        public List<string> StageFavourites = [];
        [ObservableProperty]
        public partial bool RowLayout { get; set; }
        [ObservableProperty]
        public partial bool LayoutWidthUpscale { get; set; }
        [ObservableProperty]
        public partial int LayoutMaxWidth { get; set; }
        [ObservableProperty]
        public partial bool HidableEffectsOnly { get; set; }
        [ObservableProperty]
        public partial bool CopyStage { get; set; }
        [ObservableProperty]
        public partial bool IsNotConsole { get; set; }
        // XML2 specific settings
        public string Xml2InstallPath;
        public string Xml2Arguments;
        public bool Xml2FreeSaves;
        public bool Xml2ShowClashes;
        public bool Xml2ModPack;
        public string Xml2TeamBonusName;
        public string Xml2SkinModName;
        [ObservableProperty]
        public partial bool SkinDetailsVisible { get; set; }
        [ObservableProperty]
        public partial bool SkinsDragEnabled { get; set; }

        public GUIsettings()
        {
            VersionDescription = CfgCmd.GetVersionDescription();
            GitHub = "https://github.com/ak2yny/OpenHeroSelectGUI";
            // Note: XML2 seems to have an issue when the settings.dat is removed.
            MuaFreeSaves = HidableEffectsOnly = CopyStage = AvailChars = IsNotConsole = true;
            Game = Home = GameInstallPath = ActualGameExe = ExeArguments = TeamBonusName = SkinModName = MuaInstallPath = MuaArguments = MuaTeamBonusName = Xml2InstallPath = Xml2Arguments = Xml2TeamBonusName = Xml2SkinModName = MuaSkinModName = "";
            Layout = "25 Default PC 2006";
            Model = "Default";
            ThumbnailWidth = 224;
            LayoutMaxWidth = 1000;
        }

        partial void OnGameChanged(string value)
        {
            if (CfgSt.Var is not null)
            {
                bool IsXML2 = value == "XML2";
                GameInstallPath = (IsXML2 ? Xml2InstallPath : MuaInstallPath) ?? "";
                ExeArguments = (IsXML2 ? Xml2Arguments : MuaArguments) ?? "";
                FreeSaves = IsXML2 ? Xml2FreeSaves : MuaFreeSaves;
                ShowClashes = IsXML2 ? Xml2ShowClashes : MuaShowClashes;
                ModPack = IsXML2 ? Xml2ModPack : MuaModPack;
                TeamBonusName = (IsXML2 ? Xml2TeamBonusName : MuaTeamBonusName) is string TB && TB.Length > 0 ? TB : "team_bonus";
                SkinModName = (IsXML2 ? Xml2SkinModName : MuaSkinModName) ?? "";
                CfgSt.Var.IsMua = !(CfgSt.Var.IsXml2 = IsXML2);
                CfgSt.Roster.Teams.Clear();
                ZsndLists.Sounds.Clear();
            }
        }

        partial void OnTeamBonusNameChanged(string value) => SetGameSpecific(value, "TeamBonusName");
        partial void OnGameInstallPathChanged(string value) => SetGameSpecific(value, "InstallPath");
        partial void OnExeArgumentsChanged(string value) => SetGameSpecific(value, "Arguments");
        partial void OnSkinModNameChanged(string value) => SetGameSpecific(value, "SkinModName");
        partial void OnFreeSavesChanged(bool value) => SetGameSpecific(value, "FreeSaves");
        partial void OnShowClashesChanged(bool value) => SetGameSpecific(value, "ShowClashes");
        partial void OnModPackChanged(bool value) => SetGameSpecific(value, "ModPack");

        protected void SetGameSpecific(object value, string name)
        {
            GetType().GetField($"{(Game == "XML2" ? "Xml2" : "Mua")}{name}")?.SetValue(this, value);
        }
    }
    /// <summary>
    /// The main static configuration class. 
    /// </summary>
    public class CfgSt
    {
        public static CharacterLists Roster { get; set; } = new();
        public static OHSsettings OHS { get; set; } = new();
        public static MUAsettings MUA { get; set; } = new();
        public static XML2settings XML2 { get; set; } = new();
        public static GUIsettings GUI { get; set; } = new();
        public static VariableSettings Var { get; set; } = new();

    }
    /// <summary>
    /// The main non-static configuration class. 
    /// </summary>
    public class Cfg
    {
        public CharacterLists Roster { get; set; } = CfgSt.Roster;
        public OHSsettings OHS { get; set; } = CfgSt.OHS;
        public MUAsettings MUA { get; set; } = CfgSt.MUA;
        public XML2settings XML2 { get; set; } = CfgSt.XML2;
        public GUIsettings GUI { get; set; } = CfgSt.GUI;
        public VariableSettings Var { get; set; } = CfgSt.Var;

    }
    /// <summary>
    /// Functions related to settings (save and load)
    /// </summary>
    public class CfgCmd
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private static readonly JsonSerializerOptions JsonOptionsD = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Get version information from assembly
        /// </summary>
        /// <returns>Version number</returns>
        public static string GetVersionDescription()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version!;
            string PreRelease = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion is string PR
                                && PR.LastIndexOf('-') is int i
                                && i > 0 ? PR[i..] : "";
            return $"{version.Major}.{version.Minor}.{version.Build}{PreRelease}";
            // Don't use .{version.Revision}
        }
        /// <summary>
        /// Open file dialogue.
        /// </summary>
        public static async Task<string?> LoadDialogue(params IEnumerable<string> formats)
        {
            FileOpenPicker filePicker = new();
            foreach (string format in formats) { filePicker.FileTypeFilter.Add(format); }
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
            LoadGuiSettings($"{Oini[..Oini.LastIndexOf('.')]}_GUI.xml");
            LoadOHSsettings(Oini);
            try
            {
                FileInfo TeamBonus = new($"{Oini[..Oini.LastIndexOf('.')]}_team_bonus.xml");
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
        /// Load GUI settings from an XML file (<paramref name="Gini"/>). Note: Performs a crash if there's a major problem with the file.
        /// </summary>
        private static void LoadGuiSettings(string Gini)
        {
            if (File.Exists(Gini))
            {
                XmlSerializer XS = new(typeof(GUIsettings));
                using FileStream fs = new(Gini, FileMode.Open, FileAccess.Read);
                if (XS.Deserialize(fs) is GUIsettings CfgGUI) { CfgSt.GUI = CfgGUI; }
            }
            if (CfgSt.GUI.Game == "") { CfgSt.GUI.Game = "MUA"; }
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
                    CfgSt.OHS = CfgSt.GUI.Game == "XML2"
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
            if (CfgSt.OHS.ExeName == "") { CfgSt.OHS.ExeName = OHSpath.DefaultExe; }
        }
        /// <summary>
        /// Save OHS settings in JSON &amp; GUI settings in XML to the default location. Saves CFG files according to these settings. Use default names if mod pack setting is disabled.
        /// </summary>
        /// <returns><see langword="True"/>, if saved successfully, otherwise <see langword="false"/>.</returns>
        public static bool SaveSettingsMP()
        {
            // ExeName not to default, team_bonus handled separately
            return SaveIniXml(OHSpath.GetRooted("config.ini"), Path.Combine(OHSpath.CD, "config.xml"), CfgSt.GUI.ModPack
                ? CfgSt.GUI.Game == "XML2" ? CfgSt.XML2 : CfgSt.MUA
                : CfgSt.GUI.Game == "XML2"
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
                    RosterHack = !CfgSt.GUI.IsNotConsole || CfgSt.MUA.RosterHack,
                    RosterValue = CfgSt.OHS.RosterValue,
                    GameInstallPath = CfgSt.OHS.GameInstallPath,
                    ExeName = CfgSt.OHS.ExeName,
                    HerostatFolder = CfgSt.OHS.HerostatFolder,
                    Unlocker = CfgSt.OHS.Unlocker,
                    LaunchGame = CfgSt.OHS.LaunchGame,
                    SaveTempFiles = CfgSt.OHS.SaveTempFiles,
                    ShowProgress = CfgSt.OHS.ShowProgress,
                    DebugMode = CfgSt.OHS.DebugMode
                })
                && GenerateCfgFiles(CfgSt.OHS.RosterValue, CfgSt.MUA.MenulocationsValue);
        }
        /// <summary>
        /// Save OHS settings in JSON &amp; GUI settings in XML to the default location. Saves CFG files according to these settings.
        /// </summary>
        /// <returns><see langword="True"/>, if saved successfully, otherwise <see langword="false"/>.</returns>
        public static bool SaveSettings()
        {
            return SaveIniXml(OHSpath.GetRooted("config.ini"), Path.Combine(OHSpath.CD, "config.xml"))
                && GenerateCfgFiles(CfgSt.OHS.RosterValue, CfgSt.MUA.MenulocationsValue);
        }
        /// <summary>
        /// Save OHS settings in JSON &amp; GUI settings in XML by providing a path (<paramref name="Oini"/>). Saves roster and menulocations to <paramref name="rv"/>.cfg files.
        /// </summary>
        /// <returns><see langword="True"/>, if saved successfully, otherwise <see langword="false"/>.</returns>
        public static bool SaveSettings(string Oini, string rv)
        {
            return SaveIniXml(Oini, $"{Oini[..Oini.LastIndexOf('.')]}_GUI.xml")
                && GenerateCfgFiles(rv, rv)
                && (MarvelModsXML.TeamBonusSerializer($"{Oini[..Oini.LastIndexOf('.')]}_team_bonus.xml")
                || CfgSt.Roster.Teams.Count == 0);
        }
        /// <summary>
        /// Save OHS settings in JSON (<paramref name="Oini"/>) &amp; GUI settings in XML (<paramref name="Gini"/>) by providing both paths.
        /// </summary>
        /// <returns><see langword="True"/>, if saved successfully, otherwise <see langword="false"/>.</returns>
        private static bool SaveIniXml(string Oini, string Gini) => SaveIniXml(Oini, Gini, (CfgSt.GUI.Game == "XML2") ? CfgSt.XML2 : CfgSt.MUA);
        /// <summary>
        /// Save OHS settings in JSON (<paramref name="Oini"/>) &amp; GUI settings in XML (<paramref name="Gini"/>) by providing both paths and the <paramref name="GameCfg"/> class.
        /// </summary>
        /// <returns><see langword="True"/>, if saved successfully, otherwise <see langword="false"/>.</returns>
        private static bool SaveIniXml(string Oini, string Gini, object GameCfg)
        {
            string? Parent = Path.GetDirectoryName(Oini);
            if (string.IsNullOrEmpty(Parent)) { return false; }
            try
            {
                // JSON
                _ = Directory.CreateDirectory(Parent);
                File.WriteAllText(Oini, JsonSerializer.Serialize(GameCfg, JsonOptions));

                // XML
                XmlSerializer XS = new(typeof(GUIsettings));
                using FileStream fs = File.Open(Gini, FileMode.Create);
                XS.Serialize(fs, CfgSt.GUI);
            }
            catch
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Generate the roster <paramref name="rv"/>.cfg and menulocation <paramref name="mv"/>.cfg files in the OHS game folders.
        /// </summary>
        /// <returns><see langword="True"/>, if generated successfully, otherwise <see langword="false"/>.</returns>
        private static bool GenerateCfgFiles(string rv, string mv)
        {
            if (CfgSt.Roster.Selected.Count == 0) { return true; }
            try
            {
                if (OHSpath.Game == "mua")
                {
                    WriteCfg(Path.Combine(OHSpath.CD, "mua", "menulocations", $"{mv}.cfg"), 0);
                }
                WriteCfg(OHSpath.GetRooted("rosters", $"{rv}.cfg"), 2);
            }
            catch
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Write a OHS CFG file to <paramref name="path"/> from the <see cref="CfgSt.Roster.Selected"/> list. Write exceptions.
        /// </summary>
        private static void WriteCfg(string path, int p)
        {
            if (Path.GetDirectoryName(path) is string CFGpath && CFGpath != "")
            {
                _ = Directory.CreateDirectory(CFGpath);
                using FileStream fs = File.Create(path);
                using StreamWriter sw = new(fs);
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

        public static string FilterDefaultRV(string RV)
        {
            return DefaultRV.Contains(RV, StringComparer.OrdinalIgnoreCase) ? "temp.OHSGUI" : RV;
        }

        private static string FilterDefaultMV(string MV)
        {
            return DefaultMV.Contains(MV, StringComparer.OrdinalIgnoreCase) ? "temp.OHSGUI" : MV;
        }
    }
}