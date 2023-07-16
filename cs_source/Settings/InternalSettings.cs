using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Dynamic internal settings, observable
    /// </summary>
    public partial class DynamicSettings : ObservableRecipient
    {
        [ObservableProperty]
        private string game;
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
        private IEnumerable<int>? rosterRange;
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
            riser = false;
            selectedLayout = -1;
            selectedModel = -1;
            selectedModelPath = "";
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
            return (DynamicSettings.Instance.Game == "xml2")
            ? XML2SkinNames
            : MUASkinNames;
        }
    }
}
