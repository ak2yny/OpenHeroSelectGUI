using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace OpenHeroSelectGUI.Settings;

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
    private int home;
    [ObservableProperty]
    private string game;
    [ObservableProperty]
    private bool freeSaves;
    [ObservableProperty]
    private string versionDescription;
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
        game = "mua";
        freeSaves = true;
        layout = "25 Default PC 2006";
        model = "Default";
        rowLayout = false;
    }

    private static string GetVersionDescription()
    {
        string name = "Resources/AppDisplayName".GetLocalized();
        Version version = Assembly.GetExecutingAssembly().GetName().Version!;

        // Don't use .{version.Revision}
        return $"{name} - {version.Major}.{version.Minor}.{version.Build}";
    }
}
public partial class DynamicSettings : ObservableRecipient
{
    [ObservableProperty]
    private string home;
    [ObservableProperty]
    private bool riser;
    [ObservableProperty]
    private int selectedLayout;
    [ObservableProperty]
    private int selectedModel;
    [ObservableProperty]
    private string selectedModelPath;
    [ObservableProperty]
    private IEnumerable<int>? layoutLocs;
    [ObservableProperty]
    private string rosterValueDefault;
    [ObservableProperty]
    private string menulocationsValueDefault;
    [ObservableProperty]
    public string? floatingCharacter;

    public static DynamicSettings Instance { get; set; } = new();

    public DynamicSettings()
    {
        home = "";
        riser = false;
        selectedLayout = -1;
        selectedModel = -1;
        selectedModelPath = "";
        rosterValueDefault = "";
        menulocationsValueDefault = "";
    }
}
/// <summary>
/// The configuration class. 
/// </summary>
public class Cfg
{
    public OHSsettings OHS { get; set; } = OHSsettings.Instance;
    public MUAsettings MUA { get; set; } = MUAsettings.Instance;
    public XML2settings XML2 { get; set; } = XML2settings.Instance;
    public GUIsettings GUI { get; set; } = GUIsettings.Instance;
    public DynamicSettings Dynamic { get; set; } = DynamicSettings.Instance;
    public CharacterLists Roster { get; set; } = CharacterLists.Instance;

    private static readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Open file dialogue.
    /// </summary>
    public static async Task<string?> LoadDialogue(string format)
    {
        FileOpenPicker filePicker = new();
        filePicker.FileTypeFilter.Add($"*.{format}");
        Window window = new();
        WinRT.Interop.InitializeWithWindow.Initialize(filePicker, WinRT.Interop.WindowNative.GetWindowHandle(window));
        StorageFile file = await filePicker.PickSingleFileAsync();
        if (file != null)
        {
            return file.Path;
        }
        return null;
    }
    public static string GetHerostatFolder()
    {
        string HerostatFolder = Path.IsPathRooted(OHSsettings.Instance.HerostatFolder) ?
                                OHSsettings.Instance.HerostatFolder :
                                Path.Combine(InternalSettings.cdPath, GUIsettings.Instance.Game, OHSsettings.Instance.HerostatFolder);
        return HerostatFolder;
    }
    /// <summary>
    /// Load OHS JSON data from the default OHS ini.
    /// </summary>
    public static void LoadOHSsettings() => LoadOHSsettings(Path.Combine(InternalSettings.cdPath, GUIsettings.Instance.Game, "config.ini"));
    /// <summary>
    /// Load OHS JSON data by providing a path. This assumes that the loaded file is for the currently active game tab.
    /// </summary>
    public static void LoadOHSsettings(string Oini)
    {
        if (File.Exists(Oini))
        {
            OHSsettings.Instance = GUIsettings.Instance.Game == "xml2"
                ? (XML2settings.Instance = JsonSerializer.Deserialize<XML2settings>(File.ReadAllText(Oini), options)!)
                : (MUAsettings.Instance = JsonSerializer.Deserialize<MUAsettings>(File.ReadAllText(Oini), options)!);
            // (WIP) has to load and populate the roster according to the settings in OHScfg.rosterValue ?
        }
    }
    /// <summary>
    /// Load GUI settings from the default XML file.
    /// </summary>
    public static void LoadGuiSettings() => LoadGuiSettings(Path.Combine(InternalSettings.cdPath, "config.xml"));
    /// <summary>
    /// Load GUI settings from an XML file.
    /// </summary>
    public static void LoadGuiSettings(string Gini)
    {
        if (File.Exists(Gini))
        {
            XmlSerializer XS = new(typeof(GUIsettings));
            using FileStream fs = File.Open(Gini, FileMode.Open);
            GUIsettings.Instance = (GUIsettings)XS.Deserialize(fs);
        }
    }
    /// <summary>
    /// Load OHS settings in JSON & GUI settings in XML by providing the path to the OHS .ini file. Assumes the same name with a "_GUI" suffix for the GUI .xml file.
    /// </summary>
    public static void LoadSettings(string Oini)
    {
        LoadGuiSettings(Oini.Remove(Oini.LastIndexOf(".")) + "_GUI.xml");
        LoadOHSsettings(Oini);
        // There might be a different home defined in the settings, but automatic navigation on load does not happen... intentionally.?
    }
    /// <summary>
    /// Save OHS settings in JSON & GUI settings in XML to the default location.
    /// </summary>
    public static void SaveSettings() => SaveSettings(Path.Combine(InternalSettings.cdPath, GUIsettings.Instance.Game, "config.ini"), Path.Combine(InternalSettings.cdPath, "config.xml"));
    /// <summary>
    /// Save OHS settings in JSON & GUI settings in XML by providing a path.
    /// </summary>
    public static void SaveSettings(string Oini) => SaveSettings(Oini, Oini.Remove(Oini.LastIndexOf(".")) + "_GUI.xml");
    /// <summary>
    /// Save OHS settings in JSON & GUI settings in XML by providing both paths.
    /// </summary>
    public static void SaveSettings(string Oini, string Gini)
    {
        // JSON
        string? Opath = Path.GetDirectoryName(Oini);
        if (string.IsNullOrEmpty(Opath)) return;
        Directory.CreateDirectory(Opath);
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
}

public static class InternalSettings
{
    public static readonly string cdPath = Directory.GetCurrentDirectory();  // Directory.GetCurrentDirectory()
    public static readonly string ModelPath = Path.Combine(cdPath, "stages", ".models");
    public static readonly string Activision = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Activision");
    public static bool IsDefaultRV(string RV)
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
    public static bool IsDefaultMV(string MV)
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
