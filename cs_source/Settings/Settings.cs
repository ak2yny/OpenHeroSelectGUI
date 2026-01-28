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
using Zsnd.Lib;

namespace OpenHeroSelectGUI.Settings
{
    public partial class OHSsettings : ObservableObject
    {
        [ObservableProperty]
        public partial string RosterValue { get; set; } = "Roster";
        [ObservableProperty]
        public partial string GameInstallPath { get; set; } = "";
        [ObservableProperty]
        public partial string ExeName { get; set; } = "";
        [ObservableProperty]
        public partial string HerostatName { get; set; } = "herostat.engb";
        [ObservableProperty]
        public partial string NewGamePyName { get; set; } = "new_game.py";
        [ObservableProperty]
        public partial string CharactersHeadsPackageName { get; set; } = "characters_heads.pkgb";
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
        public partial string HerostatFolder { get; set; } = "xml";
    }
    public partial class MUAsettings : OHSsettings
    {
        [ObservableProperty]
        public partial string MenulocationsValue { get; set; } = "temp.OHSGUI";
        [ObservableProperty]
        public partial bool RosterHack { get; set; }
        // MUA mod pack settings:
        [ObservableProperty]
        public partial string MannequinFolder { get; set; } = "mannequin";
        [ObservableProperty]
        public partial string CharinfoName { get; set; } = "charinfo.xmlb";
    }
    public partial class XML2settings : OHSsettings
    {
        [ObservableProperty]
        public partial int RosterSize { get; set; } = 21;
        [ObservableProperty]
        public partial bool UnlockSkins { get; set; }
    }
    /// <summary>
    /// XML serializable GUI settings. XML attributes are public properties, other fields are not serialized.
    /// </summary>
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
        public partial string AvailChars { get; set; }
        [ObservableProperty]
        public partial bool SelectedDnDInsert { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        internal partial string GameInstallPath { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        internal partial string ExeArguments { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        internal partial bool FreeSaves { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        internal partial bool ShowClashes { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        internal partial bool ModPack { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        internal partial string TeamBonusName { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        internal partial string SkinModName { get; set; }
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
        public HashSet<string> StageFavourites = [];
        [ObservableProperty]
        public partial bool RowLayout { get; set; }
        [ObservableProperty]
        public partial double ButtonOpacity { get; set; }
        [ObservableProperty]
        public partial bool LayoutWidthUpscale { get; set; }
        [ObservableProperty]
        public partial int LayoutMaxWidth { get; set; }
        [ObservableProperty]
        public partial bool HidableEffectsOnly { get; set; }
        [ObservableProperty]
        public partial bool CopyStage { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        internal partial bool IsMua { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        internal partial bool IsMuaPC { get; set; }
        [ObservableProperty]
        public partial bool IsNotConsole { get; set; }
        // XML2 specific settings
        [ObservableProperty]
        [XmlIgnore]
        internal partial bool IsXml2 { get; set; }
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
        [ObservableProperty]
        [XmlIgnore]
        internal partial bool SkinsCanDragItems { get; set; }
        [ObservableProperty]
        [XmlIgnore]
        internal partial bool SkinsCanReorder { get; set; }

        public GUIsettings()
        {
            VersionDescription = CfgCmd.GetVersionDescription();
            GitHub = "https://github.com/ak2yny/OpenHeroSelectGUI";
            // Note: XML2 seems to have an issue when the settings.dat is removed.
            MuaFreeSaves = HidableEffectsOnly = CopyStage = IsNotConsole = IsMua = true;
            Layout = Game = Home = GameInstallPath = ActualGameExe = ExeArguments = TeamBonusName = SkinModName = AvailChars = MuaInstallPath = MuaArguments = MuaTeamBonusName = Xml2InstallPath = Xml2Arguments = Xml2TeamBonusName = Xml2SkinModName = MuaSkinModName = "";
            Model = "Default";
            ThumbnailWidth = 224;
            LayoutMaxWidth = 1000;
            ButtonOpacity = 1.0;
        }

        partial void OnSkinsDragEnabledChanged(bool value)
        {
            // That's right. This is to prevent the Skins_DragItemCompleted event from firing for MUA
            SkinsCanDragItems = IsXml2 && value;
            SkinsCanReorder = IsMua || value;
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
                IsMua = !(IsXml2 = IsXML2);
                IsMuaPC = IsMua && IsNotConsole;
                SkinsCanDragItems = IsXML2 && SkinsDragEnabled;
                SkinsCanReorder = IsMua || SkinsDragEnabled;
                Lists.Sounds.Clear(); // Loads from disk, next time changing back to prev. game
                CfgSt.Roster.AvailableFiltered.Clear();
                OHSpath.Game = IsMua ? "mua" : "xml2";
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
            GetType().GetField($"{(IsMua ? "Mua" : "Xml2")}{name}")?.SetValue(this, value);
        }
    }
    /// <summary>
    /// The main static configuration class. 
    /// </summary>
    public class CfgSt
    {
        public static Layout CSS { get; set; } = new();
        public static OHSsettings OHS { get; set; } = new();
        public static MUAsettings MUA { get; set; } = new();
        public static XML2settings XML2 { get; set; } = new();
        internal static GUIsettings GUI { get; set; } = new();
        internal static InternalObservables Var { get; set; } = new();
        internal static CharacterLists Roster { get; set; } = new();
    }
    /// <summary>
    /// The main non-static configuration class. 
    /// </summary>
    public class Cfg
    {
        public OHSsettings OHS { get; set; } = CfgSt.OHS;
        public MUAsettings MUA { get; set; } = CfgSt.MUA;
        public XML2settings XML2 { get; set; } = CfgSt.XML2;
        internal GUIsettings GUI { get; set; } = CfgSt.GUI;
        internal InternalObservables Var { get; set; } = CfgSt.Var;
        internal CharacterLists Roster { get; set; } = CfgSt.Roster;
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
            StorageFile? file = await filePicker.PickSingleFileAsync();
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
                    CfgSt.OHS = CfgSt.GUI.IsMua
                        ? (CfgSt.MUA = JsonSerializer.Deserialize<MUAsettings>(File.ReadAllText(Oini), JsonOptionsD)!)
                        : (CfgSt.XML2 = JsonSerializer.Deserialize<XML2settings>(File.ReadAllText(Oini), JsonOptionsD)!);
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
        /// Save OHS settings in JSON &amp; GUI settings in XML to the default location. Saves CFG files according to these settings. Use default names if <paramref name="DisableModPackEnabled"/> or <see cref="CfgSt.GUI.ModPack"/> is false.
        /// </summary>
        /// <returns><see langword="True"/>, if saved successfully; otherwise <see langword="false"/>.</returns>
        public static bool SaveSettings(bool DisableModPackEnabled = true)
        {
            // ExeName not to default, team_bonus handled separately
            return SaveIniXml(OHSpath.GetRooted("config.ini"), Path.Combine(OHSpath.CD, "config.xml"), DisableModPackEnabled || CfgSt.GUI.ModPack
                ? CfgSt.GUI.IsMua ? CfgSt.MUA : CfgSt.XML2
                : CfgSt.GUI.IsMua
                ?
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
                }
                :
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
                })
                && GenerateCfgFiles(CfgSt.OHS.RosterValue, CfgSt.MUA.MenulocationsValue);
        }
        /// <summary>
        /// Save OHS settings in JSON &amp; GUI settings in XML by providing a path (<paramref name="Oini"/>). Saves roster and menulocations to <paramref name="rv"/>.cfg files. Saves team_bonus if necessary.
        /// </summary>
        /// <returns><see langword="True"/>, if saved successfully; otherwise <see langword="false"/>.</returns>
        public static bool SaveSettings(string Oini, string rv)
        {
            return SaveIniXml(Oini, $"{Oini[..Oini.LastIndexOf('.')]}_GUI.xml")
                && GenerateCfgFiles(rv, rv)
                && (CfgSt.Roster.Teams.Count == 0
                || BonusSerializer.Serialize($"{Oini[..Oini.LastIndexOf('.')]}_team_bonus.xml"));
        }
        /// <summary>
        /// Save OHS settings in JSON (<paramref name="Oini"/>) &amp; GUI settings in XML (<paramref name="Gini"/>) by providing both paths.
        /// </summary>
        /// <returns><see langword="True"/>, if saved successfully; otherwise <see langword="false"/>.</returns>
        private static bool SaveIniXml(string Oini, string Gini) => SaveIniXml(Oini, Gini, CfgSt.GUI.IsMua ? CfgSt.MUA : CfgSt.XML2);
        /// <summary>
        /// Save OHS settings in JSON (<paramref name="Oini"/>) &amp; GUI settings in XML (<paramref name="Gini"/>) by providing both paths and the <paramref name="GameCfg"/> class.
        /// </summary>
        /// <returns><see langword="True"/>, if saved successfully; otherwise <see langword="false"/>.</returns>
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
        /// <returns><see langword="True"/>, if generated successfully; otherwise <see langword="false"/>.</returns>
        private static bool GenerateCfgFiles(string rv, string mv)
        {
            if (CfgSt.Roster.Selected.Count == 0) { return true; }
            try
            {
                if (CfgSt.GUI.IsMua)
                {
                    WriteCfg(Path.Combine(OHSpath.CD, "mua", "menulocations", $"{mv}.cfg"), true);
                }
                WriteCfg(OHSpath.GetRooted($"rosters/{rv}.cfg"));
            }
            catch
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Write a OHS CFG file to <paramref name="path"/> from the <see cref="CfgSt.Roster.Selected"/> list.
        /// </summary>
        /// <remarks>Exceptions: System.IO exceptions.</remarks>
        private static void WriteCfg(string path, bool writeLoc = false)
        {
            if (Path.GetDirectoryName(path) is string CFGpath && CFGpath != "")
            {
                _ = Directory.CreateDirectory(CFGpath);
                using FileStream fs = File.Create(path);
                using StreamWriter sw = new(fs);
                for (int i = 0; i < CfgSt.Roster.Selected.Count; i++)
                {
                    SelectedCharacter c = CfgSt.Roster.Selected[i];
                    sw.WriteLine(writeLoc ?
                        c.Loc :
                        c.Starter ? $"*{c.Path}" : c.Unlock ? $"?{c.Path}" : c.Path);
                }
            }
        }

        public static readonly string[] DefaultRV = [
            "Default 25 Character Roster",
            "Official Characters Pack Base Roster",
            "Default 28 Character (PSP) Roster",
            "Default 33 Character (DLC, Gold) Roster",
            "36 Roster Hack Base Roster (Gold Edition Stage)",
            "36 Roster Hack Base Roster (v1.0 and 1.5)",
            "36 Roster Hack Base Roster (v2.0 and later)",
            "50 Roster Hack Base Roster",
            "50 Roster Hack Base Roster (v3.0 and later)",
            "Default 18 Character (GC, PS2, Xbox) Roster",
            "Default 20 Character (PC) Roster",
            "Default 22 Character (PSP) Roster"];

        private static readonly string[] DefaultMV = [
            "25 (Default Base Game)",
            "27 (Official Characters Pack)",
            "28 (Default PSP)",
            "33 (OCP 2 Gold Edition Stage)",
            "36 (for 36RH Gold Edition Stage)",
            "36 (for 36RH v1.0 or 1.5 Stage)",
            "36 (for 36RH v2.0 or later Stage)",
            "50 (for 50RH Stage)",
            "50 (for 50RH v3.0 or later Stage)",
            "28 (for 28 Roster Hack)",
            "33 (Default DLC, Gold Edition)"];

        public static string FilterDefaultRV(string RV)
        {
            return DefaultRV.Contains(RV, StringComparer.OrdinalIgnoreCase) ? "temp.OHSGUI" : RV;
        }

        private static string FilterDefaultMV(string MV)
        {
            return DefaultMV.Contains(MV, StringComparer.OrdinalIgnoreCase) ? "temp.OHSGUI" : MV;
        }

        public static string MatchingMV(string RV)
        {
            return DefaultRV.IndexOf(RV, StringComparer.OrdinalIgnoreCase) is int i && i is > -1 and < 9
                ? DefaultMV[i] : RV;
        }
    }
}