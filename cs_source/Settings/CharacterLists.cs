using CommunityToolkit.Mvvm.ComponentModel;
using OpenHeroSelectGUI.Functions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// <see cref="ObservableCollection{T}"/>s for <see cref="Selected"/> characters, <see cref="Teams"/> of the currently selected character, plus some observable properties
    /// </summary>
    internal partial class CharacterLists : ObservableObject
    {
        internal ObservableCollection<AvailableCharacter> AvailableFiltered { get; set; } = [];
        internal string[] AvailableAll { get; private set; } = [];
        private string[] AvailableNeedy { get; set; } = [];
        internal string[] Available => NeedyCharsOnly ? AvailableNeedy : AvailableAll;

        [ObservableProperty]
        internal partial bool AvailFilteringNeedy { get; private set; }

        internal bool NeedyCharsOnly;

        internal bool AvailableInitialized;

        internal ObservableCollection<SelectedCharacter> Selected { get; set; } = [];

        internal readonly BitArray SelectedLocs = new(100);

        [ObservableProperty]
        internal partial int Total { get; set; }

        [ObservableProperty]
        internal partial bool NumClash { get; set; }

        internal ObservableCollection<Bonus> Teams { get; set; }
        internal ObservableCollection<Bonus> TeamsMUA { get; set; } = [];
        internal ObservableCollection<Bonus> TeamsXML2 { get; set; } = [];

        internal CharacterLists()
        {
            Teams = CfgSt.GUI.IsMua ? TeamsMUA : TeamsXML2;
        }
        /// <summary>
        /// Load characters from the herostat folder.
        /// </summary>
        private async System.Threading.Tasks.Task UpdateAvailable()
        {
            if (!AvailableInitialized || AvailableAll.Length == 0)
            {
                DirectoryInfo folder = new(OHSpath.HsFolder);
                int BasePathLen = folder.FullName.Length + 1;
                AvailableAll = folder.Exists
                    ? [.. folder.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Select(f => f.FullName[BasePathLen..^f.Extension.Length])
                            .Distinct(StringComparer.OrdinalIgnoreCase)]
                    : [];
            }
            if (NeedyCharsOnly)
            {
                AvailFilteringNeedy = true;
                await System.Threading.Tasks.Task.Run(() =>
                {
                    AvailableNeedy = [.. AvailableAll
                        .Where(n => !Zsnd.Lib.Lists.XVInternalNames
                            .Contains(Herostat.GetStats(n).InternalName, StringComparer.OrdinalIgnoreCase))];
                });
                AvailFilteringNeedy = false;
            }
            AvailableInitialized = true;
        }
        /// <summary>
        /// Populate <see cref="AvailableFiltered"/> for a tree view with the file structure from the <paramref name="AvChar"/> path and update <see cref="AvailableAll"/> and <see cref="AvailableNeedy"/>.
        /// </summary>
        internal void AddAvailable(string AvChar)
        {
            // WIP: Does this slow down noticeably when adding multiple files?
            AvailableAll = [.. AvailableAll, AvChar];
            bool isNeedy = !Zsnd.Lib.Lists.XVInternalNames.Contains(Herostat.GetStats(AvChar).InternalName);
            if (isNeedy) { AvailableNeedy = [.. AvailableNeedy, AvChar]; }
            if (!NeedyCharsOnly || isNeedy) { PopulateAvailable(new() { Children = AvailableFiltered }, AvChar, AvChar); }
        }
        /// <summary>
        /// Load characters from the herostat folder. and populate <see cref="AvailableFiltered"/>.
        /// </summary>
        internal async void PopulateAvailable()
        {
            await UpdateAvailable();
            AvailableFiltered.Clear();
            if (AvailableAll.Length == 0)
            {
                AvailableFiltered.Add(new AvailableCharacter([], new Character() { Name = "Drop herostats here to install" }));
                return;
            }
            else { PopulateAvailable(new() { Children = AvailableFiltered }, Available); } // possibly make async
        }
        /// <summary>
        /// Populate <see cref="AvailableFiltered"/> for a tree view with the file structure from the <paramref name="AvailableList"/> (a array of paths), applying <paramref name="Filter"/>.
        /// </summary>
        internal void PopulateAvailable(string Filter)
        {
            string[] AvailableList = Available;
            AvailableFiltered.Clear();
            AvailableCharacter Root = new() { Children = AvailableFiltered };
            // possibly make async
            if (Filter == "") { PopulateAvailable(Root, AvailableList); return; }
            for (int i = 0; i < AvailableList.Length; i++)
            {
                if (AvailableList[i].Contains(Filter, StringComparison.CurrentCultureIgnoreCase))
                { PopulateAvailable(Root, AvailableList[i], AvailableList[i]); }
            }
        }
        /// <summary>
        /// Populate <see cref="AvailableFiltered"/> for a tree view with the file structure from the <paramref name="AvailableList"/> (a array of paths).
        /// </summary>
        private static void PopulateAvailable(AvailableCharacter Root, string[] AvailableList)
        {
            for (int i = 0; i < AvailableList.Length; i++) { PopulateAvailable(Root, AvailableList[i], AvailableList[i]); }
        }
        /// <summary>
        /// Generate a tree view structure from a file <paramref name="PathInfo"/>, by adding <paramref name="RemainingPath"/> to <paramref name="Parent"/> recursively.
        /// </summary>
        private static void PopulateAvailable(AvailableCharacter Parent, string RemainingPath, string PathInfo)
        {
            string[] Nodes = RemainingPath.Split(Path.DirectorySeparatorChar, 2);
            bool IsDir = Nodes.Length > 1;
            AvailableCharacter? child = Parent.Children.SingleOrDefault(x => x.Content.Name == Nodes[0]);
            if (child is null)
            {
                child = new(Parent.Children, new Character()
                {
                    Name = Nodes[0],
                    Path = IsDir ? PathInfo[..^Nodes[1].Length] : PathInfo
                });
                if (IsDir)
                {
                    int i = 0; for (; i < Parent.Children.Count; i++) { if (!Parent.Children[i].HasChildren) { break; } }
                    Parent.Children.Insert(i, child);
                }
                else
                {
                    Parent.Children.Add(child);
                }
            }
            if (IsDir) { PopulateAvailable(child, Nodes[1], PathInfo); }
        }
        /// <summary>
        /// Add <see cref="SelectedCharacter"/> <paramref name="SC"/>.
        /// </summary>
        internal void AddSelected(SelectedCharacter SC, bool UpdateBoxes = false, int Index = -1)
        {
            if (Index == -1) { Selected.Add(SC); } else { Selected.Insert(Index, SC); }
            SelectedLocs.Set(SC.LocNum, true);
            if (UpdateBoxes) { CfgSt.CSS.UpdateLocBoxes(SC.LocNum, true); }
        }
        /// <summary>
        /// Remove <see cref="SelectedCharacter"/> <paramref name="SC"/>, using optional <paramref name="index"/> for faster removal. Upates MUA location boxes.
        /// </summary>
        internal void RemoveSelected(SelectedCharacter SC, int index = -1)
        {
            SelectedLocs.Set(SC.LocNum, false);
            if (index == -1) { _ = Selected.Remove(SC); } else { Selected.RemoveAt(index); }
            if (CfgSt.GUI.IsMua) { CfgSt.CSS.UpdateLocBoxes(SC.LocNum, false); }
        }
        /// <summary>
        /// Remove <see cref="SelectedCharacter"/> at <paramref name="index"/>. Doesn't update MUA location boxes.
        /// </summary>
        internal void RemoveSelected(int index)
        {
            SelectedLocs.Set(Selected[index].LocNum, false);
            Selected.RemoveAt(index);
        }
        /// <summary>
        /// Remove all <see cref="SelectedCharacter"/>s from <see cref="Selected"/>.
        /// </summary>
        internal void ClearSelected()
        {
            Selected.Clear();
            SelectedLocs.SetAll(false);
            NumClash = false;
        }
    }
    /// <summary>
    /// Functions related to populating the <see cref="CharacterLists"/>
    /// </summary>
    internal static class CharacterListCommands
    {
        /// <summary>
        /// Browse for a roster file to load. Takes identically named menulocation file, if it exists.
        /// </summary>
        internal static async void LoadRosterBrowse()
        {
            string Roster = await CfgCmd.LoadDialogue(".cfg") ?? string.Empty;
            LoadRoster(Roster, Path.Combine(OHSpath.CD, "mua", "menulocations", Path.GetFileName(Roster)));
        }
        /// <summary>
        /// Load OHS JSON data from the default OHS config.ini & load the roster according to its settings.
        /// </summary>
        internal static void LoadRoster() => CfgCmd.LoadOHSsettings(Path.Combine(OHSpath.CD, OHSpath.Game, "config.ini"));
        /// <summary>
        /// Load the roster from the saved settings.
        /// </summary>
        internal static void LoadRosterVal() => LoadRosterVal(CfgSt.OHS.RosterValue, CfgSt.GUI.IsMua ? CfgSt.MUA.MenulocationsValue : CfgSt.XML2.RosterValue);
        /// <summary>
        /// Load a roster by providing <paramref name="Roster"/> and <paramref name="Mlv"/> (Menulocation) values (filenames without extension).
        /// </summary>
        internal static void LoadRosterVal(string Roster, string Mlv = "")
        {
            LoadRoster(Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", $"{Roster}.cfg"), Path.Combine(OHSpath.CD, OHSpath.Game, "menulocations", $"{Mlv}.cfg"));
        }
        /// <summary>
        /// Load a roster by providing the <paramref name="r"/>oster and <paramref name="m"/>enulocation paths.
        /// </summary>
        private static void LoadRoster(string r, string m)
        {
            if (!File.Exists(r)) { return; }
            if (File.Exists(m))
            {
                CfgSt.CSS.SetLocationsMUA();
                LoadRoster([.. CfgSt.CSS.Locs.Length == 0
                                ? File.ReadAllLines(m).Select(static s => int.TryParse(s, out int n) ? n : 0) // or other fallback than 0?
                                : File.ReadAllLines(m).Select(static s => int.TryParse(s, out int n) ? n : 0).Intersect(CfgSt.CSS.Locs)],
                           File.ReadAllLines(r));
            }
            else
            {
                if (CfgSt.GUI.IsXml2) { CfgSt.CSS.SetDefaultRosterXML2(); } else { CfgSt.CSS.SetLocationsMUA(); }
                LoadRoster(File.ReadAllLines(r));
            }
        }
        /// <summary>
        /// Load a roster by providing a <paramref name="Roster"/> array of characters (paths). Uses the available locations. (For random.)
        /// </summary>
        private static void LoadRoster(string[] Roster) => LoadRoster(CfgSt.CSS.Locs, Roster);
        /// <summary>
        /// Load a roster by providing a <paramref name="Locs"/> array of menulocations and a <paramref name="Roster"/> array of characters (paths).
        /// </summary>
        private static void LoadRoster(int[] Locs, string[] Roster)
        {
            CfgSt.Roster.ClearSelected();
            int Count = Math.Min(Locs.Length, Roster.Length);
            for (int i = 0; i < Count; i++)
            {
                AddToSelectedBulk(Locs[i],
                    Roster[i].Replace('\\', '/').Replace("*", "").Replace("?", "").Trim().Trim('/'),
                    Roster[i].Contains('?'),
                    Roster[i].Contains('*'));
            }
            if (CfgSt.GUI.IsMua) { CfgSt.CSS.UpdateLocBoxes(); }
            UpdateClashes();
        }
        /// <summary>
        /// Generate a <see cref="Random"/> character list from the available characters and populate the available locations.
        /// </summary>
        internal static void LoadRandomRoster()
        {
            if (CfgSt.Roster.AvailableAll.Length != 0)
            {
                // There is a complicated use of Random, combined with a Fisher-Yates-Shuffle worth mentioning: https://stackoverflow.com/questions/273313/randomize-a-listt
                string[] Available = (string[])CfgSt.Roster.AvailableAll.Clone();
                Random.Shared.Shuffle(Available);
                // Faster but less random:
                //int Max = Math.Min(CfgSt.Roster.Total, Available.Length);
                //Random rng = new();
                //for (int i = 0, r; i < Max; i++) { r = rng.Next(Available.Length); (Available[i], Available[r]) = (Available[r], Available[i]); }
                LoadRoster(Available);
            }
        }
        // Add Characters functions:
        internal static void AddToSelected(string PathInfo, int Index = -1)
        {
            for (int i = 0; i < CfgSt.CSS.Locs.Length; i++)
            {
                int Loc = CfgSt.CSS.Locs[i];
                if (!CfgSt.Roster.SelectedLocs.Get(Loc)) { AddToSelected(Loc, PathInfo, Index: Index); break; }
            }
        }

        internal static void AddToSelected(int Loc, string PathInfo, bool Unl = false, bool Start = false, int Index = -1)
        {
            SelectedCharacter New = ToSelected(Loc, PathInfo, Unl, Start);
            bool Removed = false;
            for (int i = CfgSt.Roster.Selected.Count - 1; i >= 0; i--)
            {
                SelectedCharacter SC = CfgSt.Roster.Selected[i];
                bool ClashesWithNew = CfgSt.GUI.ShowClashes && SC.Character_Number == New.Character_Number;
                if (string.Equals(SC.Path, New.Path, StringComparison.OrdinalIgnoreCase)
                    || SC.LocNum == New.LocNum)
                {
                    Removed = Removed || CfgSt.GUI.ShowClashes && !ClashesWithNew;
                    CfgSt.Roster.RemoveSelected(SC, i);
                }
                else if (ClashesWithNew) { CfgSt.Roster.NumClash = SC.NumClash = New.NumClash = true; }
            }
            CfgSt.Roster.AddSelected(New, CfgSt.GUI.IsMua, Index);
            if (Removed) { UpdateClashes(); }
        }

        internal static void AddToSelectedBulk(int Loc, string PathInfo, bool Unl = false, bool Start = false) => CfgSt.Roster.AddSelected(ToSelected(Loc, PathInfo, Unl, Start));

        internal static SelectedCharacter ToSelected(int Loc, string? PathInfo, bool Unl, bool Start)
        {
            string CharNum = "00";
            string CharacterName = "";
            if (!string.IsNullOrEmpty(PathInfo))
            {
                try
                {
                    // Slows down a bit, but is relevant for notifying clashes
                    Stats HS = Herostat.GetStats(PathInfo);
                    string S = HS.RootAttribute("skin");
                    if (S.Length > 2) { CharNum = S[..^2]; }
                    CharacterName = HS.CharacterName;
                }
                catch { CharacterName = PathInfo; }
            }
            return new SelectedCharacter
            {
                LocNum = Loc,
                Character_Name = CharacterName,
                Path = PathInfo,
                Character_Number = CharNum.Any(static c => !char.IsDigit(c))
                    ? "00" : $"{CharNum:00}",
                Unlock = Unl,
                Starter = Start
            };
        }
        /// <summary>
        /// Updates the number clash property of all <see cref="SelectedCharacter"/>s and the global <see cref="CharacterLists.NumClash"/>.
        /// </summary>
        internal static void UpdateClashes()
        {
            CfgSt.Roster.NumClash = false;
            if (!CfgSt.GUI.ShowClashes) { return; }
            HashSet<string?> Duplicates = [];
            HashSet<string> Uniques = [];
            for (int i = 0; i < CfgSt.Roster.Selected.Count; i++)
            {
                string? CN = CfgSt.Roster.Selected[i].Character_Number;
                _ = CN is not null && !Uniques.Add(CN) && Duplicates.Add(CN);
            }
            for (int i = 0; i < CfgSt.Roster.Selected.Count; i++)
            {
                SelectedCharacter SC = CfgSt.Roster.Selected[i];
                SC.NumClash = Duplicates.Contains(SC.Character_Number);
                CfgSt.Roster.NumClash = CfgSt.Roster.NumClash || SC.NumClash;
            }
        }
    }
}
