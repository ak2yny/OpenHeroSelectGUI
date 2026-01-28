using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using Windows.ApplicationModel.DataTransfer;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Character selection page for XML2
    /// </summary>
    public sealed partial class Tab_XML2 : Page
    {
        public Cfg Cfg { get; set; } = new();
        internal Messages Msg { get; set; } = new();

        public Tab_XML2()
        {
            InitializeComponent();

            // Initialize XML2 settings;
            RosterSizeToggle.SelectedIndex = (Cfg.XML2.RosterSize - 19) / 2;
            SkinDetailsBtn.Content = Cfg.GUI.SkinDetailsVisible
                ? "Hide Skin Details"
                : "Show Skin Details";
        }
        // Control handlers. A few of them are identical to the MUA handlers, can they be combined?
        private void BtnRunGame_Click(object sender, RoutedEventArgs e) => Util.RunGame();

        private void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Cfg.Roster.Selected.Count; i++) { Cfg.Roster.Selected[i].Unlock = true; }
        }
        /// <summary>
        /// Save the selected limit to settings and define the default roster file.
        /// </summary>
        private void RosterSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is RadioButtons RBs && RBs.SelectedItem is string RS
                && int.TryParse(RS[..2], out int Limit))
            {
                CfgSt.CSS.SetDefaultRosterXML2(Limit);
            }
        }

        /// <summary>
        /// Show the drop area when the pointer is on it
        /// </summary>
        private void SelectedCharacters_DragEnter(object sender, DragEventArgs e)
        {
            if (Cfg.GUI.SelectedDnDInsert) { return; }
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
            e.DragUIOverride.Caption = e.DataView.Properties["Roster"] is string r
                ? $"Replace {Cfg.XML2.RosterValue} with {r}" : $"{Cfg.Var.FloatingCharacter}";
        }
        /// <summary>
        /// Define the drop event for dropped characters
        /// </summary>
        private void SelectedCharacters_Drop(object sender, DragEventArgs e)
        {
            SelectedCharactersDropArea.Visibility = Visibility.Collapsed;
            if (Cfg.Var.FloatingCharacter is string FC) { AddToSelected(FC); }
            else if (e.DataView.Properties["Roster"] is string r) { LoadRosterVal(r); }
        }
        /// <summary>
        /// Load the default roster.
        /// </summary>
        private void XML2_LoadDefault(object sender, RoutedEventArgs e)
        {
            LoadRosterVal(CfgSt.CSS.Default.Roster);
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
            Cfg.Roster.ClearSelected();
        }
        /// <summary>
        /// Hide/show skin details + editor
        /// </summary>
        private void SkinDetailsBtn_Click(object sender, RoutedEventArgs e)
        {
            Cfg.GUI.SkinDetailsVisible = !Cfg.GUI.SkinDetailsVisible;
            SkinDetailsBtn.Content = Cfg.GUI.SkinDetailsVisible
                ? "Hide Skin Details"
                : "Show Skin Details";
        }
    }
}
