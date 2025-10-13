using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.ObjectModel;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AvailableTeams : Page
    {
        public Cfg Cfg { get; set; } = new();
        public ObservableCollection<TeamBonus> TeamBonusList { get; set; } = [];

        public AvailableTeams()
        {
            InitializeComponent();
            if (Cfg.Roster.Teams.Count == 0)
            {
                MarvelModsXML.TeamBonusDeserializer(new StandardUICommand(StandardUICommandKind.Delete));
            }
            Update_TeamBonusList();
        }
        /// <summary>
        /// Update the available team bonus list according to <paramref name="Filter"/> (lists all if empty or omitted).
        /// </summary>
        private void Update_TeamBonusList(string Filter = "")
        {
            TeamBonusList.Clear();
            for (int i = 0; i < Cfg.Roster.Teams.Count; i++)
            {
                TeamBonus TB = Cfg.Roster.Teams[i];
                if (TB.Name is not null && (string.IsNullOrEmpty(Filter) || TB.Name.Contains(Filter, StringComparison.CurrentCultureIgnoreCase)))
                {
                    TeamBonusList.Add(TB);
                }
            }
        }

        private void AvailableTeams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Cfg.Var.FloatingCharacter = null;
            if (((ListView)sender).SelectedItem is TeamBonus TB)
            {
                Cfg.Var.FloatingCharacter = TB.Sound;
            }
        }

        private void TeamSearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            Update_TeamBonusList(sender.Text);
        }

        private void AvailableTeams_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items[0] is TeamBonus TB)
            {
                e.Cancel = string.IsNullOrEmpty(TB.Sound);
                Cfg.Var.FloatingCharacter = TB.Sound;
                e.Data.Properties.Add("TeamBonus", TB);
            }
            else
            {
                e.Cancel = true;
            }
        }
    }
}
