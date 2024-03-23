using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Functions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// <see cref="ObservableCollection{T}"/>s for <see cref="Selected"/> characters, <see cref="Teams"/>, <see cref="SkinsList"/> of the currently selected character, plus some observable properties
    /// </summary>
    public partial class CharacterLists : ObservableRecipient
    {
        public ObservableCollection<SelectedCharacter> Selected { get; set; } = [];
        public ObservableCollection<TeamBonus> Teams { get; set; } = [];
        public ObservableCollection<TeamBonus> TeamsMUA { get; set; } = [];
        public ObservableCollection<TeamBonus> TeamsXML2 { get; set; } = [];
        public ObservableCollection<SkinDetails> SkinsList { get; set; } = [];
        [ObservableProperty]
        private string[]? available;
        [ObservableProperty]
        private int total;
        [ObservableProperty]
        private bool numClash;
        [ObservableProperty]
        private int updateCount;
    }
    /// <summary>
    /// Selected Character Class
    /// </summary>
    public partial class SelectedCharacter : ObservableRecipient
    {
        [ObservableProperty]
        private string? loc;
        [ObservableProperty]
        private string? character_Name;
        [ObservableProperty]
        private string? path;
        [ObservableProperty]
        private string? character_Number;
        [ObservableProperty]
        private bool unlock;
        [ObservableProperty]
        private bool starter;
        [ObservableProperty]
        private string? effect;
        [ObservableProperty]
        private bool numClash;

        public List<string> AvailableEffects = GUIXML.GetAvailableEffects();
    }
    /// <summary>
    /// Template selector helper for <see cref="ListView"/>s and similar, filtering by current game tab
    /// </summary>
    public class GameTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? MUA { get; set; }
        public DataTemplate? XML2 { get; set; }

        protected override DataTemplate? SelectTemplateCore(object item)
        {
            return CfgSt.GUI.Game == "xml2" ? XML2 : MUA;
        }
    }
    /// <summary>
    /// Functions related to populating the <see cref="CharacterLists"/>
    /// </summary>
    public partial class CharacterListCommands
    {
        private static Cfg Cfg { get; } = new();

        /// <summary>
        /// Browse for a roster file to load.
        /// </summary>
        public static async void LoadRosterBrowse() => LoadRoster(await CfgCmd.LoadDialogue(".cfg"));
        /// <summary>
        /// Load OHS JSON data from the default OHS config.ini & load the roster according to its settings.
        /// </summary>
        public static void LoadRoster() => CfgCmd.LoadOHSsettings(Path.Combine(OHSpath.CD, Cfg.GUI.Game, "config.ini"));
        /// <summary>
        /// Load the roster from the saved settings.
        /// </summary>
        public static void LoadRosterVal() => LoadRosterVal(CfgSt.OHS.RosterValue, Cfg.GUI.Game == "xml2" ? CfgSt.XML2.RosterValue : CfgSt.MUA.MenulocationsValue);
        /// <summary>
        /// Load a roster by providing the <paramref name="Roster"/> value (filename without extension).
        /// </summary>
        public static void LoadRosterVal(string Roster) => LoadRosterVal(Roster, Roster);
        /// <summary>
        /// Load a roster by providing <paramref name="Roster"/> and <paramref name="Mlv"/> (Menulocation) values (filenames without extension).
        /// </summary>
        public static void LoadRosterVal(string Roster, string Mlv)
        {
            string m = Path.Combine(OHSpath.CD, Cfg.GUI.Game, "menulocations", $"{Mlv}.cfg");
            string r = Path.Combine(OHSpath.CD, Cfg.GUI.Game, "rosters", $"{Roster}.cfg");
            if (File.Exists(m) && File.Exists(r))
            {
                IEnumerable<int> Ml = File.ReadAllLines(m).Select(s => string.IsNullOrEmpty(s) ? 0 : int.Parse(s));
                LoadRoster(Ml.ToArray(), Cfg.Var.LayoutLocs is null ? Ml : Ml.Intersect(Cfg.Var.LayoutLocs), File.ReadAllLines(r));
            }
            else
            {
                Cfg.Var.RosterRange = Enumerable.Range(1, CfgSt.XML2.RosterSize);
                LoadRoster(r);
            }
        }
        /// <summary>
        /// Load a roster by providing the full path to a <paramref name="Roster"/> file.
        /// </summary>
        public static void LoadRoster(string? Roster)
        {
            if (!string.IsNullOrWhiteSpace(Roster) && File.Exists(Roster))
            {
                LoadRoster(File.ReadAllLines(Roster));
            }
        }
        /// <summary>
        /// Load a roster by providing a <paramref name="Roster"/> <see langword="string[]"/> of characters (paths). Uses the available locations. (For random.)
        /// </summary>
        public static void LoadRoster(string[] Roster)
        {
            IEnumerable<int>? LL = Cfg.GUI.Game == "xml2"
                ? Cfg.Var.RosterRange
                : Cfg.Var.LayoutLocs;
            LoadRoster(LL is null ? [] : LL.ToArray(), LL, Roster);
        }
        /// <summary>
        /// Load a roster by providing an <see langword="int[]"/> of menulocations, a <see cref="IEnumerable{int}"/> of <paramref name="AvailableLocs"/> and a <paramref name="Roster"/> <see langword="string[]"/> of characters (paths).
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
        /// Generate a <see cref="Random"/> character list from the available characters and populate the available locations.
        /// </summary>
        public static void LoadRandomRoster()
        {
            if (Cfg.Roster.Available is not null)
            {
                // using a public static Random would assure that the generator doesn't use already used numbers anymore.
                // There is a complicated use of Random, combined with a Fisher-Yates-Shuffle worth mentioning: https://stackoverflow.com/questions/273313/randomize-a-listt
                Random rng = new();
                LoadRoster([.. Cfg.Roster.Available.OrderBy(a => rng.Next())]);
            }
        }
        // Add Characters functions.
        public static bool AddToSelected(string? PathInfo) => AddToSelected(PathInfo, false);
        public static bool AddToSelected(string? PathInfo, bool Unl) => AddToSelected(PathInfo, Unl, false);
        public static bool AddToSelected(string? PathInfo, bool Unl, bool Start)
        {
            IEnumerable<int> AvailableLocs = (Cfg.GUI.Game == "xml2")
                ? Enumerable.Range(1, CfgSt.XML2.RosterSize)
                : Cfg.Var.LayoutLocs is null
                ? Enumerable.Empty<int>()
                : Cfg.Var.LayoutLocs;
            IEnumerable<int> OccupiedLocs = Cfg.Roster.Selected.Select(c => string.IsNullOrEmpty(c.Loc) ? 0 : int.Parse(c.Loc));
            IEnumerable<int> FreeLocs = AvailableLocs.Except(OccupiedLocs);
            return FreeLocs.Any() &&
                   AddToSelected(FreeLocs.ToImmutableSortedSet().First().ToString().PadLeft(2, '0'), PathInfo, Unl, Start);
        }
        public static bool AddToSelected(string? Loc, string? PathInfo) => AddToSelected(Loc, PathInfo, false, false);
        public static bool AddToSelected(string? Loc, string? PathInfo, bool Unl, bool Start)
        {
            if (Herostat.Load(PathInfo) is string[] HS
                && Herostat.RootAttribute(HS, "skin") is string N
                && N[..^Math.Min(2, N.Length)] is string Number)
            {
                Number = string.IsNullOrEmpty(Number) || Number.Any(c => !char.IsDigit(c)) ? "00" : Number.PadLeft(2, '0');
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
                    Character_Name = Herostat.RootAttribute(HS, "charactername"),
                    Path = PathInfo,
                    Character_Number = Number,
                    Unlock = Unl,
                    Starter = Start
                });
                return RemOther;
            }
            return false;
        }
        /// <summary>
        /// Update the number clash property of all selected characters and the global number clash <see cref="bool"/>. If <paramref name="UpdLocs"/> is <see langword="false"/>, doesn't increment count to invoke MUA locations update.
        /// </summary>
        /// <param name="UpdLocs"></param>
        public static void UpdateClashes(bool UpdLocs = true)
        {
            // This invokes the text changed event in MUA through binding, which then starts the update locations function
            Cfg.Roster.UpdateCount += UpdLocs ? 1 : 0;
            Cfg.Roster.NumClash = false;
            if ((Cfg.GUI.Game == "xml2" && Cfg.GUI.ShowClashes != 1) || Cfg.GUI.ShowClashes == 2) { return; }
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
    }
}
