using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Functions;
using System.ComponentModel;
using System.Linq;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Variable internal settings, <see cref="ObservableObject"/>
    /// </summary>
    internal partial class InternalObservables : ObservableObject
    {
        [ObservableProperty]
        internal partial string? FloatingCharacter { get; set; }

        [ObservableProperty]
        internal partial CSSModel? SelectedStage { get; set; } // Not in CSS, because of binding

        [ObservableProperty]
        public partial Zsnd.Lib.XVSound? SelectedSound { get; set; }

        public InternalObservables()
        {
            PropertyChanged += OnPropertiesChanged;
        }

        private void OnPropertiesChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(FloatingCharacter)) { OnFloatingCharacterChanged(); }
        }

        public void OnFloatingCharacterChanged() { }
    }
    /// <summary>
    /// Template selector helper for <see cref="ListView"/>s and similar, filtering by current game tab
    /// </summary>
    public partial class GameTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? MUA { get; set; }
        public DataTemplate? XML2 { get; set; }

        protected override DataTemplate? SelectTemplateCore(object item)
        {
            return CfgSt.GUI.IsMua ? MUA : XML2;
        }
    }
    /// <summary>
    /// Static internal settings
    /// </summary>
    internal static class InternalSettings
    {
        private static readonly string[] XML2SkinAttributes =
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

        //private static readonly string[] XML1SkinNames =
        //[
        //    "skin",
        //    "skin_60s",
        //    "skin_70s",
        //    "skin_weaponx",
        //    "skin_future",
        //    "skin_civilian",
        //    "skin_magmacivilian"
        //];

        internal static readonly string[] XML2BonusSkinFilters = [.. XML2SkinAttributes[1..].Select(static s => s[5..])];

        internal static readonly string[] XML2SkinNames = ["Main", .. XML2BonusSkinFilters];

        private static readonly string[] MUASkinAttributes = ["skin", .. Enumerable.Range(2, 5).Select(static x => $"skin_0{x}")];

        internal static readonly string[] MUASkinNameAttributes = [.. Enumerable.Range(1, 6).Select(static x => $"skin_0{x}_name")];

        internal static string[] GetSkinAttributes => CfgSt.GUI.IsMua ? MUASkinAttributes : XML2SkinAttributes;

        internal static string[] RFLanguages =
        [
            "English (engb)",
            "Italian (itab)",
            "French (freb)",
            "Spanish (spab)",
            "German (gerb)",
            "Russian (rusb)",
            "Polish (polb)",
            "None (xmlb)"
        ];

        internal static string[] RavenXMLBLang = [.. RFLanguages.Select(static s => s[^5..^1])];

        internal static readonly string[] RavenFormatsXML =
        [
            "xml", "eng", "fre", "ger", "ita", "pol", "rus", "spa", "pkg", "boy", "chr", "nav"
        ];

        internal static readonly string[] RavenFormats = [.. RavenFormatsXML.Select(static x => $".{x}b")];

        internal static readonly string[] KnownModOrganizerExes =
        [
            "Vortex.exe", "Modmanager.exe", "ModOrganizer.exe"
        ];

        internal static Windows.Globalization.NumberFormatting.DecimalFormatter PaddedNumFormatter = new() { IntegerDigits = 2, FractionDigits = 0 };
        /// <summary>
        /// Representing the stage model config.xml.
        /// </summary>
        internal static Models? Models;
        /// <summary>
        /// Representing the stage effects config.xml.
        /// </summary>
        internal static CSSEffects? Effects;
        /// <summary>
        /// Loads the stage model configuration from the stages/.models/config.xml file and initializes the categories dictionary.
        /// </summary>
        /// <remarks>Currently, also calls <see cref="LoadEffects"/> to (re)load effects alongside models.</remarks>
        internal static void LoadModels()
        {
            Models = GUIXML.Deserialize(System.IO.Path.Combine(OHSpath.Model, "config.xml"), typeof(Models)) as Models;
            _ = (Models?.categories = Models.Categories.ToDictionary(static c => c.Name, static c => c.Models));
            LoadEffects();
        }
        /// <summary>
        /// Loads the stage effects configuration from the stages/.effects/config.xml file and populates the <see cref="AvailableEffects"/>.
        /// </summary>
        internal static void LoadEffects()
        {
            Effects = (CSSEffects?)GUIXML.Deserialize(System.IO.Path.Combine(OHSpath.StagesDir, ".effects", "config.xml"), typeof(CSSEffects));
        }
    }
}
