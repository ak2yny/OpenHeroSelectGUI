using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Storage;
using static OpenHeroSelectGUI.Settings.CfgCommands;
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI.Settings
{
    public class Character
    {
        public string? Path { get; set; }
        public string? Name { get; set; }
    }
    public partial class Available : ObservableRecipient
    {
        public Character Character { get; set; } = new();
        public ObservableCollection<Available> Children { get; set; } = new();
        [ObservableProperty]
        private bool isExpanded;
    }
    /// <summary>
    /// Selected Characters: Definition
    /// </summary>
    public partial class CharacterLists : ObservableRecipient
    {
        public ObservableCollection<Available> AvailableCharacterList { get; set; } = new();
        public ObservableCollection<SelectedCharacter> Selected { get; set; } = new();
        [ObservableProperty]
        private string[]? available;
        [ObservableProperty]
        private int total;
        [ObservableProperty]
        private int count;

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
        public bool unlock;
        [ObservableProperty]
        public bool starter;
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
            return DynamicSettings.Instance.Game == "xml2" ? XML2 : MUA;
        }
    }
    public class CharacterListCommands
    {
        private static Cfg Cfg { get; } = new();
        /// <summary>
        /// Load OHS JSON data from the default OHS ini & load the roster according to its settings.
        /// </summary>
        public static void LoadRoster() => LoadOHSsettings(Path.Combine(cdPath, Cfg.Dynamic.Game, "config.ini"));
        /// <summary>
        /// Load the roster from the saved settings.
        /// </summary>
        public static void LoadRosterVal() => LoadRosterVal(OHSsettings.Instance.RosterValue, Cfg.Dynamic.Game == "xml2" ? XML2settings.Instance.RosterValue : MUAsettings.Instance.MenulocationsValue);
        /// <summary>
        /// Load a roster by providing the roster value (filename without extension).
        /// </summary>
        public static void LoadRosterVal(string Roster) => LoadRosterVal(Roster, Roster);
        /// <summary>
        /// Load a roster by providing roster and menulocation values (filenames without extension).
        /// </summary>
        public static void LoadRosterVal(string Roster, string Mlv)
        {
            string m = Path.Combine(cdPath, Cfg.Dynamic.Game, "menulocations", $"{Mlv}.cfg");
            string r = Path.Combine(cdPath, Cfg.Dynamic.Game, "rosters", $"{Roster}.cfg");
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
            IEnumerable<int>? LL = Cfg.Dynamic.Game == "mua"
                ? Cfg.Dynamic.LayoutLocs
                : Cfg.Dynamic.RosterRange;
            LoadRoster(LL is null ? Array.Empty<int>() : LL.ToArray(), LL, Roster);
        }
        /// <summary>
        /// Load a roster by providing an array or menulocations, a list of available locations and an array of characters (paths).
        /// </summary>
        public static void LoadRoster(int[] Locs, IEnumerable<int>? AvailableLocs, string[] Roster)
        {
            if (Locs is not null && AvailableLocs is not null)
            {
                Cfg.Roster.Selected.Clear();
                Cfg.Roster.Count = 0;
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
                string[] RR = Cfg.Roster.Available.OrderBy(a => rng.Next()).ToArray();
                LoadRoster(RR);
            }
        }
        // Add Characters: need to check these in the end. some might be unused.
        public static bool AddToSelected(string? PathInfo) => AddToSelected(PathInfo, false);
        public static bool AddToSelected(string? PathInfo, bool Unl) => AddToSelected(PathInfo, Unl, false);
        public static bool AddToSelected(string? PathInfo, bool Unl, bool Start)
        {
            IEnumerable<int> AvailableLocs = (Cfg.Dynamic.Game == "xml2")
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
            if (string.IsNullOrEmpty(PathInfo)) return false;

            string FolderString = Path.Combine(GetHerostatFolder(), PathInfo);
            int S = FolderString.Replace('\\', '/').LastIndexOf('/');
            DirectoryInfo Folder = new(FolderString[..S].TrimEnd('/'));
            if (!Folder.Exists || !Folder.EnumerateFiles($"{FolderString[(S + 1)..]}.??????????").Any()) return false;

            _ = Cfg.Roster.Selected.Remove(Cfg.Roster.Selected.FirstOrDefault(c => c.Loc == Loc));
            bool RemOther = Cfg.Roster.Selected.Remove(Cfg.Roster.Selected.FirstOrDefault(c => c.Path == PathInfo));
            Cfg.Roster.Selected.Add(new SelectedCharacter { Loc = Loc ??= "", Character_Name = FolderString[(S + 1)..], Path = PathInfo, Unlock = Unl, Starter = Start });
            Cfg.Roster.Count = Cfg.Roster.Selected.Count;

            return RemOther;
        }
        public static void AddHerostat(StorageFile Herostat) => AddHerostat(Herostat.Path, Herostat.Name, Herostat.FileType);
        public static void AddHerostat(string Herostat) => AddHerostat(Herostat, Herostat, Path.GetExtension(Herostat));
        public static void AddHerostat(string HSpath, string HSname, string HSext)
        {
            Regex rx = new(@"(?<=charactername\W*)\b[\w\s&]+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            string[] HSlines = File.ReadAllLines(HSpath);
            string? CharLine = HSlines.FirstOrDefault(l => l.ToLower().Contains("charactername"));
            string? Name = (CharLine is null || rx.Matches(CharLine).Count == 0) ?
                Path.GetFileNameWithoutExtension(HSname) :
                rx.Match(CharLine).Value;
            DirectoryInfo Hsf = Directory.CreateDirectory(GetHerostatFolder());
            string Target = Path.Combine(Hsf.FullName, Name + HSext);
            if (File.Exists(Target)) Target = Path.Combine(Hsf.FullName, Name + $"{DateTime.Now:-yyMMdd-HHmmss}" + HSext);
            File.Copy(HSpath, Target, true);
        }
        public static string GetHerostatFolder()
        {
            return Path.IsPathRooted(OHSsettings.Instance.HerostatFolder)
                ? OHSsettings.Instance.HerostatFolder
                : Path.Combine(cdPath, Cfg.Dynamic.Game, OHSsettings.Instance.HerostatFolder);
        }
    }
}
