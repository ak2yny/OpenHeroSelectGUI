using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Settings;
using System.Collections.ObjectModel;
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
            Starter.Visibility = (Cfg.GUI.Game == "xml2") ?
                Visibility.Collapsed :
                Visibility.Visible;
            Location.Visibility = (Cfg.GUI.Game == "xml2") ?
                Visibility.Collapsed :
                Visibility.Visible;
        }
        /// <summary>
        /// List sorting function
        /// </summary>
        private void DG_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (e.Column.Tag.ToString() == "Loc")
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    Selected_Characters.ItemsSource = new ObservableCollection<SelectedCharacter>(Cfg.Roster.Selected.OrderBy(i => i.Loc));
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    Selected_Characters.ItemsSource = new ObservableCollection<SelectedCharacter>(Cfg.Roster.Selected.OrderByDescending(i => i.Loc));
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
            }
            else if (e.Column.Tag.ToString() == "Name")
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    Selected_Characters.ItemsSource = new ObservableCollection<SelectedCharacter>(Cfg.Roster.Selected.OrderBy(i => i.Character_Name));
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    Selected_Characters.ItemsSource = new ObservableCollection<SelectedCharacter>(Cfg.Roster.Selected.OrderByDescending(i => i.Character_Name));
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
            }
            else if (e.Column.Tag.ToString() == "Unlock")
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    Selected_Characters.ItemsSource = new ObservableCollection<SelectedCharacter>(Cfg.Roster.Selected.OrderBy(i => i.Unlock));
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    Selected_Characters.ItemsSource = new ObservableCollection<SelectedCharacter>(Cfg.Roster.Selected.OrderByDescending(i => i.Unlock));
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
            }
            else if (e.Column.Tag.ToString() == "Starter")
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    Selected_Characters.ItemsSource = new ObservableCollection<SelectedCharacter>(Cfg.Roster.Selected.OrderBy(i => i.Starter));
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    Selected_Characters.ItemsSource = new ObservableCollection<SelectedCharacter>(Cfg.Roster.Selected.OrderByDescending(i => i.Starter));
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
            }
            // Remove sorting indicators from other columns
            foreach (var dgColumn in Selected_Characters.Columns)
            {
                if (dgColumn.Tag.ToString() != e.Column.Tag.ToString())
                {
                    dgColumn.SortDirection = null;
                }
            }
        }
        /// <summary>
        /// Key commands. Currently only using Delete, so no key filtering.
        /// </summary>
        private void Selected_Character_Delete(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
        {
            if (Selected_Characters.SelectedItem is SelectedCharacter SC)
            {
                Cfg.Roster.Selected.Remove(SC);
                Cfg.Roster.Count = Cfg.Roster.Selected.Count;
            }
        }
    }
}
