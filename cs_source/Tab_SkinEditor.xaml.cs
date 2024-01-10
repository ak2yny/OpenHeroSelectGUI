using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static OpenHeroSelectGUI.Settings.CfgCommands;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The Skin details tab that uses the skin details pane (page) to show skin details.
    /// </summary>
    public sealed partial class Tab_SkinEditor : Page
    {
        [GeneratedRegex(@"^""?menulocation(\"": | = )")]
        private static partial Regex MLRX();

        public Cfg Cfg { get; set; } = new();
        public Tab_SkinEditor()
        {
            InitializeComponent();
            _ = AvailableCharacters.Navigate(typeof(AvailableCharacters));
            _ = SkinDetailsPage.Navigate(typeof(SkinDetailsPage));
            Cfg.Dynamic.SE_Msg_Info = Cfg.Dynamic.SE_Msg_Error = Cfg.Dynamic.SE_Msg_Success = Cfg.Dynamic.SE_Msg_Warning = Cfg.Dynamic.SE_Msg_WarnPkg = new MessageItem();
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OutputFolder.Text = TrimGameFolder(await BrowseFolder());
        }

        private void ResetOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            OutputFolder.Text = OHSsettings.Instance.HerostatFolder;
        }

        private async void LoadHerostat_Click(object sender, RoutedEventArgs e)
        {
            if (await LoadDialogue("*") is string HS)
            {
                if (RavenFormats.Contains(Path.GetExtension(HS)))
                {
                    DirectoryInfo TempFolder = Directory.CreateDirectory(Path.Combine(cdPath, "Temp"));
                    string DHS = Path.Combine(TempFolder.FullName, $"{Path.GetFileNameWithoutExtension(HS)}.xml");
                    if (Util.RunExeInCmd("json2xmlb", $"-d \"{HS}\" \"{DHS}\""))
                    {
                        SplitXMLStats(DHS);
                        TempFolder.Delete();
                    }
                }
                else
                {
                    string[] LoadedHerostat = File.ReadLines(HS).ToArray();
                    char HsFormat = LoadedHerostat.First(s => !string.IsNullOrEmpty(s.Trim())).Trim()[0];
                    if (HsFormat == '<')
                    {
                        SplitXMLStats(HS);
                    }
                    else
                    {
                        SplitOtherStats(LoadedHerostat, HsFormat);
                    }
                }
            }
        }
        /// <summary>
        /// Split an XML file to the root's child elements and saves them to new xml files if they have the charactername attribute.
        /// </summary>
        private void SplitXMLStats(string XMLFilePath)
        {
            if (GUIXML.GetXmlElement(XMLFilePath) is XmlElement Characters)
            {
                List<string> MlL = [], CnL = [];
                DirectoryInfo HSFolder = Directory.CreateDirectory(GetOHSFolder(OutputFolder.Text));
                for (int i = 0; i < Characters.ChildNodes.Count; i++)
                {
                    if (Characters.ChildNodes[i] is XmlElement XE
                        && XE.GetAttribute("charactername") is string CN
                        && CN is not "" and not "defaultman")
                    {
                        XmlDocument Xdoc = new();
                        Xdoc.LoadXml(XE.OuterXml);
                        Xdoc.Save(Path.Combine(HSFolder.FullName, $"{CN}.xml"));
                        CnL.Add(CN); MlL.Add(XE.GetAttribute("menulocation"));
                    }
                }
                WriteCfgFiles(MlL, CnL);
                SplitFinished();
            }
        }
        /// <summary>
        /// Splits herostats as lines, based on depth count by curly brackets. Saves them to files if they have the charactername line.
        /// </summary>
        private void SplitOtherStats(string[] LoadedHerostat, char HsFormat)
        {
            int Depth = HsFormat == '{' ? -1 : 0;
            List<string> SplitStat = [], MlL = [], CnL = [];
            string CN = "", ML = "";
            string Ext = HsFormat == '{' ? ".json" : ".txt";
            DirectoryInfo HSFolder = Directory.CreateDirectory(GetOHSFolder(OutputFolder.Text));
            for (int i = 0; i < LoadedHerostat.Length; i++)
            {
                string Line = LoadedHerostat[i].Trim();
                if (Depth > 1 || (Depth == 1 && Line != ""))
                {
                    SplitStat.Add(LoadedHerostat[i]);
                }
                if (!string.IsNullOrEmpty(Line))
                {
                    if (Line[0] == '{' || Line[^1] == '{') Depth++;
                    if (Line[0] == '}' || Line.TrimEnd(',')[^1] == '}') Depth--;
                }
                if (CharNameRX().Match(Line) is Match M && M.Success)
                    CN = M.Value;
                if (MLRX().IsMatch(Line))
                    ML = Line.Split(HsFormat == '{' ? ':' : '=', 2)[1].TrimEnd(';').TrimEnd(',').Trim().Trim('"');
                if (i > 5 && Depth == 1)
                {
                    if (CN is not "" and not "defaultman")
                    {
                        File.WriteAllLines(Path.Combine(HSFolder.FullName, CN + Ext), SplitStat);
                        CnL.Add(CN); MlL.Add(ML);
                    }
                    SplitStat.Clear();
                    CN = "";
                }
            }
            WriteCfgFiles(MlL, CnL);
            SplitFinished();
        }
        private static void WriteCfgFiles(List<string> MlL, List<string> CnL)
        {
            File.WriteAllLines(Path.Combine(cdPath, GUIsettings.Instance.Game, "rosters", $"Roster-{DateTime.Now:yyMMdd-HHmmss}.cfg"), CnL);
            if (GUIsettings.Instance.Game == "xml2") { return; }
            File.WriteAllLines(Path.Combine(cdPath, GUIsettings.Instance.Game, "menulocations", $"Menulocations-{DateTime.Now:yyMMdd-HHmmss}.cfg"), MlL);
        }
        private async void SplitFinished()
        {
            Cfg.Dynamic.SE_Msg_Success = new MessageItem
            {
                Message = $"Split herostats to '{GetOHSFolder(OutputFolder.Text)}'",
                IsOpen = true
            };
            await Task.Delay(5000);
            Cfg.Dynamic.SE_Msg_Success = new MessageItem() { IsOpen = false };
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start("explorer.exe", $"/select, \"{GetOHSFolder(OutputFolder.Text)}\"");
        }
    }
}
