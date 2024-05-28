using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using Windows.ApplicationModel.DataTransfer;
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

        public Tab_MUA()
        {
            InitializeComponent();
            if (Cfg.MUA.ExeName == "") { Cfg.MUA.ExeName = OHSpath.DefaultExe; }

            LoadLayout();
            _ = AvailableCharacters.Navigate(typeof(AvailableCharacters));
            _ = SelectedCharacters.Navigate(typeof(SelectedCharacters));
            if (Cfg.GUI.LayoutWidthUpscale)
            {
                LocationsBox.StretchDirection = StretchDirection.Both;
                LocationsBox.MaxWidth = Cfg.GUI.LayoutMaxWidth;
            }
            if (Util.GameExe(OHSpath.MUAexe) is FileStream fs)
            {
                byte[] bytes = new byte[2];
                fs.Position = 0x3cc28f;
                _ = fs.Read(bytes, 0, bytes.Length);
                fs.Close();
                USDnum.Text = System.Text.Encoding.Default.GetString(bytes);
            }
        }
        /// <summary>
        /// Load the layout, limit, default, model info, using saved data.
        /// </summary>
        private void LoadLayout()
        {
            string CfgLayout = Path.Combine(OHSpath.CD, "stages", Cfg.GUI.Layout, "config.xml");
            string CfgModels = Path.Combine(OHSpath.Model, "config.xml");
            if (File.Exists(CfgLayout) && File.Exists(CfgModels))
            {
                Cfg.Var.Layout ??= GUIXML.GetXmlElement(CfgLayout);
                if (Cfg.Var.Layout is XmlElement CL)
                {
                    Cfg.Var.RosterValueDefault = CL["Default_Roster"]!["roster"]!.InnerText;
                    Cfg.Var.MenulocationsValueDefault = CL["Default_Roster"]!["menulocations"]!.InnerText;

                    double Multiplier = (CL["Location_Setup"]!.GetAttribute("spacious") == "true") ? 1 : 1.4;
                    double XStretch = 1;
                    int MinX = (from XmlElement x in CL.GetElementsByTagName("X") select int.Parse(x.InnerText)).Min();
                    var AllY = from XmlElement y in CL.GetElementsByTagName("Y") select int.Parse(y.InnerText);
                    int MinY = AllY.Min();
                    LayoutHeight.MaxHeight = Cfg.GUI.RowLayout ? 5 : (AllY.Max() - MinY) * Multiplier + 30;
                    Locations.Children.Clear();
                    LocationsBox.VerticalAlignment = Cfg.GUI.RowLayout ?
                        VerticalAlignment.Center :
                        VerticalAlignment.Top;
                    Cfg.Var.LayoutLocs = from XmlElement l in CL["Location_Setup"]!.ChildNodes select int.Parse(l.GetAttribute("Number"));
                    Cfg.Roster.Total = CL["Location_Setup"]!.ChildNodes.Count;
                    foreach (XmlElement ML in CL["Location_Setup"]!.ChildNodes)
                    {
                        string Loc = ML.GetAttribute("Number").PadLeft(2, '0');
                        double Y = (int.Parse(ML["Y"]!.InnerText) - MinY) * Multiplier;
                        double X = int.Parse(ML["X"]!.InnerText);
                        if (Cfg.GUI.RowLayout)
                        {
                            XStretch = Math.Abs(X) / (Math.Abs(MinX) * 1.6) + 1;
                            Y = int.Parse(ML["Z"]!.InnerText) * 1.7 + Y * (Multiplier - 0.85);
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
                    LayoutDetails.Text = $"Layout: {CL["Information"]!["name"]!.InnerText} for {CL["Information"]!["platform"]!.InnerText}";
                    StageDetails.Text = "";
                    StageImage.Source = null;
                    if (GUIXML.GetXmlElement(CfgModels) is XmlElement Models
                        && Models.SelectSingleNode($"descendant::Model[Name=\"{Cfg.GUI.Model}\"]") is XmlElement M
                        && M.ParentNode is XmlElement P
                        && CL.SelectSingleNode($"descendant::Model[text()='{P.Name}']") is XmlElement CM
                        && GUIXML.GetStageInfo(M, CM) is StageModel StageItem)
                    {
                        Cfg.Var.SelectedStage = StageItem;
                        StageDetails.Text = $"Model: {StageItem.Name} by {StageItem.Creator}";
                        StageImage.Source = StageItem.Image;
                    }
                }
            }
        }
        /// <summary>
        /// Update the state of all location boxes. Required after each add and remove process except when clicked or dragged on unoccupied locations.
        /// </summary>
        private void UpdateLocBoxes()
        {
            foreach (ToggleButton LocBox in Locations.Children.Cast<ToggleButton>())
            {
                LocBox.IsChecked = Cfg.Roster.Selected.Any(c => c.Loc == LocBox.Content.ToString());
            }
        }
        /// <summary>
        /// If RH option is on, checks for the roster hack in the game folder and updates the RH message.
        /// </summary>
        private void UpdateRH()
        {
            if (RosterHackToggle.IsOn)
            {
                string dinput = Path.Combine(OHSpath.GamePath, "dinput8.dll");
                if (File.Exists(dinput)) { try { dinput = File.ReadAllText(dinput); } catch { dinput = ""; } }
                RHInfo.Message = $"Roster hack (RH) not detected in '{OHSpath.GamePath}'. This message can be ignored, if the RH's installed in the actual game folder or if detection failed for another reason. MO2 users can browse for the actual game .exe, to keep this message from opening in the future. The RH fixes a crash when using more than 27 characters.";
                RHInfo.IsOpen = !Directory.Exists(Path.Combine(OHSpath.GamePath, "plugins")) || !dinput.Contains("asi-loader");
            }
            else
            {
                RHInfo.IsOpen = false;
            }
        }
        // Control handlers:
        private void BtnRunGame_Click(object sender, RoutedEventArgs e) => Util.RunGame();

        private void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (SelectedCharacter c in Cfg.Roster.Selected)
            {
                c.Unlock = true;
            }
        }
        /// <summary>
        /// Roster Hack switch: A message warns us about potentially missing roster hack files.
        /// </summary>
        private void RosterHack_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateRH();
        }
        /// <summary>
        /// Re-load stage info from configuration files and images.
        /// </summary>
        private void RefreshStages_Click(object sender, RoutedEventArgs e)
        {
            LoadLayout();
            UpdateLocBoxes();
        }

        /// <summary>
        /// Open the stage selection tab.
        /// </summary>
        private void SelectStage_Click(object sender, RoutedEventArgs e)
        {
            _ = Frame.Navigate(typeof(Tab_Stages));
        }
        /// <summary>
        /// Hex edits the upside-down arrow location, if the USD button was pressed, otherwise adds the Cfg.Var.FloatingCharacter to the selected characters list, at the location of the clicked location box.
        /// </summary>
        private void LocButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton LocBox)
            {
                if (Cfg.Var.FloatingCharacter == "<"
                    && LocBox.Content.ToString() is string Loc
                    && Util.HexEdit(0x3cc28f, Loc, OHSpath.MUAexe))
                {
                    USDnum.Text = Loc;
                    Cfg.Var.FloatingCharacter = "";
                }
                AddToSelectedMUA(LocBox);
            }
        }

        private void LocButton_Drop(object sender, DragEventArgs e)
        {
            if (sender is ToggleButton LocBox) { AddToSelectedMUA(LocBox); }
        }
        /// <summary>
        /// Add the floating character to the selected list on location <paramref name="LocBox"/>
        /// </summary>
        private void AddToSelectedMUA(ToggleButton LocBox)
        {
            if (!string.IsNullOrEmpty(Cfg.Var.FloatingCharacter) && Cfg.Var.FloatingCharacter != "<")
            {
                if (AddToSelected(LocBox.Content.ToString(), Cfg.Var.FloatingCharacter)) { UpdateLocBoxes(); }
                UpdateClashes(false);
            }
            LocBox.IsChecked = Cfg.Roster.Selected.Any(c => c.Loc == LocBox.Content.ToString());
        }
        /// <summary>
        /// When the pointer enters a location button: Write the location number in the FloatingLoc variable.
        /// </summary>
        private void LocButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
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
                _ = AddToSelected(FC);
                UpdateClashes();
            }
        }
        /// <summary>
        /// Load the default roster for the current layout.
        /// </summary>
        private void MUA_LoadDefault(object sender, RoutedEventArgs e)
        {
            LoadRosterVal(Cfg.Var.RosterValueDefault, Cfg.Var.MenulocationsValueDefault);
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
            Cfg.Roster.Selected.Clear();
            Cfg.Roster.NumClash = false;
            foreach (ToggleButton LocBox in Locations.Children.Cast<ToggleButton>()) { LocBox.IsChecked = false; }
        }
        /// <summary>
        /// Invoked when the text of a hidden text box changes, which happens on UpdateClash events, due to binding.
        /// </summary>
        private void ClashesUpdated(object sender, TextChangedEventArgs e) => UpdateLocBoxes();
        /// <summary>
        /// Browse for the game folder with the roster hack, if using MO2.
        /// </summary>
        private async void BrowseRH_Click(object sender, RoutedEventArgs e)
        {
            Cfg.GUI.ActualGameExe = await CfgCmd.LoadDialogue(".exe") ?? "";
            UpdateRH();
        }

        private void USD_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (Util.HexEdit(0x3cc28f, "00", OHSpath.MUAexe))
            {
                USDnum.Text = "00";
            }
        }

        private void USD_Click(object sender, RoutedEventArgs e)
        {
            Cfg.Var.FloatingCharacter = "<";
        }
    }
}
