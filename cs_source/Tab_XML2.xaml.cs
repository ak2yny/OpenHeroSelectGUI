using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using static OpenHeroSelectGUI.Settings.CfgCommands;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Character selection page for XML2
    /// </summary>
    public sealed partial class Tab_XML2 : Page
    {
        public Settings.Cfg Cfg { get; set; } = new();
        public Tab_XML2()
        {
            InitializeComponent();
            LoadXML2Limit();
            _ = AvailableCharacters.Navigate(typeof(AvailableCharacters));
            _ = SelectedCharacters.Navigate(typeof(SelectedCharacters));
            _ = SkinDetailsPage.Navigate(typeof(SkinDetailsPage));
        }
        private void LoadXML2Limit()
        {
            ReplDefaultmanToggle.IsOn = false;
            int Size = Cfg.XML2.RosterSize;
            SetXML2DefaultRoster(Size);
            Cfg.Dynamic.RosterRange = Enumerable.Range(1, Size);
            if (Cfg.XML2.RosterSize % 2 == 1)
            {
                Size -= 1;
                ReplDefaultmanToggle.IsOn = true;
            }
            RosterSizeToggle.SelectedIndex = (Size - 18) / 2;

            // Initialize other XML2 settings;
            if (Cfg.XML2.ExeName == "") Cfg.XML2.ExeName = "Xmen.exe";
            SkinDetailsBtn.Content = Cfg.GUI.SkinDetailsVisible
                ? "Hide Skin Details"
                : "Show Skin Details";
        }
        private void SetXML2Limit()
        {
            if (RosterSizeToggle.SelectedItem is object RT && RT.ToString() is string RS && int.TryParse(RS[..2], out int Limit))
            {
                Limit = ReplDefaultmanToggle.IsOn ? Limit + 1 : Limit;
                Cfg.Roster.Total = Cfg.XML2.RosterSize = Limit;
                Cfg.Dynamic.RosterRange = Enumerable.Range(1, Limit);
                SetXML2DefaultRoster(Limit);
            }
        }
        private void SetXML2DefaultRoster(int Limit)
        {
            Cfg.Dynamic.RosterValueDefault = Limit > 21
                ? "Default 22 Character (PSP) Roster"
                : Limit > 19
                ? "Default 20 Character (PC) Roster"
                : "Default 18 Character (GC, PS2, Xbox) Roster";
        }
        // Control handlers. A few of them are identical to the MUA handlers, can they be combined?
        private void BtnRunGame_Click(object sender, RoutedEventArgs e)
        {
            if (Cfg.GUI.FreeSaves) MoveSaves("Save", $"{DateTime.Now:yyMMdd-HHmmss}");
            _ = Process.Start(Path.Combine(Cfg.GUI.GameInstallPath, Cfg.OHS.ExeName));
        }

        private void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (Settings.SelectedCharacter c in Cfg.Roster.Selected)
            {
                c.Unlock = true;
            }
        }

        private void RosterSize_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetXML2Limit();

        private void ReplDefaultman_Toggled(object sender, RoutedEventArgs e) => SetXML2Limit();
        /// <summary>
        /// Show the drop area when the pointer is on it
        /// </summary>
        private void SelectedCharacters_DragEnter(object sender, DragEventArgs e)
        {
            SelectedCharactersDropArea.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Hide the drop area when pointer is not on it
        /// </summary>
        private void SelectedCharacters_DragLeave(object sender, DragEventArgs e)
        {
            SelectedCharactersDropArea.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// Define the allowed drop info
        /// </summary>
        private void SelectedCharacters_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = $"{Cfg.Dynamic.FloatingCharacter}";
        }
        /// <summary>
        /// Define the drop event for dropped characters
        /// </summary>
        private void SelectedCharacters_Drop(object sender, DragEventArgs e)
        {
            SelectedCharactersDropArea.Visibility = Visibility.Collapsed;
            if (Cfg.Dynamic.FloatingCharacter is string FC)
            {
                AddToSelected(FC);
            }
        }
        /// <summary>
        /// Load the default roster for the current layout.
        /// </summary>
        private void XML2_LoadDefault(object sender, RoutedEventArgs e)
        {
            LoadRosterVal(Cfg.Dynamic.RosterValueDefault);
        }
        /// <summary>
        /// Browse for a roster file to load.
        /// </summary>
        private async void XML2_LoadRoster(SplitButton sender, SplitButtonClickEventArgs args)
        {
            string? RosterValue = await LoadDialogue(".cfg");
            if (RosterValue != null)
            {
                LoadRoster(RosterValue);
            }
        }
        /// <summary>
        /// Generate a random character list from the available characters.
        /// </summary>
        private void XML2_Random(object sender, RoutedEventArgs e)
        {
            LoadRandomRoster();
        }
        /// <summary>
        /// Clear the selected roster list
        /// </summary>
        private void XML2_Clear(object sender, RoutedEventArgs e)
        {
            Cfg.Roster.Selected.Clear();
            Cfg.Roster.Count = 0;
        }

        private void SkinDetailsBtn_Click(object sender, RoutedEventArgs e)
        {
            SkinDetailsBtn.Content = Cfg.GUI.SkinDetailsVisible
                ? "Hide Skin Details"
                : "Show Skin Details";
            Cfg.GUI.SkinDetailsVisible = !Cfg.GUI.SkinDetailsVisible;
        }
    }
}
