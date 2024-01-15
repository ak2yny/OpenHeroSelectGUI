using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Settings;
using System.Linq;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The list with all selected characters
    /// </summary>
    public sealed partial class SelectedCharacters : Page
    {
        public Cfg Cfg { get; set; } = new();
        public SelectedCharacters()
        {
            InitializeComponent();
            LocColumn.Visibility = StarterHeader.Visibility = EffectHeader.Visibility = Cfg.GUI.Game == "xml2" ? Visibility.Collapsed : Visibility.Visible;
            Clashes.Message = "Clashing characters will have\nidentical mannequins.";
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
            }
        }
    }
}
