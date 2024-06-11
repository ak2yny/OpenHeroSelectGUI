using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Functions;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
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
        /// <summary>
        /// Define the page controls (from saved settings)
        /// </summary>
        private void LoadXML2Limit()
        {
            int Size = Cfg.Roster.Total = Cfg.XML2.RosterSize;
            SetXML2DefaultRoster(Size);
            Cfg.Var.RosterRange = Enumerable.Range(1, Size);
            RosterSizeToggle.SelectedIndex = (Size - 19) / 2;

            // Initialize other XML2 settings;
            SkinDetailsBtn.Content = Cfg.GUI.SkinDetailsVisible
                ? "Hide Skin Details"
                : "Show Skin Details";
        }
        /// <summary>
        /// Save the selected limit to settings and define the default roster file.
        /// </summary>
        private void SetXML2Limit()
        {
            if (RosterSizeToggle.SelectedItem is object RT
                && RT.ToString() is string RS
                && RS.Length > 1
                && int.TryParse(RS[..2], out int Limit))
            {
                Cfg.Roster.Total = Cfg.XML2.RosterSize = Limit;
                Cfg.Var.RosterRange = Enumerable.Range(1, Limit);
                SetXML2DefaultRoster(Limit);
            }
        }
        /// <summary>
        /// Define the default roster file, according to the selected <paramref name="Limit"/>.
        /// </summary>
        private void SetXML2DefaultRoster(int Limit)
        {
            Cfg.Var.RosterValueDefault = Limit >= 23
                ? "Default 23 Character (PSP) Roster"
                : Limit >= 21
                ? "Default 21 Character (PC) Roster"
                : "Default 19 Character (GC, PS2, Xbox) Roster";
        }
        // Control handlers. A few of them are identical to the MUA handlers, can they be combined?
        private void BtnRunGame_Click(object sender, RoutedEventArgs e) => Util.RunGame();

        private void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (Settings.SelectedCharacter c in Cfg.Roster.Selected)
            {
                c.Unlock = true;
            }
        }

        private void RosterSize_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetXML2Limit();
        /// <summary>
        /// Show the drop area when the pointer is on it
        /// </summary>
        private void SelectedCharacters_DragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties["Character"] is not null)
            {
                SelectedCharactersDropArea.Visibility = Visibility.Visible;
            }
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
            e.DragUIOverride.Caption = $"{Cfg.Var.FloatingCharacter}";
        }
        /// <summary>
        /// Define the drop event for dropped characters
        /// </summary>
        private void SelectedCharacters_Drop(object sender, DragEventArgs e)
        {
            SelectedCharactersDropArea.Visibility = Visibility.Collapsed;
            if (Cfg.Var.FloatingCharacter is string FC)
            {
                AddToSelected(FC);
                UpdateClashes();
            }
        }
        /// <summary>
        /// Load the default roster.
        /// </summary>
        private void XML2_LoadDefault(object sender, RoutedEventArgs e)
        {
            LoadRosterVal(Cfg.Var.RosterValueDefault);
        }
        /// <summary>
        /// Browse for a roster file to load.
        /// </summary>
#pragma warning disable CA1822 // Mark members as static
        private void XML2_LoadRoster(SplitButton sender, SplitButtonClickEventArgs args)
#pragma warning restore CA1822 // Mark members as static
        {
            LoadRosterBrowse();
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
            Cfg.Roster.NumClash = false;
        }

        private void SkinDetailsBtn_Click(object sender, RoutedEventArgs e)
        {
            Cfg.GUI.SkinDetailsVisible = !Cfg.GUI.SkinDetailsVisible;
            SkinDetailsBtn.Content = Cfg.GUI.SkinDetailsVisible
                ? "Hide Skin Details"
                : "Show Skin Details";
        }
    }
}
