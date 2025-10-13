using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Tab to build and modify teams and bonuses
    /// </summary>
    public sealed partial class Tab_Teams : Page
    {
        public Cfg Cfg { get; set; } = new();
        public int TeamsLimit = CfgSt.GUI.Game == "XML2" ? 17 : 32;
        public int TeamMembersLimit = CfgSt.GUI.Game == "XML2" ? 6 : 8;
        private readonly StandardUICommand DeleteCommand = new(StandardUICommandKind.Delete);

        public Tab_Teams()
        {
            DeleteCommand.ExecuteRequested += DeleteCommand_ExecuteRequested;

            InitializeComponent();
            LoadTeams();
            _ = SelectedCharacters.Navigate(typeof(SelectedCharacters));
        }

        private void LoadTeams()
        {
            MarvelModsXML.TeamBonusDeserializer(DeleteCommand);
            AddTeam.Visibility = Cfg.Roster.Teams.Count < TeamsLimit
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
                    ? [.. Cfg.Roster.Teams.OrderBy(i => i.Name)]
                    : SI == "name.desc"
                    ? [.. Cfg.Roster.Teams.OrderByDescending(static i => i.Name)]
                    : [.. Cfg.Roster.Teams];
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
        /// Remove the <paramref name="Team"/> from the team bonus list and update the visibility of the add button
        /// </summary>
        private void RemoveTeam(TeamBonus Team)
        {
            _ = Cfg.Roster.Teams.Remove(Team);
            AddTeam.Visibility = Cfg.Roster.Teams.Count < TeamsLimit
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
                    Members = [],
                    Command = DeleteCommand
                }
            );
            AddTeam.Visibility = Cfg.Roster.Teams.Count < TeamsLimit
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        /// <summary>
        /// Add floating character to selected <see cref="TeamBonus"/> as <see cref="TeamMember"/>
        /// </summary>
        private void AddTeamMember()
        {
            if (AvailableTeams.SelectedItem is TeamBonus ST
                && ST.Members is not null
                && ST.Members.Count < TeamMembersLimit
                && Herostat.GetInternalName() is string Hero
                && !ST.Members.Select(m => m.Name).Contains(Hero, StringComparer.OrdinalIgnoreCase))
            {
                ST.Members.Add(new TeamMember { Name = Hero, Skin = "" });
                TeamMembersCount.Text = ST.Members.Count.ToString();
            }
        }
        /// <summary>
        /// Delete selected team members.
        /// </summary>
        private void TeamMembers_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (AvailableTeams.SelectedItem is TeamBonus ST && args.Element is ListView Members && ST.Members is ObservableCollection<TeamMember> Ms)
            {
                TeamMember[]? Temp = [.. Ms.Except(Members.SelectedItems.Cast<TeamMember>())];
                ST.Members.Clear();
                for (int i = 0; i < Temp.Length; i++)
                {
                    ST.Members.Add(Temp[i]);
                }
                TeamMembersCount.Text = ST.Members.Count.ToString();
            }
            args.Handled = true;
        }

        private void DeleteSwipeMember_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (AvailableTeams.SelectedItem is TeamBonus ST && args.SwipeControl.DataContext is TeamMember TM && ST.Members is not null)
            {
                _ = ST.Members.Remove(TM);
            }
        }

        private void TeamMembers_DragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties["SelectedCharacter"] is not null
                && AvailableTeams.SelectedItem is TeamBonus ST
                && string.IsNullOrEmpty(ST.Skinset))
            {
                TeamMembersDropArea.Visibility = Visibility.Visible;
            }
        }

        private void TeamMembers_DragLeave(object sender, DragEventArgs e)
        {
            TeamMembersDropArea.Visibility = Visibility.Collapsed;
        }

        private void TeamMembers_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = $"{Cfg.Var.FloatingCharacter}";
        }

        private void TeamMembers_Drop(object sender, DragEventArgs e)
        {
            TeamMembersDropArea.Visibility = Visibility.Collapsed;
            AddTeamMember();
        }

        private void SelectedCharacters_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => AddTeamMember();
    }
}
