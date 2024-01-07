using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Settings;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;
using static OpenHeroSelectGUI.Settings.MarvelModsXML;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Tab_Teams : Page
    {
        public Cfg Cfg { get; set; } = new();
        private readonly StandardUICommand DeleteCommand = new(StandardUICommandKind.Delete);

        /// <summary>
        /// Page to build and modify teams and bonuses
        /// </summary>
        public Tab_Teams()
        {
            DeleteCommand.ExecuteRequested += DeleteCommand_ExecuteRequested;

            InitializeComponent();
            LoadTeams();
            _ = SelectedCharacters.Navigate(typeof(SelectedCharacters));
        }

        private void LoadTeams()
        {
            if (Cfg.Roster.Teams.Count == 0)
            {
                TeamBonusDeserializer(DeleteCommand);
            }
            AddTeam.Visibility = Cfg.Roster.Teams.Count < Cfg.Dynamic.TeamsLimit
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        // Controls
        private void LV_Sorting(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem SortItem)
            {
                string? SI = SortItem.Tag.ToString();
                TeamBonus[]? Temp = SI == "name.asc"
                    ? Cfg.Roster.Teams.OrderBy(i => i.Name).ToArray()
                    : SI == "name.desc"
                    ? Cfg.Roster.Teams.OrderByDescending(i => i.Name).ToArray()
                    : Cfg.Roster.Teams.ToArray();
                Cfg.Roster.Teams.Clear();
                for (int i = 0; i < Temp.Length; i++)
                {
                    Cfg.Roster.Teams.Add(Temp[i]);
                }
            }
        }

        private void AvailableTeams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AvailableTeams.SelectedItem is TeamBonus ST && ST.Members is not null)
            {
                TeamMembersCount.Text = ST.Members.Count.ToString();
            }
        }

        private void TeamTemplate_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Mouse or Microsoft.UI.Input.PointerDeviceType.Pen)
            {
                VisualStateManager.GoToState(sender as Control, "HoverButtonsShown", true);
            }
        }

        private void TeamTemplate_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(sender as Control, "HoverButtonsHidden", true);
        }
        /// <summary>
        /// Remove Team
        /// </summary>
        private void RemoveTeam(TeamBonus Team)
        {
            _ = Cfg.Roster.Teams.Remove(Team);
            AddTeam.Visibility = Cfg.Roster.Teams.Count < Cfg.Dynamic.TeamsLimit
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter != null
                && Cfg.Roster.Teams.FirstOrDefault(s => s.Name == (args.Parameter as string)) is TeamBonus Team)
            {
                RemoveTeam(Team);
            }
        }

        private void AvailableTeams_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (AvailableTeams.SelectedItem is TeamBonus Team)
            {
                RemoveTeam(Team);
            }
            args.Handled = true;
        }

        private void FocusToSelect(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement Control)
            {
                AvailableTeams.SelectedItem = Control.DataContext;
            }
        }

        private void AddTeam_Click(object sender, RoutedEventArgs e)
        {
            Cfg.Roster.Teams.Add(
                new TeamBonus
                {
                    Name = "",
                    Descbonus = "+5 All Resistances",
                    Sound = "common/team_bonus_",
                    Members = new(),
                    Command = DeleteCommand
                }
            );
            AddTeam.Visibility = Cfg.Roster.Teams.Count < Cfg.Dynamic.TeamsLimit
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void AddTeamMember()
        {
            if (AvailableTeams.SelectedItem is TeamBonus ST
                && ST.Members is not null
                && ST.Members.Count < Cfg.Dynamic.TeamMembersLimit
                && GetInternalName() is string Hero
                && !ST.Members.Select(m => m.Name).Contains(Hero))
            {
                ST.Members.Add(new TeamMember { Name = Hero, Skin = "" });
                TeamMembersCount.Text = ST.Members.Count.ToString();
            }
        }
        /// <summary>
        /// Delete selected team members. WIP: Possibly add swipe to team members
        /// </summary>
        private void TeamMembers_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (AvailableTeams.SelectedItem is TeamBonus ST && args.Element is ListView Members && ST.Members is ObservableCollection<TeamMember> Ms)
            {
                TeamMember[]? Temp = Ms.Except(Members.SelectedItems.Cast<TeamMember>()).ToArray();
                ST.Members.Clear();
                for (int i = 0; i < Temp.Length; i++)
                {
                    ST.Members.Add(Temp[i]);
                }
                TeamMembersCount.Text = ST.Members.Count.ToString();
            }
            args.Handled = true;
        }

        private void TeamMembers_DragEnter(object sender, DragEventArgs e)
        {
            TeamMembersDropArea.Visibility = Visibility.Visible;
        }

        private void TeamMembers_DragLeave(object sender, DragEventArgs e)
        {
            TeamMembersDropArea.Visibility = Visibility.Collapsed;
        }

        private void TeamMembers_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = $"{Cfg.Dynamic.FloatingCharacter}";
        }

        private void TeamMembers_Drop(object sender, DragEventArgs e)
        {
            TeamMembersDropArea.Visibility = Visibility.Collapsed;
            AddTeamMember();
        }

        private void SelectedCharacters_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => AddTeamMember();
    }
}
