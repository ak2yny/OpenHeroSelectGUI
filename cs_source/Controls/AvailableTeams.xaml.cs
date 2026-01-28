using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Settings;

namespace OpenHeroSelectGUI.Controls
{
    public sealed partial class AvailableTeams : UserControl
    {
        internal System.Collections.ObjectModel.ObservableCollection<Bonus> BonusList { get; set; }

        public AvailableTeams()
        {
            InitializeComponent();
            BonusSerializer.Deserialize();
            BonusList = new(CfgSt.Roster.Teams);
        }
        /// <summary>
        /// Update the available team bonus list according to <paramref name="Filter"/> (lists all if empty or omitted).
        /// </summary>
        public void LoadAvailable(string Filter)
        {
            bool NoFilter = Filter == "";
            BonusList.Clear();
            for (int i = 0; i < CfgSt.Roster.Teams.Count; i++)
            {
                Bonus TB = CfgSt.Roster.Teams[i];
                if (TB.Name is not null && (NoFilter || TB.Name.Contains(Filter, System.StringComparison.CurrentCultureIgnoreCase)))
                {
                    BonusList.Add(TB);
                }
            }
        }

        private void AvailableTeams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CfgSt.Var.FloatingCharacter = ((ListView)sender).SelectedItem is Bonus TB ? TB.Sound : null;
        }

        private void AvailableTeams_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items[0] is Bonus TB && !string.IsNullOrEmpty(TB.Sound))
            {
                CfgSt.Var.FloatingCharacter = TB.Sound;
                e.Data.Properties.Add("TeamBonus", TB);
            }
            else
            {
                e.Cancel = true;
            }
        }
    }
}
