using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OpenHeroSelectGUI.Settings;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using Windows.ApplicationModel.DataTransfer;
using static OpenHeroSelectGUI.Settings.CfgCommands;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;
using static OpenHeroSelectGUI.Settings.GUIXML;
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Character selection page for MUA
    /// </summary>
    public sealed partial class Tab_MUA : Page
    {
        public Cfg Cfg { get; set; } = new();
        public string? FloatingLoc { get; set; }
        public int? SelectedCount { get => Cfg.Roster.Selected.Count; }
        public Tab_MUA()
        {
            InitializeComponent();
            if (Cfg.MUA.ExeName == "") Cfg.MUA.ExeName = "Game.exe";
            LoadLayout();
            _ = AvailableCharacters.Navigate(typeof(AvailableCharacters));
            _ = SelectedCharacters.Navigate(typeof(SelectedCharacters));
            if (Cfg.GUI.LayoutWidthUpscale)
            {
                LocationsBox.StretchDirection = StretchDirection.Both;
                LocationsBox.MaxWidth = Cfg.GUI.LayoutMaxWidth;
            }
        }
        /// <summary>
        /// Load the layout, limit, default, model info using saved data.
        /// </summary>
        private void LoadLayout()
        {
            string CfgLayout = Path.Combine(cdPath, "stages", Cfg.GUI.Layout, "config.xml");
            string CfgModels = Path.Combine(ModelPath, "config.xml");
            if (File.Exists(CfgLayout) && File.Exists(CfgModels))
            {
                Cfg.Dynamic.Layout ??= GetXmlElement(CfgLayout);
                if (Cfg.Dynamic.Layout is not null)
                {
                    Cfg.Dynamic.RosterValueDefault = Cfg.Dynamic.Layout["Default_Roster"]["roster"].InnerText;
                    Cfg.Dynamic.MenulocationsValueDefault = Cfg.Dynamic.Layout["Default_Roster"]["menulocations"].InnerText;

                    double Multiplier = (Cfg.Dynamic.Layout["Location_Setup"].GetAttribute("spacious") == "true") ? 1 : 1.4;
                    double XStretch = 1;
                    int MinX = (from XmlElement x in Cfg.Dynamic.Layout.GetElementsByTagName("X") select int.Parse(x.InnerText)).Min();
                    var AllY = from XmlElement y in Cfg.Dynamic.Layout.GetElementsByTagName("Y") select int.Parse(y.InnerText);
                    int MinY = AllY.Min();
                    LayoutHeight.MaxHeight = Cfg.GUI.RowLayout ? 5 : (AllY.Max() - MinY) * Multiplier + 30;
                    Locations.Children.Clear();
                    LocationsBox.VerticalAlignment = Cfg.GUI.RowLayout ?
                        VerticalAlignment.Center :
                        VerticalAlignment.Top;
                    Cfg.Dynamic.LayoutLocs = from XmlElement l in Cfg.Dynamic.Layout["Location_Setup"].ChildNodes select int.Parse(l.GetAttribute("Number"));
                    Cfg.Roster.Total = Cfg.Dynamic.Layout["Location_Setup"].ChildNodes.Count;
                    foreach (XmlElement ML in Cfg.Dynamic.Layout["Location_Setup"].ChildNodes)
                    {
                        string Loc = ML.GetAttribute("Number").PadLeft(2, '0');
                        double Y = (int.Parse(ML["Y"].InnerText) - MinY) * Multiplier;
                        double X = int.Parse(ML["X"].InnerText);
                        if (Cfg.GUI.RowLayout)
                        {
                            XStretch = Math.Abs(X) / (Math.Abs(MinX) * 1.6) + 1;
                            Y = int.Parse(ML["Z"].InnerText) * 1.7 + Y * (Multiplier - 0.85);
                        }
                        X = (X * XStretch - MinX * (Cfg.GUI.RowLayout ? 1 / 1.6 + 1 : 1)) * Multiplier;
                        ToggleButton LocButton = new()
                        {
                            Content = Loc,
                            Padding = new Thickness(5, 2, 5, 2),
                            Margin = new Thickness(X, 0, 0, Y),
                            AllowDrop = true,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Bottom,
                        };
                        ToolTip Tt = new();
                        Tt.Opened += new RoutedEventHandler(LocButton_ToolTip);
                        ToolTipService.SetToolTip(LocButton, Tt);
                        LocButton.Click += new RoutedEventHandler(LocButton_Click);
                        LocButton.Drop += new DragEventHandler(LocButton_Drop);
                        LocButton.DragOver += new DragEventHandler(SelectedCharacters_DragOver);
                        LocButton.PointerEntered += LocButton_PointerEntered;

                        Locations.Children.Add(LocButton);
                    }
                    LayoutDetails.Text = $"Layout: {Cfg.Dynamic.Layout["Information"]["name"].InnerText} for {Cfg.Dynamic.Layout["Information"]["platform"].InnerText}";
                }
                StageDetails.Text = "";
                StageImage.Source = null;
                if (GetXmlElement(CfgModels) is XmlElement Models
                        && Models.SelectSingleNode($"descendant::Model[Name='{Cfg.GUI.Model}']") is XmlElement M
                        && Cfg.Dynamic.Layout.SelectSingleNode($"descendant::Model[text()='{M.ParentNode.Name}']") is XmlElement CM
                        && GetStageInfo(M, CM) is StageModel StageItem)
                {
                    Cfg.Dynamic.SelectedStage = StageItem;
                    StageDetails.Text = $"Model: {StageItem.Name} by {StageItem.Creator}";
                    StageImage.Source = StageItem.Image;
                }
            }
            UpdateLocBoxes();
        }
        /// <summary>
        /// Update the state of all location boxes. Required after each add process except when clicked (or dragged?).
        /// </summary>
        private void UpdateLocBoxes()
        {
            foreach (ToggleButton LocBox in Locations.Children.Cast<ToggleButton>())
            {
                LocBox.IsChecked = Cfg.Roster.Selected.Any(c => c.Loc == LocBox.Content.ToString());
            }
        }
        // Control handlers:
        private void BtnRunGame_Click(object sender, RoutedEventArgs e)
        {
            RunGame(Cfg.GUI.GameInstallPath, Cfg.OHS.ExeName, Cfg.GUI.ExeArguments);
        }

        private void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (SelectedCharacter c in Cfg.Roster.Selected)
            {
                c.Unlock = true;
            }
        }
        /// <summary>
        /// Roster Hack switch: A red number warns us about missing roster hack files.
        /// </summary>
        private void RosterHack_Toggled(object sender, RoutedEventArgs e)
        {
            string GamePath = string.IsNullOrEmpty(Cfg.GUI.GameInstallPath)
                ? Cfg.OHS.GameInstallPath
                : Cfg.GUI.GameInstallPath;
            string dinput = Path.Combine(GamePath, "dinput8.dll");
            if (File.Exists(dinput)) { dinput = File.ReadAllText(dinput); }
            bool RHFilesExist = Directory.Exists(Path.Combine(GamePath, "plugins")) && dinput.Contains("asi-loader");

            RosterHackToggle.Foreground = RosterHackToggle.IsOn && !RHFilesExist
                ? new SolidColorBrush(Microsoft.UI.Colors.Red)
                : UnlockToggle.Foreground;
            RosterHackToggle.Header = RosterHackToggle.IsOn
                ? RHFilesExist
                ? "(Roster Hack)"
                : "(RH not found!)"
                : "(Default)";
        }
        /// <summary>
        /// Reload stage info from configuration files and images.
        /// </summary>
        private void RefreshStages_Click(object sender, RoutedEventArgs e) => LoadLayout();
        /// <summary>
        /// Open the stage selection tab.
        /// </summary>
        private void SelectStage_Click(object sender, RoutedEventArgs e)
        {
            _ = Frame.Navigate(typeof(Tab_Stages));
        }

        private void LocButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton LocBox) AddToSelectedMUA(LocBox);
        }

        private void LocButton_Drop(object sender, DragEventArgs e)
        {
            if (sender is ToggleButton LocBox) AddToSelectedMUA(LocBox);
        }
        private void AddToSelectedMUA(ToggleButton LocBox)
        {
            if (!string.IsNullOrEmpty(Cfg.Dynamic.FloatingCharacter))
            {
                if (AddToSelected(LocBox.Content.ToString(), Cfg.Dynamic.FloatingCharacter)) { UpdateLocBoxes(); }
                UpdateClashes();
            }
            LocBox.IsChecked = Cfg.Roster.Selected.Any(c => c.Loc == LocBox.Content.ToString());
        }
        /// <summary>
        /// When the pointer enters a location button: Write the location number in the FloatingLoc variable.
        /// </summary>
        private void LocButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // we may need an event to clear the floating character on exit, but it's probably better to not, so it stays available until entering the next location.
            if (sender is ToggleButton Loc)
            {
                FloatingLoc = Loc.Content as string;
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
                _ = AddToSelected(FC);
                UpdateLocBoxes();
                UpdateClashes();
            }
        }
        /// <summary>
        /// Load the default roster for the current layout.
        /// </summary>
        private void MUA_LoadDefault(object sender, RoutedEventArgs e)
        {
            LoadRosterVal(Cfg.Dynamic.RosterValueDefault, Cfg.Dynamic.MenulocationsValueDefault);
            UpdateLocBoxes();
        }
        /// <summary>
        /// Browse for a roster file to load. Populates according to the layout setup, should be left to right. Currently ignores menulocation files.
        /// </summary>
        private async void MUA_LoadRoster(SplitButton sender, SplitButtonClickEventArgs args)
        {
            string? RosterValue = await LoadDialogue(".cfg");
            if (RosterValue != null)
            {
                LoadRoster(RosterValue);
                UpdateLocBoxes();
            }
        }
        /// <summary>
        /// Generate a random character list from the available characters.
        /// </summary>
        private void MUA_Random(object sender, RoutedEventArgs e)
        {
            LoadRandomRoster();
            UpdateLocBoxes();
        }
        /// <summary>
        /// Clear the selected roster list
        /// </summary>
        private void MUA_Clear(object sender, RoutedEventArgs e)
        {
            Cfg.Roster.Selected.Clear();
            Cfg.Roster.NumClash = false;
            UpdateLocBoxes();
        }

        private void SelectedCharacters_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => UpdateLocBoxes();

        private void AvailableCharacters_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => UpdateLocBoxes();
    }
}
