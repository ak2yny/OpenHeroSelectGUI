using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Storage;
using static OpenHeroSelectGUI.Settings.CfgCommands;
using static OpenHeroSelectGUI.Settings.GUIXML;
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI.Settings
{
    public class Character
    {
        public string? Path { get; set; }
        public string? Name { get; set; }

        public override string ToString()
        {
            return Name is null ? "" : Name;
        }
    }
    public class TeamBonus
    {
        public string? Name { get; set; }
        public string? Descbonus { get; set; }
        public string? Sound { get; set; }
        public string? Skinset { get; set; }
        public ObservableCollection<TeamMember>? Members { get; set; }
        public StandardUICommand? Command;
    }
    public class TeamMember
    {
        public string? Name { get; set; }
        public string? Skin { get; set; }
    }
    /// <summary>
    /// Selected Characters: Definition
    /// </summary>
    public partial class CharacterLists : ObservableRecipient
    {
        public ObservableCollection<SelectedCharacter> Selected { get; set; } = [];
        public ObservableCollection<TeamBonus> Teams { get; set; } = [];
        public ObservableCollection<TeamBonus> TeamsMUA { get; set; } = [];
        public ObservableCollection<TeamBonus> TeamsXML2 { get; set; } = [];
        [ObservableProperty]
        private string[]? available;
        [ObservableProperty]
        private int total;
        [ObservableProperty]
        private bool numClash;

        public static CharacterLists Instance { get; set; } = new();
    }
    /// <summary>
    /// Selected Character Class
    /// </summary>
    public partial class SelectedCharacter : ObservableRecipient
    {
        [ObservableProperty]
        public string? loc;
        [ObservableProperty]
        public string? character_Name;
        [ObservableProperty]
        public string? path;
        [ObservableProperty]
        public string? character_Number;
        [ObservableProperty]
        public bool unlock;
        [ObservableProperty]
        public bool starter;
        [ObservableProperty]
        public string? effect;
        [ObservableProperty]
        public bool numClash;

        public List<string> AvailableEffects = GetAvailableEffects();
    }
    /// <summary>
    /// Selected Character list view column selector
    /// </summary>
    public class GameTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? MUA { get; set; }
        public DataTemplate? XML2 { get; set; }

        protected override DataTemplate? SelectTemplateCore(object item)
        {
            return GUIsettings.Instance.Game == "xml2" ? XML2 : MUA;
        }
    }
    /// <summary>
    /// Visibility converter. Returns Visible, if value is true (Collapsed if false). Use ConvertParameter "Invert" to invert returns.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool b && b ^ (parameter as string ?? string.Empty).Equals("Invert") ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility v && v == Visibility.Visible ^ (parameter as string ?? string.Empty).Equals("Invert");
        }
    }
    /// <summary>
    /// Visibility converter. Returns Visible, if value string equals ConvertParameter (Collapsed if not). ConvertBack only works for game conversion.
    /// </summary>
    public class GameToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is string game && parameter is string cp && game == cp ? Visibility.Visible : Visibility.Collapsed;
        }
        public object? ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string cp = parameter as string ?? string.Empty;
            return value is Visibility v && v == Visibility.Visible ? cp : cp == "mua" ? "xml2" : "mua";
        }
    }
    public partial class CharacterListCommands
    {
        [GeneratedRegex(@"(?<=charactername\W*)\b[\w\s&]+\b", RegexOptions.IgnoreCase, "en-CA")]
        public static partial Regex CharNameRX();  // optionally use [^;="\n], but should probably also not use \b

        private static Cfg Cfg { get; } = new();

        /// <summary>
        /// Load OHS JSON data from the default OHS ini & load the roster according to its settings.
        /// </summary>
        public static void LoadRoster() => LoadOHSsettings(Path.Combine(cdPath, Cfg.GUI.Game, "config.ini"));
        /// <summary>
        /// Load the roster from the saved settings.
        /// </summary>
        public static void LoadRosterVal() => LoadRosterVal(OHSsettings.Instance.RosterValue, Cfg.GUI.Game == "xml2" ? XML2settings.Instance.RosterValue : MUAsettings.Instance.MenulocationsValue);
        /// <summary>
        /// Load a roster by providing the roster value (filename without extension).
        /// </summary>
        public static void LoadRosterVal(string Roster) => LoadRosterVal(Roster, Roster);
        /// <summary>
        /// Load a roster by providing roster and menulocation values (filenames without extension).
        /// </summary>
        public static void LoadRosterVal(string Roster, string Mlv)
        {
            string m = Path.Combine(cdPath, Cfg.GUI.Game, "menulocations", $"{Mlv}.cfg");
            string r = Path.Combine(cdPath, Cfg.GUI.Game, "rosters", $"{Roster}.cfg");
            if (File.Exists(m) && File.Exists(r))
            {
                IEnumerable<int> Ml = File.ReadAllLines(m).Select(s => string.IsNullOrEmpty(s) ? 0 : int.Parse(s));
                LoadRoster(Ml.ToArray(), Cfg.Dynamic.LayoutLocs is null ? Ml : Ml.Intersect(Cfg.Dynamic.LayoutLocs), File.ReadAllLines(r));
            }
            else
            {
                Cfg.Dynamic.RosterRange = Enumerable.Range(1, XML2settings.Instance.RosterSize);
                LoadRoster(r);
            }
        }
        /// <summary>
        /// Load a roster by providing the full path to a roster file.
        /// </summary>
        public static void LoadRoster(string Roster)
        {
            if (File.Exists(Roster))
            {
                LoadRoster(File.ReadAllLines(Roster));
            }
        }
        /// <summary>
        /// Load a roster by providing an array of characters (paths). Uses the available locations. (For random.)
        /// </summary>
        public static void LoadRoster(string[] Roster)
        {
            IEnumerable<int>? LL = Cfg.GUI.Game == "mua"
                ? Cfg.Dynamic.LayoutLocs
                : Cfg.Dynamic.RosterRange;
            LoadRoster(LL is null ? [] : LL.ToArray(), LL, Roster);
        }
        /// <summary>
        /// Load a roster by providing an array or menulocations, a list of available locations and an array of characters (paths).
        /// </summary>
        public static void LoadRoster(int[] Locs, IEnumerable<int>? AvailableLocs, string[] Roster)
        {
            if (Locs is not null && AvailableLocs is not null)
            {
                Cfg.Roster.Selected.Clear();
                for (int i = 0; i < Math.Min(Locs.Length, Roster.Length); i++)
                {
                    if (AvailableLocs.Contains(Locs[i]))
                    {
                        _ = AddToSelected(Locs[i].ToString().PadLeft(2, '0'),
                                          Roster[i].Replace('\\', '/').Replace("*", string.Empty).Replace("?", string.Empty).Trim().Trim('/'),
                                          Roster[i].Contains('?'),
                                          Roster[i].Contains('*'));
                    }
                }
                UpdateClashes();
            }
        }
        /// <summary>
        /// Generate a random character list from the available characters and populate the available locations.
        /// </summary>
        public static void LoadRandomRoster()
        {
            if (Cfg.Roster.Available is not null)
            {
                // using a public static Random would assure that the generator doesn't use already used numbers anymore.
                // There is a complicated use of Random, combined with a Fisher-Yates-Shuffle worth mentioning: https://stackoverflow.com/questions/273313/randomize-a-listt
                Random rng = new();
                string[] RR = [.. Cfg.Roster.Available.OrderBy(a => rng.Next())];
                LoadRoster(RR);
            }
        }
        // Add Characters.
        public static bool AddToSelected(string? PathInfo) => AddToSelected(PathInfo, false);
        public static bool AddToSelected(string? PathInfo, bool Unl) => AddToSelected(PathInfo, Unl, false);
        public static bool AddToSelected(string? PathInfo, bool Unl, bool Start)
        {
            IEnumerable<int> AvailableLocs = (Cfg.GUI.Game == "xml2")
                ? Enumerable.Range(1, XML2settings.Instance.RosterSize)
                : Cfg.Dynamic.LayoutLocs is null
                ? Enumerable.Empty<int>()
                : Cfg.Dynamic.LayoutLocs;
            IEnumerable<int> OccupiedLocs = Cfg.Roster.Selected.Select(c => string.IsNullOrEmpty(c.Loc) ? 0 : int.Parse(c.Loc));
            IEnumerable<int> FreeLocs = AvailableLocs.Except(OccupiedLocs);
            return FreeLocs.Any() &&
                   AddToSelected(FreeLocs.ToImmutableSortedSet().First().ToString().PadLeft(2, '0'), PathInfo, Unl, Start);
        }
        public static bool AddToSelected(string? Loc, string? PathInfo) => AddToSelected(Loc, PathInfo, false, false);
        public static bool AddToSelected(string? Loc, string? PathInfo, bool Unl, bool Start)
        {
            if (LoadHerostat(PathInfo) is string[] HS
                && GetHerostatAttribute(HS, "skin") is string N
                && N[..^Math.Min(2, N.Length)] is string Number)
            {
                Number = string.IsNullOrEmpty(Number) || Number.Any(c => !char.IsDigit(c)) ? "00" : Number;
                bool RemOther = false;
                for (int i = 0; i < Cfg.Roster.Selected.Count; i++)
                {
                    SelectedCharacter SC = Cfg.Roster.Selected[i];
                    RemOther = SC.Path == PathInfo || RemOther;
                    if (SC.Loc == Loc || SC.Path == PathInfo) { Cfg.Roster.Selected.RemoveAt(i); }
                }
                Cfg.Roster.Selected.Add(new SelectedCharacter
                {
                    Loc = Loc ?? "",
                    Character_Name = GetHerostatAttribute(HS, "charactername"),
                    Path = PathInfo,
                    Character_Number = Number,
                    Unlock = Unl,
                    Starter = Start
                });
                return RemOther;
            }
            return false;
        }
        public static void UpdateClashes()
        {
            Cfg.Roster.NumClash = false;
            string[] Check = new string[Cfg.Roster.Selected.Count];
            for (int i = 0; i < Check.Length; i++)
            {
                if (Cfg.Roster.Selected[i] is SelectedCharacter SC && SC.Character_Number is string N)
                {
                    SC.NumClash = Array.IndexOf(Check, N) is int d && d > -1;
                    if (d > -1) { Cfg.Roster.Selected[d].NumClash = d > -1; }
                    Cfg.Roster.NumClash = SC.NumClash || Cfg.Roster.NumClash;
                    Check[i] = N;
                }
            }
        }
        public static void AddHerostat(StorageFile Herostat) => AddHerostat(Herostat.Path, Herostat.Name, Herostat.FileType);
        public static void AddHerostat(string Herostat) => AddHerostat(Herostat, Herostat, Path.GetExtension(Herostat));
        public static void AddHerostat(string HSpath, string HSname, string HSext)
        {
            string[] HSlines = File.ReadAllLines(HSpath);
            string? Name = HSlines.FirstOrDefault(l => l.Contains("charactername", StringComparison.CurrentCultureIgnoreCase)) is string CharLine
                && CharNameRX().Match(CharLine) is Match M && M.Success
                ? M.Value
                : Path.GetFileNameWithoutExtension(HSname);
            DirectoryInfo Hsf = Directory.CreateDirectory(GetHerostatFolder());
            string Target = Path.Combine(Hsf.FullName, Name + HSext);
            if (File.Exists(Target)) Target = Path.Combine(Hsf.FullName, Name + $"{DateTime.Now:-yyMMdd-HHmmss}" + HSext);
            File.Copy(HSpath, Target, true);
        }
        /// <summary>
        /// Get the full path to the herostat folder.
        /// </summary>
        /// <returns>Full path to the herostat folder</returns>
        public static string GetHerostatFolder() => GetOHSFolder(OHSsettings.Instance.HerostatFolder);
        /// <summary>
        /// Get the full path to the herostat file, providing the path relative in the herostat folder.
        /// </summary>
        /// <returns>Full path to the herostat folder</returns>
        public static FileInfo? GetHerostatFile(string HsPath)
        {
            if (Path.Combine(GetHerostatFolder(), HsPath) is string FolderString
                && FolderString.Replace('\\', '/').LastIndexOf('/') is int S
                && new DirectoryInfo(FolderString[..S].TrimEnd('/')) is DirectoryInfo folder
                && folder.Exists
                && folder.EnumerateFiles($"{FolderString[(S + 1)..]}.??????????").FirstOrDefault() is FileInfo HS)
            {
                return HS;
            }
            return null;
        }

        /// <summary>
        /// Get the full path to an OHS game sub folder.
        /// </summary>
        /// <returns>Full path to an OHS game sub folder</returns>
        public static string GetOHSFolder(string FolderString)
        {
            return Path.IsPathRooted(FolderString)
                ? FolderString
                : Cfg.GUI.Game == ""
                ? Path.Combine(cdPath, "mua", FolderString)
                : Path.Combine(cdPath, Cfg.GUI.Game, FolderString);
        }
        /// <summary>
        /// Load herostat from the selected character.
        /// </summary>
        public static string[]? LoadHerostat(string? FC)
        {
            if (!string.IsNullOrEmpty(FC)
                && GetHerostatFile(FC) is FileInfo HS)
            {
                string[] Herostat = File.ReadLines(HS.FullName).Where(l => l.Trim() != "").ToArray();
                Cfg.Dynamic.HsPath = HS;
                Cfg.Dynamic.HsFormat = Herostat[0].Trim()[0];
                return Herostat;
            }
            return null;
        }
        /// <summary>
        /// Gets the internal name from the currently selected character
        /// </summary>
        /// <returns>The internal name or empty string</returns>
        public static string GetInternalName()
        {
            return LoadHerostat(Cfg.Dynamic.FloatingCharacter) is string[] HS ? GetHerostatAttribute(HS, "name") : string.Empty;
        }
        /// <summary>
        /// Read the herostat for a root attribute by providing a herostat string[], regardless of the format.
        /// </summary>
        /// <returns>The value of the attribute</returns>
        public static string GetHerostatAttribute(string[] Herostat, string AttrName)
        {
            return Herostat[0].Trim()[0] == '<' ? GetRootAttribute(Herostat, AttrName) : GetFakeXmlJsonAttr(Herostat, AttrName);
        }
        /// <summary>
        /// Search a fake XML or JSON file for an attribute. Ignores parent/child relations. Treats nodes as attributes but returns empty.
        /// </summary>
        /// <param name="array">An array of strings (lines), read from a fake XML or JSON file</param>
        /// <param name="name">The name of the attribute</param>
        /// <returns>The value of the first matching attribute</returns>
        public static string GetFakeXmlJsonAttr(string[] array, string name)
        {
            Regex RXstartsWith = new($"^\"?{name}(\": | =)");
            string[] Lines = Array.FindAll(array, c => RXstartsWith.IsMatch(c.Trim().ToLower()));
            if (Lines.Length != 0)
            {
                DynamicSettings.Instance.HsFormat = Lines[0].Trim()[0] == '"' ? ':' : '=';
                return Lines[0].Split([DynamicSettings.Instance.HsFormat], 2)[1].TrimEnd(';').TrimEnd(',').Trim().Trim('"');
            }
            return string.Empty;
        }
    }
}
