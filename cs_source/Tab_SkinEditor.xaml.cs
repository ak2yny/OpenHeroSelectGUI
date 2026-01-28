using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The Skin details tab that uses the <see cref="SkinDetailsPage"/> pane to show <see cref="SkinDetails"/>.
    /// </summary>
    public sealed partial class Tab_SkinEditor : Page
    {
        public Cfg Cfg { get; set; } = new();
        internal Messages Msg { get; set; } = new();

        public Tab_SkinEditor()
        {
            InitializeComponent();
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OutputFolder.Text = OHSpath.TrimGameFolder(await CfgCmd.BrowseFolder());
        }

        private void ResetOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            OutputFolder.Text = CfgSt.OHS.HerostatFolder;
        }

        private async void LoadHerostat_Click(object sender, RoutedEventArgs e)
        {
            if (await CfgCmd.LoadDialogue("*") is string HS)
            {
                SplitHS(HS);
            }
        }

        private void SplitHS(string HS)
        {
            try
            {
                string Out = Directory.CreateDirectory(OHSpath.GetRooted(string.IsNullOrWhiteSpace(OutputFolder.Text)
                    ? CfgSt.OHS.HerostatFolder
                    : OutputFolder.Text)).FullName;
                if (InternalSettings.RavenFormats.Contains(Path.GetExtension(HS), StringComparer.OrdinalIgnoreCase))
                {
                    string DHS = Path.Combine(OHSpath.Temp, $"{Path.GetFileNameWithoutExtension(HS)}.xml");
                    if (Util.RunExeInCmd("json2xmlb", $"-d \"{HS}\" \"{DHS}\"")
                        && MarvelModsXML.SplitXMLStats(DHS, Out))
                    {
                        SplitFinished(Out);
                    }
                }
                else
                {
                    using FileStream fs = File.OpenRead(HS);
                    using StreamReader sr = new(fs);
                    char HsFormat;
                    while (char.IsWhiteSpace(HsFormat = (char)sr.Read()) && !sr.EndOfStream) { }
                    fs.Close();
                    if (HsFormat == '<')
                    {
                        if (!MarvelModsXML.SplitXMLStats(HS, Out)) { return; }
                    }
                    else
                    {
                        Herostat.Split(File.ReadAllLines(HS), HsFormat == '{', Out);
                    }
                    SplitFinished(Out);
                }
            }
            catch { } // Just don't split/continue (Possibly show error messages in future versions)
        }
        /// <summary>
        /// Display split finished message in <see cref="InfoBar"/> for 5 seconds
        /// </summary>
        private async void SplitFinished(string Out)
        {
            Msg.SE_Success.Message = $"Split herostats to '{Out}'";
            await Task.Delay(5000);
            Msg.SE_Success.IsOpen = false;
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start("explorer.exe", new Uri(OHSpath.GetRooted(OutputFolder.Text)).LocalPath);
        }

        private void SplitterDropArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                SplitterDropArea.Visibility = Visibility.Visible;
            }
        }

        private void SplitterDropAreaBG_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Split herostat";
        }

        private void SplitterDropAreaBG_DragLeave(object sender, DragEventArgs e)
        {
            SplitterDropArea.Visibility = Visibility.Collapsed;
        }

        private async void SplitterDropAreaBG_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)
                && (await e.DataView.GetStorageItemsAsync())[0] is StorageFile HS)
            {
                SplitHS(HS.Path);
            }
            SplitterDropArea.Visibility = Visibility.Collapsed;
        }
    }
}
