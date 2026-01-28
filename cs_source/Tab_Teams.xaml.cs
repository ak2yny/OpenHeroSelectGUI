using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
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
        public int TeamsLimit = CfgSt.GUI.IsMua ? 32 : 17;
        public int TeamMembersLimit = CfgSt.GUI.IsMua ? 8 : 6;
        private readonly StandardUICommand DeleteCommand = new(StandardUICommandKind.Delete);

        public Tab_Teams()
        {
            DeleteCommand.ExecuteRequested += DeleteCommand_ExecuteRequested;

            InitializeComponent();
            LoadTeams();
        }

        private void LoadTeams()
        {
            BonusSerializer.Deserialize(DeleteCommand);
            UpdateAddButton();
        }
        /// <summary>
        /// Update the visibility of the add button, after removing or adding a team <see cref="Bonus"/>.
        /// </summary>
        private void UpdateAddButton()
        {
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
                Cfg.Roster.Teams.Sort(
                    SI == "name.asc"
                    ? c => c.OrderBy(static i => i.Name)
                    : SI == "name.desc"
                    ? c => c.OrderByDescending(static i => i.Name)
                    : c => c.OrderBy(static i => i)); // never happens
            }
        }

        private void AvailableTeams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AvailableTeams.SelectedItem is Bonus ST && ST.Members is not null)
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

        private void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is Bonus B && Cfg.Roster.Teams.Remove(B)) { UpdateAddButton(); }
        }

        private void AvailableTeams_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (AvailableTeams.SelectedIndex > -1) // -1 should not be possible
            {
                Cfg.Roster.Teams.RemoveAt(AvailableTeams.SelectedIndex);
                UpdateAddButton();
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
            Cfg.Roster.Teams.Add(new Bonus { Command = DeleteCommand, Members = [] });
            UpdateAddButton();
        }
        /// <summary>
        /// Add floating character to the selected <see cref="Bonus"/> as <see cref="Hero"/>
        /// </summary>
        private void AddTeamMember()
        {
            if (AvailableTeams.SelectedItem is Bonus ST
                && ST.Members is not null
                && ST.Members.Count < TeamMembersLimit
                && Herostat.GetInternalName() is string Hero
                && (Cfg.GUI.IsMua || !ST.Members.Any(m => m.Name.Equals(Hero, StringComparison.OrdinalIgnoreCase))))
            {
                ST.Members.Add(new Hero { Name = Hero });
                TeamMembersCount.Text = ST.Members.Count.ToString();
            }
        }
        /// <summary>
        /// Delete selected team members.
        /// </summary>
        private void TeamMembers_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (AvailableTeams.SelectedItem is Bonus ST && args.Element is ListView Members && ST.Members is not null)
            {
                foreach (ItemIndexRange IR in Members.SelectedRanges.Reverse())
                {
                    for (int i = IR.LastIndex; i >= IR.FirstIndex; i--) { ST.Members.RemoveAt(i); }
                }
                TeamMembersCount.Text = ST.Members.Count.ToString();
            }
            args.Handled = true;
        }

        private void DeleteSwipeMember_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (AvailableTeams.SelectedItem is Bonus ST && ST.Members is not null && args.SwipeControl.DataContext is Hero TM)
            {
                _ = ST.Members.Remove(TM);
            }
        }

        private void TeamMembers_DragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties["SelectedCharacter"] is not null
                && AvailableTeams.SelectedItem is Bonus ST
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
