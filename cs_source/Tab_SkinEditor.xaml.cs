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
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The Skin details tab that uses the skin details pane (page) to show skin details.
    /// </summary>
    public sealed partial class Tab_SkinEditor : Page
    {
        public Tab_SkinEditor()
        {
            InitializeComponent();
            _ = AvailableCharacters.Navigate(typeof(AvailableCharacters));
            _ = SkinDetailsPage.Navigate(typeof(SkinDetailsPage));
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
            string? HS = await LoadDialogue("*");
            if (HS != null)
            {
                Path.GetFileName(HS);
                if (RavenFormats.Contains(Path.GetExtension(HS)))
                {
                    DirectoryInfo TempFolder = Directory.CreateDirectory(Path.Combine(cdPath, "Temp"));
                    string DHS = Path.Combine(TempFolder.FullName, $"{Path.GetFileNameWithoutExtension(HS)}.xml");
                    _ = Util.RunDosCommnand("json2xmlb", $"-d \"{HS}\" \"{DHS}\"");
                    SplitXMLStats(DHS);
                    TempFolder.Delete();
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
                List<string> MlL = new(), CnL = new();
                DirectoryInfo HSFolder = Directory.CreateDirectory(CharacterListCommands.GetOHSFolder(OutputFolder.Text));
                for (int i = 0; i < Characters.ChildNodes.Count; i++)
                {
                    string CN = ((XmlElement)Characters.ChildNodes[i]).GetAttribute("charactername");
                    string ML = ((XmlElement)Characters.ChildNodes[i]).GetAttribute("menulocation");
                    if (!string.IsNullOrEmpty(CN) && CN != "defaultman")
                    {
                        XmlDocument Xdoc = new();
                        Xdoc.LoadXml(Characters.ChildNodes[i].OuterXml);
                        Xdoc.Save(Path.Combine(HSFolder.FullName, $"{CN}.xml"));
                        CnL.Add(CN); MlL.Add(ML);
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
            List<string> SplitStat = new(), MlL = new(), CnL = new();
            Regex RXisChar = new(@"^""?charactername(\"": | = )"), RXisMl = new(@"^""?menulocation(\"": | = )");
            string CN = "", ML = "";
            string Ext = HsFormat == '{' ? ".json" : ".txt";
            DirectoryInfo HSFolder = Directory.CreateDirectory(CharacterListCommands.GetOHSFolder(OutputFolder.Text));
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
                if (RXisChar.IsMatch(Line))
                    CN = LoadedHerostat[i].Split(new[] { HsFormat == '{' ? ':' : '=' }, 2)[1].TrimEnd(';').TrimEnd(',').Trim().Trim('"');
                if (RXisMl.IsMatch(Line))
                    ML = LoadedHerostat[i].Split(new[] { HsFormat == '{' ? ':' : '=' }, 2)[1].TrimEnd(';').TrimEnd(',').Trim().Trim('"');
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
            File.WriteAllLines(Path.Combine(cdPath, GUIsettings.Instance.Game, "menulocations", $"Menulocations-{DateTime.Now:yyMMdd-HHmmss}.cfg"), MlL);
        }
        private async void SplitFinished()
        {
            SuccessMessage.Text = "Successfully Split";
            await Task.Delay(TimeSpan.FromSeconds(1.5));
            SuccessMessage.Text = "";
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start("explorer.exe", $"/select, \"{CharacterListCommands.GetOHSFolder(OutputFolder.Text)}\"");
        }
    }
}
