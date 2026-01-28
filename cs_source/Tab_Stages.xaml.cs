using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// A page to select from the available stages.
    /// </summary>
    public sealed partial class Tab_Stages : Page
    {
        public Cfg Cfg { get; set; } = new();

        private Settings.Layout? SelectedLayout;

        public Tab_Stages()
        {
            InitializeComponent();
            ReloadLayouts();
        }
        /// <summary>
        /// Load the laout folder list and populate the layout list view
        /// </summary>
        private void ReloadLayouts()
        {
            StageLayouts.Items.Clear();
            try
            {
                foreach (string LayoutName in Directory.EnumerateDirectories(OHSpath.StagesDir)
                    .Select(d => Path.GetFileName(d))
                    .Where(d => !d.StartsWith('.') && !StageLayouts.Items.Contains(d)
                        && File.Exists($"{OHSpath.StagesDir}/{d}/config.xml")))
                {
                    StageLayouts.Items.Add(LayoutName);
                }
                StageLayouts.SelectedItem = Cfg.GUI.Layout;
            }
            catch (System.Exception ex) { Error.Message = ex.Message; Error.IsOpen = true; }
        }
        /// <summary>
        /// Load the compatible models from the config and populate the thumbnails.
        /// </summary>
        /// <remarks>Clears thumbnails and reloads all <see cref="CSSModel"/> with images every time to reflect changes on disk.</remarks>
        private void LoadModels()
        {
            if (StageLayouts.SelectedItem is string Selected)
            {
                CSSModel? SelectedModel = null;
                StageThumbnails.Items.Clear();
                if (InternalSettings.Models is not null &&
                    GUIXML.Deserialize(Path.Combine(OHSpath.StagesDir, Selected, "config.xml"),
                                       typeof(Settings.Layout)) is Settings.Layout CL)
                {
                    SelectedLayout = CL;
                    RHNotEnabled.IsOpen = !Cfg.MUA.RosterHack && CL.LocationSetup.Locations.Count > 27;
                    for (int i = 0; i < CL.CompatibleModels.Length; i++)
                    {
                        CompatibleModel CM = CL.CompatibleModels[i];
                        if (!string.IsNullOrWhiteSpace(CM.Name) && InternalSettings.Models.categories.TryGetValue(CM.Name, out List<Model>? Models))
                        {
                            for (int j = 0; j < Models.Count; j++)
                            {
                                if (Models[j].ToCSSModel(!Cfg.GUI.StageFavouritesOn) is CSSModel StageItem)
                                {
                                    StageThumbnails.Items.Add(StageItem);
                                    if (StageItem.Name == Cfg.GUI.Model) { SelectedModel = StageItem; }
                                }
                            }
                        }
                    }
                }
                StageThumbnails.SelectedIndex = StageThumbnails.Items.IndexOf(SelectedModel);
            }
        }
        /// <summary>
        /// Save the selection to <see cref="GUIsettings"/> and navigate back to the <see cref="Tab_MUA"/>
        /// </summary>
        private void StageConfirmed()
        {
            if (StageThumbnails.SelectedItem is CSSModel SelectedModel
                && StageLayouts.SelectedItem is string SL
                && SelectedLayout is not null)
            {
                Cfg.GUI.Layout = SL;
                Cfg.GUI.Model = SelectedModel.Name;
                Cfg.Var.SelectedStage = SelectedModel;
                SelectedLayout.SetLocationsMUA();
                Cfg.MUA.RosterHack = SelectedLayout.LocationSetup.Locations.Count > 27;
                CfgSt.CSS = SelectedLayout;
                _ = Frame.Navigate(typeof(Tab_MUA));
            }
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadModels();

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            InternalSettings.LoadModels();
            ReloadLayouts();
        }

        private void BtnFilterFavs_Click(object sender, RoutedEventArgs e) => LoadModels();

        private void StageThumbnails_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => StageConfirmed();

        private void Stage_Confirm(object sender, RoutedEventArgs e) => StageConfirmed();

        private void Stage_Cancel(object sender, RoutedEventArgs e)
        {
            _ = Frame.Navigate(typeof(Tab_MUA));
        }

        private void AddToFavourites(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton FB && FB.DataContext is CSSModel Stage && Stage.Name is not null)
            {
                _ = FB.IsChecked == true
                    ? Cfg.GUI.StageFavourites.Add(Stage.Name)
                    : Cfg.GUI.StageFavourites.Remove(Stage.Name);
            }
        }
    }
}
