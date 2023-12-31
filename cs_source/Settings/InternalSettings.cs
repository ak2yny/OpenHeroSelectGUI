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
    /// Model with a name, creator, path and image data. Path should be checked separately for IGB model.
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
    /// Dynamic internal settings, observable
    /// </summary>
    public partial class DynamicSettings : ObservableRecipient
    {
        [ObservableProperty]
        private string game;
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

        public static DynamicSettings Instance { get; set; } = new();

        public DynamicSettings()
        {
            game = "";
            rosterValueDefault = "";
            menulocationsValueDefault = "";
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
        {
            "skin",
            "skin_astonishing",
            "skin_aoa",
            "skin_60s",
            "skin_70s",
            "skin_weaponx",
            "skin_future",
            "skin_winter",
            "skin_civilian"
        };
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
        {
            "xml", "eng", "fre", "ger", "ita", "pol", "rus", "spa", "pkg", "boy", "chr", "nav"
        };
        public static readonly string[] RavenFormats = RavenFormatsXML.Select(x => $".{x}b").ToArray();

        // Alchemy static resources
        public static readonly string? AlchemyRoot = Environment.GetEnvironmentVariable("IG_ROOT");
        public static readonly string Alchemy_ini = Path.Combine(Path.GetTempPath(), "OHSGUI_Alchemy.ini");
        public static readonly string GetSkinInfo = string.Join(Environment.NewLine, new string[] { AlchemyHead(3), AlchemyiST(1), AlchemyiSG(2), AlchemyiSS(3) });
        public static string AlchemyHead(int N)
        {
            string[] Lines =
            {
                "[OPTIMIZE]",
                $"optimizationCount = {N}",
                "hierarchyCheck = true"
            };
            return string.Join(Environment.NewLine, Lines);
        }
        private static string AlchemyiST(int N)
        {
            string[] Lines =
            {
                $"[OPTIMIZATION{N}]",
                "name = igStatisticsTexture",
                AlchemyStats("0x00000117"),
                "useFullPath = false"
            };
            return string.Join(Environment.NewLine, Lines);
        }
        private static string AlchemyiSG(int N)
        {
            string[] Lines =
            {
                $"[OPTIMIZATION{N}]",
                "name = igStatisticsGeometry",
                AlchemyStats("0x00500000")
            };
            return string.Join(Environment.NewLine, Lines);
        }
        private static string AlchemyiSS(int N)
        {
            string[] Lines =
            {
                $"[OPTIMIZATION{N}]",
                "name = igStatisticsSkin",
                AlchemyStats("0x00000006")
            };
            return string.Join(Environment.NewLine, Lines);
        }
        private static string AlchemyStats(string Mask)
        {
            string[] Lines =
            {
                "separatorString = |",
                "columnMaxWidth = -1",
                $"showColumnsMask = {Mask}",
                "sortColumn = -1"
            };
            return string.Join(Environment.NewLine, Lines);
        }
        public static string AlchemyRen(int N, string SourceName, string NewName)
        {
            string[] Lines =
            {
                $"[OPTIMIZATION{N}]",
                "name = igChangeObjectName",
                "objectTypeName = igNamedObject",
                $"targetName = ^{SourceName}$",
                $"newName = {NewName}"
            };
            return string.Join(Environment.NewLine, Lines);
        }
        public static string AlchemyGGC(int N)
        {
            string[] Lines =
            {
                $"[OPTIMIZATION{N}]",
                "name = igGenerateGlobalColor"
            };
            return string.Join(Environment.NewLine, Lines);
        }
        public static string AlchemyCGA(int N)
        {
            string[] Lines =
            {
                $"[OPTIMIZATION{N}]",
                "name = igConvertGeometryAttr",
                "accessMode = 3",
                "storeBoundingVolume = false"
            };
            return string.Join(Environment.NewLine, Lines);
        }
    }
}
