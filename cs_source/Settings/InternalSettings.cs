using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
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
    }
    /// <summary>
    /// Class with strings (message + title) and boolean (IsOpen) properties for message binding.
    /// </summary>
    public class MessageItem
    {
        public string? Title { get; set; }
        public string? Message { get; set; }
        public bool IsOpen { get; set; }
    }
    /// <summary>
    /// Dynamic internal settings, observable
    /// </summary>
    public partial class DynamicSettings : ObservableRecipient
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
        private MessageItem? sE_Msg_Error;
        [ObservableProperty]
        private MessageItem? sE_Msg_Info;
        [ObservableProperty]
        private MessageItem? sE_Msg_Success;
        [ObservableProperty]
        private MessageItem? sE_Msg_Warning;
        [ObservableProperty]
        private MessageItem? sE_Msg_WarnPkg;

        public static DynamicSettings Instance { get; set; } = new();

        public DynamicSettings()
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
        public static readonly string cdPath = Directory.GetCurrentDirectory();  // Directory.GetCurrentDirectory()
        public static readonly string ModelPath = Path.Combine(cdPath, "stages", ".models");
        public static readonly string Activision = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Activision");
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
        public static readonly string[] XML2Skins = XML2SkinNames[1..].Select(s => s.Replace("skin_", "")).ToArray();
        private static string[] GetMUASkinNames()
        {
            string[] MUAskins = new string[6];
            MUAskins[0] = "skin";
            for (int i = 1; i < 6; i++) { MUAskins[i] = $"skin_0{i + 1}"; }
            return MUAskins;
        }
        private static readonly string[] MUASkinNames = GetMUASkinNames();
        public static string[] GetSkinIdentifiers()
        {
            return (GUIsettings.Instance.Game == "xml2")
            ? XML2SkinNames
            : MUASkinNames;
        }
        public static readonly string[] RavenFormatsXML =
        [
            "xml", "eng", "fre", "ger", "ita", "pol", "rus", "spa", "pkg", "boy", "chr", "nav"
        ];
        public static readonly string[] RavenFormats = RavenFormatsXML.Select(x => $".{x}b").ToArray();
        /// <summary>
        /// Team bonus powerups with description for use in team_bonus files
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
        /// Team bonus powerups with description for use in team_bonus files
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

        // Alchemy static resources, WIP: can be improved
        public static readonly string? AlchemyRoot = Environment.GetEnvironmentVariable("IG_ROOT");
        public static readonly string Alchemy_ini = Path.Combine(Path.GetTempPath(), "OHSGUI_Alchemy.ini");
        public static readonly string GetSkinInfo = string.Join(Environment.NewLine, new string[] { AlchemyHead(3), AlchemyiST(1), AlchemyiSG(2), AlchemyiSS(3) });
        public static string AlchemyHead(int N)
        {
            string[] Lines =
            [
                "[OPTIMIZE]",
                $"optimizationCount = {N}",
                "hierarchyCheck = true"
            ];
            return string.Join(Environment.NewLine, Lines);
        }
        private static string AlchemyiST(int N)
        {
            string[] Lines =
            [
                $"[OPTIMIZATION{N}]",
                "name = igStatisticsTexture",
                AlchemyStats("0x00000117"),
                "useFullPath = false"
            ];
            return string.Join(Environment.NewLine, Lines);
        }
        private static string AlchemyiSG(int N)
        {
            string[] Lines =
            [
                $"[OPTIMIZATION{N}]",
                "name = igStatisticsGeometry",
                AlchemyStats("0x00500000")
            ];
            return string.Join(Environment.NewLine, Lines);
        }
        private static string AlchemyiSS(int N)
        {
            string[] Lines =
            [
                $"[OPTIMIZATION{N}]",
                "name = igStatisticsSkin",
                AlchemyStats("0x00000006")
            ];
            return string.Join(Environment.NewLine, Lines);
        }
        private static string AlchemyStats(string Mask)
        {
            string[] Lines =
            [
                "separatorString = |",
                "columnMaxWidth = -1",
                $"showColumnsMask = {Mask}",
                "sortColumn = -1"
            ];
            return string.Join(Environment.NewLine, Lines);
        }
        public static string AlchemyRen(int N, string SourceName, string NewName)
        {
            string[] Lines =
            [
                $"[OPTIMIZATION{N}]",
                "name = igChangeObjectName",
                "objectTypeName = igNamedObject",
                $"targetName = ^{SourceName}$",
                $"newName = {NewName}"
            ];
            return string.Join(Environment.NewLine, Lines);
        }
        public static string AlchemyGGC(int N)
        {
            string[] Lines =
            [
                $"[OPTIMIZATION{N}]",
                "name = igGenerateGlobalColor"
            ];
            return string.Join(Environment.NewLine, Lines);
        }
        public static string AlchemyCGA(int N)
        {
            string[] Lines =
            [
                $"[OPTIMIZATION{N}]",
                "name = igConvertGeometryAttr",
                "accessMode = 3",
                "storeBoundingVolume = false"
            ];
            return string.Join(Environment.NewLine, Lines);
        }
    }
}
