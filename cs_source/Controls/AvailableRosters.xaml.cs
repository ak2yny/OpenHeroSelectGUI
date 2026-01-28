using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace OpenHeroSelectGUI.Controls
{
    public sealed partial class AvailableRosters : UserControl
    {
        internal ObservableCollection<string> Rosters { get; set; }

        private string[]? Available;

        public AvailableRosters()
        {
            LoadAvailable();
            Rosters = new(Available!);
            CfgSt.Var.FloatingCharacter = null;
            InitializeComponent();
        }
        /// <summary>
        /// Load the [OHS]/<see cref="OHSpath.Game"/>/rosters/*.cfg files from disk (refreshes)
        /// </summary>
        private void LoadAvailable()
        {
            Available = [.. Directory.EnumerateFiles(Path.Combine(OHSpath.CD, OHSpath.Game, "rosters"), "*.cfg")
                .Select(static c => Path.GetFileNameWithoutExtension(c))];
        }
        /// <summary>
        /// Reload the <see cref="Available"/> from disk and keep the <paramref name="Filter"/> (if any).
        /// </summary>
        public void LoadAvailable(string Filter)
        {
            LoadAvailable();
            SearchAvailable(Filter);
        }
        /// <summary>
        /// <paramref name="Filter"/> the <see cref="Available"/>, and add the matches to the <see cref="Rosters"/>.
        /// </summary>
        public void SearchAvailable(string Filter)
        {
            bool NoFilter = Filter == "";
            Rosters.Clear();
            for (int i = 0; i < Available!.Length; i++)
            {
                if (NoFilter || Available[i].Contains(Filter, System.StringComparison.CurrentCultureIgnoreCase)) { Rosters.Add(Available[i]); }
            }
        }
        /// <summary>
        /// Delete the selected list view item, except if it's a default roster
        /// </summary>
        private void DeleteSelected()
        {
            int i = AvailableRostersList.SelectedIndex;
            string Roster = Rosters[i];
            if (CfgCmd.DefaultRV.Contains(Roster)) { return; } // Not reporting
            try
            {
                File.Delete(Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", $"{Roster}.cfg"));
                if (CfgSt.GUI.IsMua) { File.Delete(Path.Combine(OHSpath.CD, OHSpath.Game, "menulocations", $"{Roster}.cfg")); }
                Rosters.RemoveAt(i);
                DeleteFailed.IsOpen = false;
            }
            catch { DeleteFailed.IsOpen = true; }
        }
        /// <summary>
        /// Add the selected roster to the selected list on double-tap
        /// </summary>
        private void AvailableRosters_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (AvailableRostersList.SelectedItem is string Roster)
            {
                CfgSt.MUA.MenulocationsValue = CfgSt.OHS.RosterValue = CfgCmd.FilterDefaultRV(Roster);
                CharacterListCommands.LoadRosterVal(Roster, CfgCmd.MatchingMV(Roster));
            }
        }
        /// <summary>
        /// Right click to select
        /// </summary>
        private void AvailableRosters_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (GUIHelpers.FindFirstParent<ListViewItem>((DependencyObject)e.OriginalSource) is ListViewItem RTLVI)
            {
                RTLVI.IsSelected = true;
            }
        }
        /// <summary>
        /// Add drag property of name "Roster" to <paramref name="args"/> (also adds "Character" for D&D caption)
        /// </summary>
        private void AvailableRosters_DragItemsStarting(object sender, DragItemsStartingEventArgs args)
        {
            if (args.Items[0] is string r) { args.Data.Properties.Add("Roster", r); args.Data.Properties.Add("Character", false); }
        }
        // Context menu commands:
        private async void AvailableRoster_NewClick(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await EnterNewName.ShowAsync();
            string NewName = EnterNewName.InputText;
            if (result != ContentDialogResult.Secondary || NewName == "") { return; }
            NewName = OHSpath.GetVacant(Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", NewName), ".cfg", 1);
            try { File.Create(NewName).Close(); } catch { } // Will try again when running OHS
            Rosters.Insert(AvailableRostersList.SelectedIndex, Path.GetFileNameWithoutExtension(NewName));
        }

        private void AvailableRoster_CopyClick(object sender, RoutedEventArgs e)
        {
            int i = AvailableRostersList.SelectedIndex;
            string file = Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", $"{Rosters[i]}.cfg");
            string Ext = Path.GetExtension(file);
            string newfile = OHSpath.GetVacant(file[..^Ext.Length], Ext, 1);
            try
            {
                File.Copy(file, newfile);
                Rosters.Insert(i + 1, Path.GetFileName(newfile)[..^Ext.Length]);
            }
            catch { } // no duplicate added, should be obvious to the user that it failed
        }

        private void AvailableRoster_DeleteClick(object sender, RoutedEventArgs e) => DeleteSelected();

        private async void AvailableRoster_RenameClick(object sender, RoutedEventArgs e)
        {
            int i = AvailableRostersList.SelectedIndex;
            ContentDialogResult result = await EnterNewName.ShowAsync(Rosters[i]);
            string NewName = EnterNewName.InputText;
            if (result != ContentDialogResult.Secondary || NewName == "") { return; }
            try
            {
                string rosters = Path.Combine(OHSpath.CD, OHSpath.Game, "rosters");
                string NewPath = OHSpath.GetVacant(Path.Combine(rosters, NewName), ".cfg");
                File.Move(Path.Combine(rosters, $"{Rosters[i]}.cfg"), NewPath);
                Rosters[i] = NewPath[(rosters.Length + 1)..^4];
                DeleteFailed.IsOpen = false;
            }
            catch { RenameFailed.IsOpen = true; }
        }

        private void AvailableRoster_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => DeleteSelected();
    }
}
