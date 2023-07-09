using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Storage;
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
        public override string ToString()
        {
            return (Character is null || Character.Path is null) ?
                "" :
                Character.Path;
        }
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
    public class SelectedCharacter
    {
        public string? Loc { get; set; }
        public string? Character_Name { get; set; }
        public string? Path { get; set; }
        public bool Unlock { get; set; }
        public bool Starter { get; set; }
    }
    public class CharacterListCommands
    {
        private static Cfg Cfg { get; } = new();
        /// <summary>
        /// Load the XML2 roster, using the saved settings.
        /// </summary>
        public static void LoadRosterXML2() => LoadRoster();
        /// <summary>
        /// Load the MUA roster, using the saved settings.
        /// </summary>
        public static void LoadRosterMUA() => LoadRosterVal(Cfg.MUA.RosterValue, Cfg.MUA.MenulocationsValue);
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
                LoadRoster(r);
            }
        }
        /// <summary>
        /// Load the saved roster using available locations (LayoutLocs).
        /// </summary>
        public static void LoadRoster() => LoadRoster(Path.Combine(cdPath, Cfg.GUI.Game, "rosters", $"{Cfg.OHS.RosterValue}.cfg"));
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
        public static void LoadRoster(string[] Roster) => LoadRoster(Cfg.Dynamic.LayoutLocs is null ? Array.Empty<int>() : Cfg.Dynamic.LayoutLocs.ToArray(), Cfg.Dynamic.LayoutLocs, Roster);
        /// <summary>
        /// Load a roster by providing an array or menulocations, a list of available locations and an array of characters (paths).
        /// </summary>
        public static void LoadRoster(int[] Locs, IEnumerable<int>? AvailableLocs, string[] Roster)
        {
            if (Locs is not null && AvailableLocs is not null)
            {
                Cfg.Roster.Selected.Clear();
                Cfg.Roster.Count = 0;
                int[] IL = Locs.ToArray();
                for (int i = 0; i < Math.Min(IL.Length, Roster.Length); i++)
                {
                    if (AvailableLocs.Contains(IL[i]))
                    {
                        _ = AddToSelected(IL[i].ToString().PadLeft(2, '0'),
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
            IEnumerable<int> AvailableLocs = (Cfg.GUI.Game == "xml2") ?
                Enumerable.Range(1, Cfg.XML2.RosterSize) :
                Cfg.Dynamic.LayoutLocs is null ? Enumerable.Empty<int>() :
                Cfg.Dynamic.LayoutLocs;
            IEnumerable<int> OccupiedLocs = Cfg.Roster.Selected.Select(c => string.IsNullOrEmpty(c.Loc) ? 0 : int.Parse(c.Loc));
            IEnumerable<int> FreeLocs = AvailableLocs.Except(OccupiedLocs);
            return FreeLocs.Any() &&
                   AddToSelected(FreeLocs.ToImmutableSortedSet().First().ToString().PadLeft(2, '0'), PathInfo, Unl, Start);
        }
        public static bool AddToSelected(string? Loc, string? PathInfo) => AddToSelected(Loc, PathInfo, false, false);
        public static bool AddToSelected(string? Loc, string? PathInfo, bool Unl, bool Start)
        {
            if (string.IsNullOrEmpty(PathInfo)) return false;

            string FolderString = Path.Combine(Cfg.GetHerostatFolder(), PathInfo);
            int S = FolderString.Replace('\\', '/').LastIndexOf('/');
            DirectoryInfo Folder = new(FolderString[..S].TrimEnd('/'));
            if (!Folder.Exists || !Folder.EnumerateFiles($"{FolderString[(S + 1)..]}.??????????").Any()) return false;

            _ = Cfg.Roster.Selected.Remove(Cfg.Roster.Selected.FirstOrDefault(c => c.Loc == Loc));
            bool RemOther = Cfg.Roster.Selected.Remove(Cfg.Roster.Selected.FirstOrDefault(c => c.Path == PathInfo));
            Cfg.Roster.Selected.Add(new SelectedCharacter { Loc = Loc ??= "", Character_Name = FolderString[(S + 1)..], Path = PathInfo, Unlock = Unl, Starter = Start });
            Cfg.Roster.Count = Cfg.Roster.Selected.Count;

            return RemOther;
        }
        public static void AddHerostat(StorageFile Herostat)
        {
            Regex rx = new(@"(?<=charactername\W*)\b[\w\s&]+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            string[] HSlines = File.ReadAllLines(Herostat.Path);
            string? CharLine = HSlines.FirstOrDefault(l => l.ToLower().Contains("charactername"));
            string? Name = (CharLine is null || rx.Matches(CharLine).Count == 0) ?
                Path.GetFileNameWithoutExtension(Herostat.Name) :
                rx.Match(CharLine).Value;
            DirectoryInfo Hsf = Directory.CreateDirectory(Cfg.GetHerostatFolder());
            string Target = Path.Combine(Hsf.FullName, Name + Herostat.FileType);
            if (File.Exists(Target)) Target = Path.Combine(Hsf.FullName, Name + $"{DateTime.Now:-yyMMdd-HHmmss}" + Herostat.FileType);
            //using (FileStream SS = File.Open(Herostat.Path, FileMode.Open))
            //{
            //    using FileStream TS = File.Create(Target);
            //    await SS.CopyToAsync(TS);
            //}
            File.Copy(Herostat.Path, Target, true);
        }
    }
}
