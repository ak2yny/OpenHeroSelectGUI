using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace OpenHeroSelectGUI.Controls
{
    public sealed partial class SelectedCharacters : UserControl
    {
        public Cfg Cfg { get; set; } = new();
        private string? Mod;

        public SelectedCharacters()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Resolves a character number clash by renumbering a mod according to user inputs.
        /// </summary>
        /// <returns>An error message if the operation encounters an error; otherwise <see cref="string.Empty"/> if aborted; otherwise the new number.</returns>
        private async Task<ModOps.Result> ResolveClash(SelectedCharacter SC)
        {
            if (!SC.NumClash) { return ModOps.Result.Failure($"{SC.Character_Name} is not clashing."); }
            if (string.IsNullOrEmpty(SC.Path) || string.IsNullOrEmpty(SC.Character_Number))
            { return ModOps.Result.Failure("No herostat found or the old number can't be identified."); }
            string? HF = null;
            try { HF = OHSpath.GetHsFile(SC.Path); } // The dialogue will not open on exceptions
            catch (Exception ex) { return ModOps.Result.Failure($"Herostat file could not be opened.\n{ex.Message}"); }
            NewModNumber.Header = $"Enter a new number for {SC.Character_Number} {SC.Character_Name}:";
            ContentDialogResult result = await ModRenumber.ShowAsync();
            if (result != ContentDialogResult.Secondary
                || NewModNumber.Text == SC.Character_Number) { return ModOps.Result.Cancel(); }
            ClsResProg.Visibility = Visibility.Visible;
            string NN = NewModNumber.Text;
            string NewName = $"{SC.Character_Name} - {NN}";
            bool isArchive = MBrowseSwitch.IsOn;
            return await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(Mod)
                    && (isArchive ? Util.Run7z(Mod, NewName)
                    : Path.Combine(Mod, "..", NewName) is string C && OHSpath.CopyFilesRecursively(Mod, C)
                    ? C : null) is string Clone)
                {
                    return ModOps.Renumber(Clone, NewName, SC.Character_Number, NN, HF);
                }
                else
                {
                    new Stats(HF).Clone(SC.Character_Number, NN);
                    return ModOps.Result.Success(NN, $"No associated mod renumbered.\n{NewName} might not work in the game.");
                }
            });
        }
        /// <summary>
        /// List sorting function
        /// </summary>
        private void LV_Sorting(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem SortItem)
            {
                string? SI = SortItem.Tag.ToString();
                Cfg.Roster.Selected.Sort(
                    SI == "loc.asc"
                    ? c => c.OrderBy(static i => i.LocNum)
                    : SI == "loc.desc"
                    ? c => c.OrderByDescending(static i => i.LocNum)
                    : SI == "name.asc"
                    ? c => c.OrderBy(static i => i.Character_Name)
                    : SI == "name.desc"
                    ? c => c.OrderByDescending(static i => i.Character_Name)
                    : SI == "path.asc"
                    ? c => c.OrderBy(static i => i.Path)
                    : SI == "path.desc"
                    ? c => c.OrderByDescending(static i => i.Path)
                    : SI == "num.asc"
                    ? c => c.OrderBy(static i => int.Parse(i.Character_Number ?? "0"))
                    : SI == "num.desc"
                    ? c => c.OrderByDescending(static i => int.Parse(i.Character_Number ?? "0"))
                    : c => c.OrderBy(static i => i)); // never happens
            }
        }
        /// <summary>
        /// Disable unlocks on starters. Unlocks can be turned on again separately. Unlocks are restored when starters are disabled.
        /// </summary>
        private void Starter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox CB && CB.DataContext is SelectedCharacter SC)
            {
                SC.Unlock = !SC.Starter;
            }
        }
        /// <summary>
        /// Delete all selected (on Delete key)
        /// </summary>
        private void Selected_Characters_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            foreach (ItemIndexRange IR in SelectedCharactersList.SelectedRanges.Reverse())
            {
                for (int i = IR.LastIndex; i >= IR.FirstIndex; i--) { Cfg.Roster.RemoveSelected(i); }
            }
            if (CfgSt.GUI.IsMua) { CfgSt.CSS.UpdateLocBoxes(); }
            CharacterListCommands.UpdateClashes();
            args.Handled = true;
        }
        /// <summary>
        /// Delete item that was swiped
        /// </summary>
        private void DeleteSwipeMember_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (args.SwipeControl.DataContext is SelectedCharacter SC)
            {
                Cfg.Roster.RemoveSelected(SC);
                CharacterListCommands.UpdateClashes();
            }
        }
        /// <summary>
        /// Apply selected character as floating character (for D&D)
        /// </summary>
        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedCharactersList.SelectedItem is SelectedCharacter SC)
            {
                Cfg.Var.FloatingCharacter = SC.Path;
            }
        }
        /// <summary>
        /// Apply selected character as floating character (for D&D) and prepare the D&D data as "SelectedCharacter" (only first selection).
        /// </summary>
        private void SelectedCharactersList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items[0] is SelectedCharacter SC)
            {
                Cfg.Var.FloatingCharacter = SC.Path;
                e.Data.Properties.Add("SelectedCharacter", SC);
            }
        }
        /// <summary>
        /// If <see cref="GUIsettings.SelectedDnDInsert"/> is on, show insert caption (depending on source) and enable drop.
        /// </summary>
        private void SelectedCharactersList_DragOver(object sender, DragEventArgs e)
        {
            if (!Cfg.GUI.SelectedDnDInsert) { return; }
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = e.DataView.Properties["Roster"] is string r
                ? $"Replace {Cfg.OHS.RosterValue} with {r}" : $"{Cfg.Var.FloatingCharacter}";
        }
        /// <summary>
        /// Add floating character to selected characters at the drop position, or load the roster (depending on source).
        /// </summary>
        private void SelectedCharactersList_Drop(object sender, DragEventArgs e)
        {
            if (Cfg.Var.FloatingCharacter is string FC)
            {
                var position = e.GetPosition(SelectedCharactersList);
                int i = 0; for (; i < SelectedCharactersList.Items.Count; i++)
                {
                    if (SelectedCharactersList.ContainerFromIndex(i) is ListViewItem LVI
                        && position.Y < (double)(LVI.TransformToVisual(SelectedCharactersList)
                        .TransformPoint(new(0, 0)).Y + (LVI.ActualHeight / 2)))
                    {
                        break;
                    }
                }
                CharacterListCommands.AddToSelected(FC, i);
            }
            else if (e.DataView.Properties["Roster"] is string r) { CharacterListCommands.LoadRosterVal(r, CfgCmd.MatchingMV(r)); }
        }
        /// <summary>
        /// Resolve clashes when clicked on button (only available on clashes, if show enabled),
        /// replacing the selected charater's properties to the resolved ones or showing errors depending on the resolving results.
        /// </summary>
        private async void ResolveClash_Click(object sender, RoutedEventArgs e)
        {
            ModOps.Result Result;
            if (SelectedCharactersList.SelectedItem is SelectedCharacter SC)
            {
                Result = await ResolveClash(SC);
                if (!Result.Critical)
                {
                    SC.Path = Cfg.Var.FloatingCharacter;
                    SC.Character_Number = Result.NewNumber;
                    // user has the chance to change selection and move selected (index) while waiting -- ignoring
                    Cfg.Roster.Selected[SelectedCharactersList.SelectedIndex] = SC;
                    CharacterListCommands.UpdateClashes();
                    Cfg.Roster.AddAvailable(SC.Path!);
                    ClsRes.IsOpen = true;
                }
                Cfg.Var.FloatingCharacter = Mod = null;
                ClsResProg.Visibility = Visibility.Collapsed;
            }
            else { Result = ModOps.Result.Failure("Please select a clashing character."); }
            if (ClsResNo.IsOpen = !Result.NoError) { ClsResNo.Message = Result.Message; }
        }
        /// <summary>
        /// Attach input validation to the NumberBox's underlying TextBox.
        /// </summary>
        private void ModNumberBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (ModNumberBox_FindFirst((DependencyObject)sender) is TextBox TB)
            {
                TB.BeforeTextChanging += ModNumber_BeforeTextChanging;
            }
        }
        /// <summary>
        /// Find the first <see cref="TextBox"/> in the visual tree of the specified <paramref name="parent"/> element.
        /// </summary>
        /// <returns>The first found <see cref="TextBox"/> instance if any; otherwise, <see langword="null"/>.</returns>
        private static TextBox? ModNumberBox_FindFirst(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox TB) { return TB; }
                if (ModNumberBox_FindFirst(child) is TextBox TBR) { return TBR; }
            }
            return null;
        }
        /// <summary>
        /// Restrict mod number input
        /// </summary>
        private void ModNumber_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Length != 0 && (args.NewText.Length > 3 || !int.TryParse(args.NewText, out _));
        }
        /// <summary>
        /// Get next available number if it's already in the roster and ensure two digit number
        /// </summary>
        private void NewModNumber_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            sender.Value = Enumerable.Range(0, 0x100)
                .Except(Cfg.Roster.Selected.Select(n => n.Character_Number is null ? 0 : int.Parse(n.Character_Number)))
                .Aggregate((nc, nx) => Math.Abs(nc - sender.Value) < Math.Abs(nx - sender.Value) ? nc : nx);
        }
        /// <summary>
        /// Brose for archive or folder, dependng on the toggle, and set <see cref="Mod"/> to the selected path.
        /// </summary>
        private async void MBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Mod = MBrowseSwitch.IsOn
                ? await CfgCmd.LoadDialogue("*")
                : await CfgCmd.BrowseFolder();
        }
        /// <summary>
        /// The toggle, to switch between archive and folder browsing modes.
        /// </summary>
        private void MBrowseSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            MBrowseButton.Content = MBrowseSwitch.IsOn ? "Browse for archive" : "Browse for folder";
        }
    }
}
