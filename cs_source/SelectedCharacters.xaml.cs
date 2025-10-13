using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The list with all selected characters
    /// </summary>
    public sealed partial class SelectedCharacters : Page
    {
        private string? Mod;
        public Cfg Cfg { get; set; } = new();
        public SelectedCharacters()
        {
            InitializeComponent();
            LocColumn.Visibility = StarterHeader.Visibility = EffectHeader.Visibility = Cfg.GUI.Game == "XML2" ? Visibility.Collapsed : Visibility.Visible;
        }
        /// <summary>
        /// List sorting function
        /// </summary>
        private void LV_Sorting(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem SortItem)
            {
                string? SI = SortItem.Tag.ToString();
                SelectedCharacter[]? Temp = SI == "loc.asc"
                    ? [.. Cfg.Roster.Selected.OrderBy(i => i.Loc)]
                    : SI == "loc.desc"
                    ? [.. Cfg.Roster.Selected.OrderByDescending(i => i.Loc)]
                    : SI == "name.asc"
                    ? [.. Cfg.Roster.Selected.OrderBy(i => i.Character_Name)]
                    : SI == "name.desc"
                    ? [.. Cfg.Roster.Selected.OrderByDescending(i => i.Character_Name)]
                    : SI == "path.asc"
                    ? [.. Cfg.Roster.Selected.OrderBy(i => i.Path)]
                    : SI == "path.desc"
                    ? [.. Cfg.Roster.Selected.OrderByDescending(i => i.Path)]
                    : SI == "num.asc"
                    ? [.. Cfg.Roster.Selected.OrderBy(i => int.Parse(i.Character_Number ?? "0"))]
                    : SI == "num.desc"
                    ? [.. Cfg.Roster.Selected.OrderByDescending(i => int.Parse(i.Character_Number ?? "0"))]
                    : [.. Cfg.Roster.Selected];
                Cfg.Roster.Selected.Clear();
                for (int i = 0; i < Temp.Length; i++)
                {
                    Cfg.Roster.Selected.Add(Temp[i]);
                }
            }
        }
        /// <summary>
        /// Disable unlocks on starters. Unlocks can be turned on again separately. Unlocks are restored when starters are disabled.
        /// </summary>
        private void Starter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox CB
                && CB.DataContext is SelectedCharacter SC
                && Cfg.Roster.Selected.FirstOrDefault(s => s.Path == SC.Path) is SelectedCharacter Chr)
            {
                Chr.Unlock = !Chr.Starter;
            }
        }

        private void Selected_Characters_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            for (int i = 0; i < SelectedCharactersList.SelectedItems.Count;)
            {
                if (SelectedCharactersList.SelectedItems[i] is SelectedCharacter SC)
                {
                    _ = Cfg.Roster.Selected.Remove(SC);
                }
            }
            CharacterListCommands.UpdateClashes();
            args.Handled = true;
        }

        private void DeleteSwipeMember_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (args.SwipeControl.DataContext is SelectedCharacter SC)
            {
                _ = Cfg.Roster.Selected.Remove(SC);
                CharacterListCommands.UpdateClashes();
            }
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedCharactersList.SelectedItem is SelectedCharacter SC)
            {
                Cfg.Var.FloatingCharacter = SC.Path;
            }
        }

        private void SelectedCharactersList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items[0] is SelectedCharacter SC)
            {
                Cfg.Var.FloatingCharacter = SC.Path;
                e.Data.Properties.Add("SelectedCharacter", SC);
            }
        }

        private async void ResolveClash_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCharactersList.SelectedItem is SelectedCharacter SC)
            {
                ClsResNo.Message = $"{SC.Character_Name} is not clashing.";
                ClsResNo.IsOpen = !SC.NumClash;
                if (SC.NumClash
                    && !string.IsNullOrEmpty(SC.Path)
                    && !string.IsNullOrEmpty(SC.Character_Number)
                    && Herostat.GetFile(SC.Path) is FileInfo HF)
                {
                    // The dialogue will not open if there's a problem with the herostat file HF
                    NewModNumber.Header = $"Enter a new number for {SC.Character_Number} {SC.Character_Name}:";
                    ContentDialogResult result = await ModRenumber.ShowAsync();
                    if (result == ContentDialogResult.Secondary)
                    {
                        ClsResProg.Visibility = Visibility.Visible;
                        string NN = NewModNumber.Text;
                        string NewName = $"{SC.Character_Name} - {NN}";
                        string? ClsErr = null;
                        bool isArchive = MBrowseSwitch.IsOn;
                        await Task.Run(() =>
                        {
                            if (!string.IsNullOrEmpty(Mod)
                                && (isArchive
                                ? Util.Run7z(Mod, NewName)
                                : OHSpath.CopyFilesRecursively(new DirectoryInfo(Mod), Path.Combine(Mod, "..", NewName))
                                ? Path.Combine(Mod, "..", NewName)
                                : null) is string Clone)
                            {
                                ClsErr = ModOps.Renumber(Clone, NewName, SC.Character_Number, NN, HF);
                            }
                            else
                            {
                                Herostat.Clone(File.ReadAllText(HF.FullName), HF, SC.Character_Number, NN);
                                ClsErr = $"No associated mod renumbered.\n{NewName} might not work in the game.";
                            }
                        });
                        ClsResNo.Message = ClsErr;
                        ClsResNo.IsOpen = ClsErr is not null;
                        if (ClsErr is null || !ClsErr.StartsWith("Herostat"))
                        {
                            _ = CharacterListCommands.AddToSelected(SC.Loc, Cfg.Var.FloatingCharacter, SC.Unlock, SC.Starter);
                            CharacterListCommands.UpdateClashes();
                            Cfg.Var.PopAvail = !Cfg.Var.PopAvail;
                            ClsRes.IsOpen = true;
                        }
                        Cfg.Var.FloatingCharacter = Mod = null;
                        ClsResProg.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                ClsResNo.Message = "Please select a clashing character.";
                ClsResNo.IsOpen = true;
            }
        }
        /// <summary>
        /// Restrict mod number input
        /// </summary>
        private void ModNumber_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c)) || args.NewText.Length > 3;
        }
        /// <summary>
        /// Disable OK button when number is more than 255
        /// </summary>
        private void ModNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            ModRenumber.IsSecondaryButtonEnabled = int.TryParse(NewModNumber.Text, out int N) && N < 256;
        }
        /// <summary>
        /// Get next available number if it's already in the roster and ensure two digit number
        /// </summary>
        private void ModNumber_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (sender is TextBox SN && int.TryParse(SN.Text, out int N))
            {
                SN.Text = Enumerable.Range(0, 256)
                    .Except(Cfg.Roster.Selected.Select(n => int.Parse(n.Character_Number ?? "0")))
                    .Aggregate((nc, nx) => Math.Abs(nc - N) < Math.Abs(nx - N) ? nc : nx)
                    .ToString()
                    .PadLeft(2, '0');
            }
            args.Handled = true;
        }

        private async void MBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Mod = MBrowseSwitch.IsOn
                ? await CfgCmd.LoadDialogue("*")
                : await CfgCmd.BrowseFolder();
        }

        private void MBrowseSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            MBrowseButton.Content = MBrowseSwitch.IsOn ? "Browse for archive" : "Browse for folder";
        }
    }
}
