using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Character selection page for MUA
    /// </summary>
    public sealed partial class Tab_MUA : Page
    {
        public Cfg Cfg { get; set; } = new();

        public string? FloatingLoc { get; set; }
        /// <summary>
        /// Tries to construct the path to MUA's Game.exe. Falls back to Game Install Path .exe. Doesn't check for a valid path or existence.
        /// </summary>
        /// <returns>The full path to the .exe or an invalid path/string if settings are wrong.</returns>
        private string MUAexe
        {
            get
            {
                field ??= Cfg.GUI.ActualGameExe != "" ? Cfg.GUI.ActualGameExe : OHSpath.GetStartExe;
                return field;
            }
        }

        public Tab_MUA()
        {
            InitializeComponent();

            LoadLayout();
            //_ = AvailableCharacters.Navigate(typeof(AvailableCharacters));
            if (Cfg.GUI.LayoutWidthUpscale)
            {
                LocationsBox.StretchDirection = StretchDirection.Both;
                LocationsBox.MaxWidth = Cfg.GUI.LayoutMaxWidth;
            }
            if (Util.GameExe(MUAexe) is FileStream fs)
            {
                byte[] bytes = new byte[2];
                fs.Position = 0x3cc28f;
                _ = fs.Read(bytes, 0, bytes.Length);
                fs.Close();
                USDnum.Text = System.Text.Encoding.Default.GetString(bytes);
            }
            if (Cfg.GUI.StageInfoTransparency)
            {
                LayoutDetails.Foreground = StageDetails.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
                StageInfo.Background = new SolidColorBrush(Color.FromArgb(200, 40, 40, 40));
            }
        }
        /// <summary>
        /// Load the layout, limit, default, model info, using saved data.
        /// </summary>
        private void LoadLayout()
        {
            if (Cfg.GUI.Layout.Length == 0) { Cfg.GUI.Layout = "25 Default PC 2006"; FirstUseSelectStage.IsOpen = true; }
            if (CfgSt.CSS.CompatibleModels.Length == 0)
            {
                string CfgLayout = Path.Combine(OHSpath.StagesDir, Cfg.GUI.Layout, "config.xml");
                if (File.Exists(CfgLayout) && GUIXML.Deserialize(CfgLayout, typeof(Settings.Layout)) is Settings.Layout CL)
                {
                    CfgSt.CSS = CL;
                }
                else { return; }
            }
            StageImage.VerticalAlignment = Cfg.GUI.RowLayout ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            LocationsBox.Margin = new(0, Cfg.GUI.RowLayout ? 20 : 5, 0, 0);
            LayoutDetails.Text = CfgSt.CSS.Information.ToString(); // Information is not observable

            if (InternalSettings.Models is not null
                && InternalSettings.Models.Categories.SelectMany(c => c.Models, (c, m) => new { c, m })
                .FirstOrDefault(p => p.m.Name == Cfg.GUI.Model) is var M && M is not null
                && CfgSt.CSS.CompatibleModels.FirstOrDefault(m => m.Name == M.c.Name) is CompatibleModel CM)
            {
                if (CfgSt.CSS.Locs.Length == 0 && M.m.ToCSSModel() is CSSModel StageModel)
                { Cfg.Var.SelectedStage = StageModel; }
                CfgSt.CSS.SelectedStageRiser = CM.Riser;
            }
            CfgSt.CSS.SetLocationsMUA();
        }
        // Control handlers:
        private void BtnRunGame_Click(object sender, RoutedEventArgs e) => Util.RunGame();

        private void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Cfg.Roster.Selected.Count; i++) { Cfg.Roster.Selected[i].Unlock = true; }
        }
        /// <summary>
        /// Re-load stage info from configuration files and images.
        /// </summary>
        private void RefreshStages_Click(object sender, RoutedEventArgs e)
        {
            CfgSt.CSS.CompatibleModels = [];
            LoadLayout();
        }
        /// <summary>
        /// Open the stage selection tab.
        /// </summary>
        private void SelectStage_Click(object sender, RoutedEventArgs e)
        {
            _ = Frame.Navigate(typeof(Tab_Stages));
        }
        /// <summary>
        /// Hex edits the upside-down arrow location, if the USD button was pressed; otherwise adds the <see cref="InternalObservables.FloatingCharacter"/> to the selected characters list, at the location of the clicked location box.
        /// </summary>
        private void LocButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton Loc && Loc.DataContext is LocationButton LB)
            {
                if (Cfg.Var.FloatingCharacter == "<" && Util.HexEdit(0x3cc28f, LB.NumberString, MUAexe))
                {
                    USDnum.Text = LB.NumberString;
                    Cfg.Var.FloatingCharacter = null;
                }
                else { AddToSelectedMUA(LB); }
            }
        }
        /// <summary>
        /// Adds the <see cref="InternalObservables.FloatingCharacter"/> to the selected characters list, at the location of the location box the char. was dropped on.
        /// </summary>
        private void LocButton_Drop(object sender, DragEventArgs e)
        {
            if (sender is ToggleButton Loc && Loc.DataContext is LocationButton LB) { AddToSelectedMUA(LB); }
        }
        /// <summary>
        /// Add the floating character to the selected list on location <paramref name="Loc"/>
        /// </summary>
        private void AddToSelectedMUA(LocationButton LB)
        {
            if (!string.IsNullOrEmpty(Cfg.Var.FloatingCharacter) && Cfg.Var.FloatingCharacter != "<")
            {
                AddToSelected(LB.Number, Cfg.Var.FloatingCharacter);
            }
        }
        /// <summary>
        /// When the pointer enters a location button: Write the location number in the FloatingLoc variable.
        /// </summary>
        private void LocButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is ToggleButton Loc)
            {
                FloatingLoc = (string)Loc.Content;
            }
        }
        /// <summary>
        /// The tooltip should show the character in this location or not show if still free.
        /// </summary>
        private void LocButton_ToolTip(object sender, RoutedEventArgs e)
        {
            if (sender is ToolTip Tt && FloatingLoc is not null)
            {
                if (Cfg.Roster.Selected.FirstOrDefault(c => c.Loc == FloatingLoc) is SelectedCharacter SC)
                { Tt.Content = SC.Path; }
                else
                { Tt.IsOpen = false; }
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
                ? $"Replace {Cfg.MUA.RosterValue} with {r}" : $"{Cfg.Var.FloatingCharacter}";
        }
        /// <summary>
        /// Define the drop event for dropped characters
        /// </summary>
        private void SelectedCharacters_Drop(object sender, DragEventArgs e)
        {
            SelectedCharactersDropArea.Visibility = Visibility.Collapsed;
            if (Cfg.Var.FloatingCharacter is string FC) { AddToSelected(FC); }
            else if (e.DataView.Properties["Roster"] is string r) { LoadRosterVal(r, CfgCmd.MatchingMV(r)); }
        }
        /// <summary>
        /// Load the default roster for the current layout.
        /// </summary>
        private void MUA_LoadDefault(object sender, RoutedEventArgs e)
        {
            LoadRosterVal(CfgSt.CSS.Default.Roster, CfgSt.CSS.Default.Menulocations);
        }
        /// <summary>
        /// Browse for a roster file to load. Populates according to the layout setup, should be left to right. Currently ignores menulocation files.
        /// </summary>
#pragma warning disable CA1822 // Mark members as static
        private void MUA_LoadRoster(SplitButton sender, SplitButtonClickEventArgs args)
#pragma warning restore CA1822 // Mark members as static
        {
            LoadRosterBrowse();
        }
        /// <summary>
        /// Generate a <see cref="Random"/> character list from the available characters.
        /// </summary>
        private void MUA_Random(object sender, RoutedEventArgs e)
        {
            LoadRandomRoster();
        }
        /// <summary>
        /// Clear the selected roster list
        /// </summary>
        private void MUA_Clear(object sender, RoutedEventArgs e)
        {
            Cfg.Roster.ClearSelected();
            CfgSt.CSS.DeselectLocBoxes();
        }
        /// <summary>
        /// Upside-down button doulbe-click: Try to reset the location to "00".
        /// </summary>
        private void USD_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (Util.HexEdit(0x3cc28f, "00", MUAexe))
            {
                USDnum.Text = "00";
            }
        }
        /// <summary>
        /// Upside-down button click: Use the floating character so that a click on a menulocation box sets the new location.
        /// </summary>
        private void USD_Click(object sender, RoutedEventArgs e)
        {
            Cfg.Var.FloatingCharacter = "<";
        }
    }
}
