using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Stage model with a name, creator, path and image data. Path should be checked separately for IGB model.
    /// </summary>
    public class StageModel
    {
        public string? Name { get; set; }
        public string? Creator { get; set; }
        public DirectoryInfo? Path { get; set; }
        public BitmapImage? Image { get; set; }
        public bool Riser { get; set; }
        public bool Favourite { get; set; }
    }
    /// <summary>
    /// Class with <see cref="Message"/> + <see cref="Title"/> (<see cref="string"/>s) and <see cref="IsOpen"/> (<see cref="bool"/>) properties for message binding.
    /// </summary>
    public class MessageItem
    {
        public string? Title { get; set; }
        public string? Message { get; set; }
        public bool IsOpen { get; set; }
    }
    /// <summary>
    /// Variable internal settings, <see cref="ObservableRecipient"/>
    /// </summary>
    public partial class VariableSettings : ObservableRecipient
    {
        [ObservableProperty]
        private StageModel? selectedStage;
        [ObservableProperty]
        private IEnumerable<int>? layoutLocs;
        [ObservableProperty]
        private IEnumerable<int>? rosterRange;
        [ObservableProperty]
        private XmlElement? layout;
        [ObservableProperty]
        private string rosterValueDefault;
        [ObservableProperty]
        private string menulocationsValueDefault;
        [ObservableProperty]
        public string? floatingCharacter;
        [ObservableProperty]
        private char hsFormat;
        [ObservableProperty]
        private FileInfo? hsPath;
        [ObservableProperty]
        private bool popAvail;
        [ObservableProperty]
        private MessageItem? sE_Msg_Error;
        [ObservableProperty]
        private MessageItem? sE_Msg_Info;
        [ObservableProperty]
        private MessageItem? sE_Msg_Success;
        [ObservableProperty]
        private MessageItem? sE_Msg_Warning;
        [ObservableProperty]
        private bool sE_Msg_WarnPkg;
        [ObservableProperty]
        private bool isMua;
        [ObservableProperty]
        private bool isXml2;

        public VariableSettings()
        {
            rosterValueDefault = "";
            menulocationsValueDefault = "";
            hsFormat = ' ';
        }
    }
    /// <summary>
    /// Static internal settings
    /// </summary>
    public static class InternalSettings
    {
        private static readonly string[] XML2SkinNames =
        [
            "skin",
            "skin_astonishing",
            "skin_aoa",
            "skin_60s",
            "skin_70s",
            "skin_weaponx",
            "skin_future",
            "skin_winter",
            "skin_civilian"
        ];
        public static readonly string[] XML2Skins = XML2SkinNames[1..].Select(s => s[5..]).ToArray();
        private static readonly string[] MUASkinNames = ["skin", .. Enumerable.Range(2, 5).Select(x => $"skin_0{x}")];
        public static string[] SkinIdentifiers => CfgSt.GUI.Game == "XML2"
            ? XML2SkinNames
            : MUASkinNames;
        public static readonly string[] RavenFormatsXML =
        [
            "xml", "eng", "fre", "ger", "ita", "pol", "rus", "spa", "pkg", "boy", "chr", "nav"
        ];
        public static readonly string[] RavenFormats = RavenFormatsXML.Select(x => $".{x}b").ToArray();
        public static readonly string[] KnownModOrganizerExes =
        [
            "Vortex.exe", "ModOrganizer.exe"
        ];
        /// <summary>
        /// Team bonus powerups with description for use in MUA team_bonus files
        /// </summary>
        public static readonly Dictionary<string, string> TeamPowerups = new()
        {
            ["5% Damage Inflicted as Health Gain"] = "shared_team_damage_to_health",
            ["20 Energy Per Knockout"] = "shared_team_energy_per_kill",
            ["+60% S.H.I.E.L.D. Credit Drops"] = "shared_team_extra_money",
            ["20 Health Per Knockout"] = "shared_team_health_per_kill",
            ["+5 Health Regeneration"] = "shared_team_health_regen",
            ["+5% Criticals"] = "shared_team_increase_criticals",
            ["+5% Damage"] = "shared_team_increase_damage",
            ["+5 All Resistances"] = "shared_team_increase_resistances",
            ["+15 Strike"] = "shared_team_increase_striking",
            ["+6 Body, Strike, Focus"] = "shared_team_increase_traits",
            ["+5% Experience Points"] = "shared_team_increase_xp",
            ["+15% Maximum Energy"] = "shared_team_max_energy",
            ["+15% Maximum Health"] = "shared_team_max_health",
            ["10% Reduced Energy Cost"] = "shared_team_reduce_energy_cost",
            ["15% Reduced Energy Cost"] = "shared_team_reduce_energy_cost_dlc"
        };
        /// <summary>
        /// Team bonus powerups with description for use in XML2 team_bonus files
        /// </summary>
        public static readonly Dictionary<string, string> TeamPowerupsXML2 = new()
        {
            ["+20 Energy per Knockout"] = "shared_team_bruiser_bash",
            ["5% Dmg inflicted as Health Gain"] = "shared_team_femme_fatale",
            ["+5% Experience"] = "shared_team_brotherhood_of_evil",
            ["+10 All Resistances"] = "shared_team_elemental_fusion",
            ["+100% Attack Rating"] = "shared_team_age_of_apoc",
            ["+5% Damage"] = "shared_team_special_ops",
            ["+10 All Traits"] = "shared_team_heavy_metal",
            ["+5 Health Regeneration"] = "shared_team_family_affair",
            ["+15% Max Energy"] = "shared_team_old_school",
            ["20 Health per KO"] = "shared_team_double_date",
            ["+15% Max Health"] = "shared_team_new_xmen",
            ["+60% Techbit drops"] = "shared_team_raven_knights"
        };
    }
}
