using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System.IO;
using System.Linq;
using System.Xml;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// A page to select from the available stages.
    /// </summary>
    public sealed partial class Tab_Stages : Page
    {
        public Cfg Cfg { get; set; } = new();
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
            if (Path.Combine(OHSpath.CD, "stages") is string SP && Directory.Exists(SP))
            {
                DirectoryInfo folder = new(SP);
                foreach (DirectoryInfo f in folder.GetDirectories().Where(d => !d.Name.StartsWith('.') && File.Exists(Path.Combine(d.FullName, "config.xml"))).ToList())
                {
                    StageLayouts.Items.Add(f.Name);
                }
                StageLayouts.SelectedIndex = StageLayouts.Items.IndexOf(Cfg.GUI.Layout);
            }
        }
        /// <summary>
        /// Load the compatible models from the config and populate the thumbnails
        /// </summary>
        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StageLayouts.SelectedItem is string Selected && Path.Combine(OHSpath.CD, "stages", Selected, "config.xml") is string Config && File.Exists(Config))
            {
                StageThumbnails.Items.Clear();
                Cfg.Var.Layout = GUIXML.GetXmlElement(Config);
                if (Cfg.Var.Layout is XmlElement Layout && Layout["Compatible_Models"] is XmlNode CMP)
                {
                    foreach (XmlElement CM in CMP.ChildNodes)
                    {
                        if (GUIXML.GetXmlElement(Path.Combine(OHSpath.Model, "config.xml")) is XmlElement MCfg && MCfg[CM.InnerText] is XmlNode MC)
                        {
                            foreach (XmlElement M in MC.ChildNodes)
                            {
                                if (GUIXML.GetStageInfo(M, CM) is StageModel StageItem)
                                {
                                    StageThumbnails.Items.Add(StageItem);
                                }
                            }
                        }
                    }
                }
                StageModel? SelectedModel = StageThumbnails.Items.Cast<StageModel>().FirstOrDefault(m => m.Name == Cfg.GUI.Model);
                StageThumbnails.SelectedIndex = StageThumbnails.Items.IndexOf(SelectedModel);
            }
        }
        /// <summary>
        /// Save the selection to <see cref="GUIsettings"/> and navigate back to the <see cref="Tab_MUA"/>
        /// </summary>
        private void StageConfirmed()
        {
            if (StageThumbnails.SelectedItem is StageModel SelectedModel && StageLayouts.SelectedItem is string SelectedLayout)
            {
                Cfg.GUI.Layout = SelectedLayout;
                Cfg.GUI.Model = SelectedModel.Name ?? "";
                _ = Frame.Navigate(typeof(Tab_MUA));
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e) => ReloadLayouts();

        private void StageThumbnails_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => StageConfirmed();

        private void Stage_Confirm(object sender, RoutedEventArgs e) => StageConfirmed();
    }
}
