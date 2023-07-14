using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Windows.ApplicationModel.DataTransfer;
using static OpenHeroSelectGUI.Settings.CfgCommands;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Character selection page for MUA
    /// </summary>
    public sealed partial class Tab_MUA : Page
    {
        private ObservableCollection<string> Layouts { get; } = new();
        private ObservableCollection<string> Stages { get; } = new();
        public XmlElement? Models { get; set; }
        public Cfg Cfg { get; set; } = new();
        public string? FloatingLoc { get; set; }
        public int? SelectedCount { get => Cfg.Roster.Selected.Count; }
        public Tab_MUA()
        {
            InitializeComponent();
            if (Cfg.MUA.ExeName == "") Cfg.MUA.ExeName = "Game.exe";
            LoadLayouts();
            UpdateLocBoxes();
            AvailableCharacters.Navigate(typeof(AvailableCharacters));
            SelectedCharacters.Navigate(typeof(SelectedCharacters));
        }
        /// <summary>
        /// Load the stage information, images, locations
        /// </summary>
        private void LoadLayouts()
        {
            ReloadLayouts();
            Cfg.Dynamic.SelectedLayout = Layouts.IndexOf(Cfg.GUI.Layout);
            Stages.Clear();
            Models = GetXmlElement(Path.Combine(ModelPath, "config.xml"));

        }
        private void ReloadLayouts()
        {
            Layouts.Clear();
            DirectoryInfo folder = new(Path.Combine(cdPath, "stages"));
            foreach (DirectoryInfo f in folder.GetDirectories().Where(d => !d.Name.StartsWith(".")).ToList())
            {
                Layouts.Add(f.Name);
            }
        }
        /// <summary>
        /// Load the layout, limit, default, model list by providing the path to a stage layout config.xml file.
        /// </summary>
        private void LoadLayout(string CfgFile)
        {
            if (File.Exists(CfgFile))
            {
                XmlElement? Layout = GetXmlElement(CfgFile);
                if (Layout is not null && Models is not null)
                {
                    // Note: config.xml can theoretically not contain the required entries at the correct location. If this is the case, the app will throw an unhandled exception. This can be avoided by verifying the XML with a scheme definition.
                    LayoutDetails.Text = $"Layout: {Layout["Information"]["name"].InnerText} for {Layout["Information"]["platform"].InnerText}";
                    foreach (XmlElement CM in Layout["Compatible_Models"].ChildNodes)
                    {
                        if (CM.InnerText == "Official" && CM.GetAttribute("Riser") == "true")
                        {
                            Cfg.Dynamic.Riser = true;
                        }
                        foreach (XmlElement M in Models[CM.InnerText].ChildNodes)
                        {
                            Stages.Add(M["Name"].InnerText);
                        }
                    }
                    Cfg.Dynamic.SelectedModel = Stages.IndexOf(Cfg.GUI.Model);
                    Cfg.Dynamic.RosterValueDefault = Layout["Default_Roster"]["roster"].InnerText;
                    Cfg.Dynamic.MenulocationsValueDefault = Layout["Default_Roster"]["menulocations"].InnerText;

                    double Multiplier = (Layout["Location_Setup"].GetAttribute("spacious") == "true") ? 1 : 1.4;
                    double XStretch = 1;
                    int MinX = (from XmlElement x in Layout.GetElementsByTagName("X") select int.Parse(x.InnerText)).Min();
                    var AllY = from XmlElement y in Layout.GetElementsByTagName("Y") select int.Parse(y.InnerText);
                    int MinY = AllY.Min();
                    LayoutHeight.MaxHeight = (Cfg.GUI.RowLayout) ? 5 : (AllY.Max() - MinY) * Multiplier + 30;
                    Locations.Children.Clear();
                    Locations.VerticalAlignment = (Cfg.GUI.RowLayout) ?
                        VerticalAlignment.Center :
                        VerticalAlignment.Top;
                    Cfg.Dynamic.LayoutLocs = from XmlElement l in Layout["Location_Setup"].ChildNodes select int.Parse(l.GetAttribute("Number"));
                    Cfg.Roster.Total = Layout["Location_Setup"].ChildNodes.Count;
                    foreach (XmlElement ML in Layout["Location_Setup"].ChildNodes)
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
                    UpdateLocBoxes();
                }
            }
        }
        /// <summary>
        /// Get the root XML elemet by providing the path to an XML file. File must not have an XML identifier.
        /// </summary>
        /// <returns>The root XML element containing the complete XML structure</returns>
        private static XmlElement? GetXmlElement(string Path)
        {
            XmlElement? XmlElement = null;
            if (File.Exists(Path))
            {
                XmlDocument XmlDocument = new();
                using XmlReader reader = XmlReader.Create(Path, new XmlReaderSettings() { IgnoreComments = true });
                XmlDocument.Load(reader);
                if (XmlDocument.FirstChild is XmlElement Root) XmlElement = Root;
            }
            return XmlElement;
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
        /// <summary>
        /// Roster Hack switch: A red number warns us from missing roster hack files.
        /// </summary>
        private void RosterHack_Toggled(object sender, RoutedEventArgs e)
        {
            RosterHackToggle.Foreground = UnlockToggle.Foreground;
            RosterHackToggle.Header = "(Default)";
            if (RosterHackToggle.IsOn)
            {
                RosterHackToggle.Header = "(Roster Hack)";
                string dinput = Path.Combine(Cfg.OHS.GameInstallPath, "dinput8.dll");
                string plugin = Path.Combine(Cfg.OHS.GameInstallPath, "plugins");
                if (File.Exists(dinput)) { dinput = File.ReadAllText(dinput); }
                if (!Directory.Exists(plugin) || !dinput.Contains("asi-loader"))
                {
                    RosterHackToggle.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0));
                    RosterHackToggle.Header = "(RH not found!)";
                }
            }
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
        /// <summary>
        /// Load the layout, limit, default, model list for the selected stage layout.
        /// </summary>
        private void LayoutBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LayoutDetails.Text = "";
            Stages.Clear();
            Cfg.Dynamic.SelectedModel = -1;
            Cfg.Dynamic.Riser = false;
            if (LayoutBox is ComboBox LB && LB.SelectedItem is string Selected)
            {
                LoadLayout(Path.Combine(cdPath, "stages", Selected, "config.xml"));
                Cfg.GUI.Layout = Selected;
            }
        }

        private void RefreshLayouts_Click(object sender, RoutedEventArgs e)
        {
            ReloadLayouts();
        }
        /// <summary>
        /// Load the image preview and define the variables for the selected stage model.
        /// </summary>
        private void StagesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StageDetails.Text = "";
            StageImage.Source = null;
            if (StagesBox is ComboBox SB && SB.SelectedItem is string Selected)
            {
                XmlNode? M = Models.GetElementsByTagName("Model").Cast<XmlNode>()
                                   .First(n => n["Name"].InnerText == StagesBox.SelectedValue.ToString());
                Cfg.Dynamic.SelectedModelPath = Path.Combine(ModelPath, M["Path"].InnerText);

                StageDetails.Text = $"Model: {M["Name"].InnerText} by {M["Creator"].InnerText}";
                DirectoryInfo ModelFolder = new(Cfg.Dynamic.SelectedModelPath);
                if (ModelFolder.Exists)
                {
                    var ModelImages = ModelFolder.EnumerateFiles("*.png").Union(ModelFolder.EnumerateFiles("*.jpg"));
                    if (ModelImages.Any())
                    {
                        StageImage.Source = new BitmapImage(new Uri(ModelImages.First().FullName));
                    }
                }
                Cfg.GUI.Model = Selected;
            }
        }
        /// <summary>
        /// Reload stage configuration file. New stages only show when the layout selection changes.
        /// </summary>
        private void RefreshStages_Click(object sender, RoutedEventArgs e)
        {
            Models = GetXmlElement(Path.Combine(ModelPath, "config.xml"));
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
                if (AddToSelected(LocBox.Content.ToString(), Cfg.Dynamic.FloatingCharacter)) UpdateLocBoxes();
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
                UpdateLocBoxes();
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
            Cfg.Roster.Count = 0;
            UpdateLocBoxes();
        }

        private void SelectedCharacters_Delete(UIElement sender, ProcessKeyboardAcceleratorEventArgs args) => UpdateLocBoxes();

        private void AvailableCharacters_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => UpdateLocBoxes();
    }
}
