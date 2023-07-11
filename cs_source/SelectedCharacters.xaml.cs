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
            LocColumn.Visibility = Cfg.Dynamic.Game == "xml2" ? Visibility.Collapsed : Visibility.Visible;
            UnlockHeader.Text = Cfg.Dynamic.Game == "xml2" ? "Unlock" : "Unlock | Starter";
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
                    ? Cfg.Roster.Selected.OrderBy(i => i.Loc).ToArray()
                    : SI == "loc.desc"
                    ? Cfg.Roster.Selected.OrderByDescending(i => i.Loc).ToArray()
                    : SI == "name.asc"
                    ? Cfg.Roster.Selected.OrderBy(i => i.Character_Name).ToArray()
                    : SI == "name.desc"
                    ? Cfg.Roster.Selected.OrderByDescending(i => i.Character_Name).ToArray()
                    : SI == "path.asc"
                    ? Cfg.Roster.Selected.OrderBy(i => i.Path).ToArray()
                    : SI == "path.desc"
                    ? Cfg.Roster.Selected.OrderByDescending(i => i.Path).ToArray()
                    : Cfg.Roster.Selected.ToArray();
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
        /// <summary>
        /// Key commands. Currently only using Delete, so no key filtering.
        /// </summary>
        private void Selected_Character_Delete(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
        {
            if (SelectedCharactersList.SelectedItem is SelectedCharacter SC)
            {
                Cfg.Roster.Selected.Remove(SC);
                Cfg.Roster.Count = Cfg.Roster.Selected.Count;
            }
        }
    }
}
