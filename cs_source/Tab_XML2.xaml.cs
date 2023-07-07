using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;
using static OpenHeroSelectGUI.Settings.CharacterLists;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Tab_XML2 : Page
    {
        public Cfg Cfg { get; set; } = new();
        public Tab_XML2()
        {
            InitializeComponent();
            LoadXML2Limit();
            AvailableCharacters.Navigate(typeof(AvailableCharacters));
            SelectedCharacters.Navigate(typeof(SelectedCharacters));
        }
        private void LoadXML2Limit()
        {
            ReplDefaultmanToggle.IsOn = false;
            int Size = Cfg.XML2.RosterSize;
            Cfg.Dynamic.LayoutLocs = Enumerable.Range(1, Size);
            if (Cfg.XML2.RosterSize%2 == 1)
            {
                Size -= 1;
                ReplDefaultmanToggle.IsOn = true;
            }
            RosterSizeToggle.SelectedIndex = (Size - 18) / 2;

            if (Cfg.XML2.ExeName == "") Cfg.XML2.ExeName = "Xmen.exe";
        }
        private void SetXML2Limit()
        {
            if (RosterSizeToggle.SelectedItem is object RT && RT.ToString() is string RS && int.TryParse(RS[..2], out int Limit))
            {
                Limit = ReplDefaultmanToggle.IsOn ? Limit + 1 : Limit;
                Cfg.Roster.Total = Cfg.XML2.RosterSize = Limit;
                Cfg.Dynamic.LayoutLocs = Enumerable.Range(1, Limit);
            }
        }
        // Control handlers. A few of them are identical to the MUA handlers. Can they be combined?
        /// <summary>
        /// Define the allowed drop info
        /// </summary>
        private void AvailableCharacters_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "Add herostat";
            }
        }
        /// <summary>
        /// Define the drop event for dropped herostats.
        /// </summary>
        private async void AvailableCharacters_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var Items = await e.DataView.GetStorageItemsAsync();
                if (Items.Count > 0 && Items[0] is StorageFile Herostat)
                {
                    AddHerostat(Herostat);
                    AvailableCharacters.Navigate(typeof(AvailableCharacters));
                }
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            AvailableCharacters.Navigate(typeof(AvailableCharacters));
        }

        private void BtnRunGame_Click(object sender, RoutedEventArgs e)
        {
            if (Cfg.GUI.FreeSaves) Tab_Settings.MoveSaves("Save", $"{DateTime.Now:yyMMdd-HHmmss}");
            Process.Start(Path.Combine(Cfg.OHS.GameInstallPath, Cfg.OHS.ExeName));
        }

        private void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (SelectedCharacter c in Cfg.Roster.Selected)
            {
                c.Unlock = true;
            }
        }

        private void RosterSize_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetXML2Limit();

        private void ReplDefaultman_Toggled(object sender, RoutedEventArgs e) => SetXML2Limit();
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
        private async void XML2_LoadRoster(object sender, RoutedEventArgs e)
        {
            string? RosterValue = await Cfg.LoadDialogue("cfg");
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
    }
}
