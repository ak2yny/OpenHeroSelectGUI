using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The Skin details tab that uses the <see cref="OpenHeroSelectGUI.SkinDetailsPage"/> pane to show <see cref="SkinDetails"/>.
    /// </summary>
    public sealed partial class Tab_SkinEditor : Page
    {
        public Cfg Cfg { get; set; } = new();

        public Tab_SkinEditor()
        {
            InitializeComponent();
            _ = AvailableCharacters.Navigate(typeof(AvailableCharacters));
            _ = SkinDetailsPage.Navigate(typeof(SkinDetailsPage));
            Cfg.Var.SE_Msg_Info = Cfg.Var.SE_Msg_Error = Cfg.Var.SE_Msg_Success = Cfg.Var.SE_Msg_Warning = Cfg.Var.SE_Msg_WarnPkg = new MessageItem();
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
                if (InternalSettings.RavenFormats.Contains(Path.GetExtension(HS)))
                {
                    DirectoryInfo TempFolder = Directory.CreateDirectory(Path.Combine(OHSpath.CD, "Temp"));
                    string DHS = Path.Combine(TempFolder.FullName, $"{Path.GetFileNameWithoutExtension(HS)}.xml");
                    if (Util.RunExeInCmd("json2xmlb", $"-d \"{HS}\" \"{DHS}\"")
                        && GUIXML.SplitXMLStats(DHS, OutputFolder.Text))
                    {
                        TempFolder.Delete(true);
                        SplitFinished();
                    }
                }
                else
                {
                    string[] LoadedHerostat = File.ReadLines(HS).ToArray();
                    char HsFormat = LoadedHerostat.First(s => !string.IsNullOrEmpty(s.Trim())).Trim()[0];
                    if (HsFormat == '<')
                    {
                        if (!GUIXML.SplitXMLStats(HS, OutputFolder.Text)) { return; }
                    }
                    else
                    {
                        Herostat.Split(LoadedHerostat, HsFormat, OutputFolder.Text);
                    }
                    SplitFinished();
                }
            }
        }
        /// <summary>
        /// Display success message in <see cref="InfoBar"/> for 5 seconds
        /// </summary>
        private async void SplitFinished()
        {
            Cfg.Var.SE_Msg_Success = new MessageItem
            {
                Message = $"Split herostats to '{OHSpath.GetOHSFolder(OutputFolder.Text)}'",
                IsOpen = true
            };
            await Task.Delay(5000);
            Cfg.Var.SE_Msg_Success = new MessageItem() { IsOpen = false };
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start("explorer.exe", $"/select, \"{OHSpath.GetOHSFolder(OutputFolder.Text)}\"");
        }
    }
}
