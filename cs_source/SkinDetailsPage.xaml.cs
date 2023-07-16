using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Xml;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The Skin details pane that shows skin details. Depends on Floating character and can be added to other pages.
    /// </summary>
    public sealed partial class SkinDetailsPage : Page
    {
        // WIP: using these varaibles, changing the page will reset the skin edits and the user will have to read from the herostat again. Is this the desired behaviour?
        private string? HerostatPath;
        private string[]? LoadedHerostat;
        private static char HsFormat = ' ';
        private readonly StandardUICommand DeleteCommand = new(StandardUICommandKind.Delete);
        public Cfg Cfg { get; set; } = new();
        public SkinDetailsPage()
        {
            DeleteCommand.ExecuteRequested += DeleteCommand_ExecuteRequested;

            InitializeComponent();
            Skins.AllowDrop = Skins.CanReorderItems = Cfg.Dynamic.Game == "mua";
            LoadSkinList();
        }
        /// <summary>
        /// Load skin details from a herostat with a single stat only.
        /// </summary>
        private void LoadSkinList()
        {
            if (Cfg.Dynamic.FloatingCharacter is string FC)
            {
                Cfg.Skins.SkinsList.Clear();
                string FolderString = Path.Combine(GetHerostatFolder(), FC);
                int S = FolderString.Replace('\\', '/').LastIndexOf('/');
                DirectoryInfo folder = new(FolderString[..S].TrimEnd('/'));
                if (!folder.Exists) return;
                IEnumerable<FileInfo> Herostat = folder.EnumerateFiles($"{FolderString[(S + 1)..]}.??????????");
                if (!Herostat.Any()) return;
                LoadedHerostat = File.ReadLines(Herostat.First().FullName).Where(l => l.Trim() != "").ToArray();
                AddCharNum.Text = "";
                HerostatPath = Herostat.First().FullName;
                if (LoadedHerostat[0].Trim()[0] == '<')
                {
                    XmlDocument XmlStat = new();
                    XmlStat.LoadXml(string.Join(Environment.NewLine, LoadedHerostat));
                    if (XmlStat.DocumentElement is XmlElement Stats && Stats.HasAttribute("skin"))
                    {
                        CharacterName.Text = Stats.GetAttribute("charactername");
                        string CN = Stats.GetAttribute("skin")[..^2];
                        for (int i = 0; i < GetSkinIdentifiers().Length; i++)
                        {
                            if (Stats.HasAttribute(GetSkinIdentifiers()[i]))
                            {
                                string SkinName = Cfg.Dynamic.Game == "mua"
                                    ? i == 0
                                        ? Stats.GetAttribute("skin_01_name")
                                        : Stats.GetAttribute($"{GetSkinIdentifiers()[i]}_name")
                                    : i == 0
                                        ? "Main"
                                        : GetSkinIdentifiers()[i][5..];
                                AddSkin(CN, Stats.GetAttribute(GetSkinIdentifiers()[i])[^2..], SkinName);
                            }
                        }
                        HsFormat = 'x';
                    }

                }
                else
                {
                    CharacterName.Text = GetFakeXmlJsonAttr(LoadedHerostat, "charactername");
                    string CN = GetFakeXmlJsonAttr(LoadedHerostat, "skin")[..^2];
                    for (int i = 0; i < GetSkinIdentifiers().Length; i++)
                    {
                        string Skn = GetFakeXmlJsonAttr(LoadedHerostat, GetSkinIdentifiers()[i]);
                        if (!string.IsNullOrEmpty(Skn))
                        {
                            string SkinName = Cfg.Dynamic.Game == "mua"
                                ? i == 0
                                    ? GetFakeXmlJsonAttr(LoadedHerostat, "skin_01_name")
                                    : GetFakeXmlJsonAttr(LoadedHerostat, $"{GetSkinIdentifiers()[i]}_name")
                                : i == 0
                                    ? "Main"
                                    : GetSkinIdentifiers()[i][5..];
                            AddSkin(CN, Skn[^2..], SkinName);
                        }
                    }
                    // Note: The JSON herostat can also be split into a simple JSON and be parsed (as below) but the code is just too much to add and regex is just as good IMHO.
                    //else if (HSlines[0].Trim() == "\"stats\": {")
                    //{
                    //    int Indx = Array.IndexOf(HSlines, HSlines[1..].FirstOrDefault(l => l.Contains('{')));
                    //    string[] TopHsLines = HSlines[..(Indx == -1 ? ^1 : Indx)];
                    //    TopHsLines[^1] = TopHsLines[^1].TrimEnd(',');
                    //    string MainJsonHS = $"{{ {string.Join(' ', TopHsLines[1..])} }}";
                    //}
                }
            }
        }
        /// <summary>
        /// Add Skin to the display list
        /// </summary>
        private void AddSkin(string CharacterNumber, string Number, string SkinName)
        {
            AddCharNum.Text = CharacterNumber;
            Cfg.Skins.SkinsList.Add(new SkinDetails
            {
                CharNum = CharacterNumber,
                Number = Number,
                Name = SkinName,
                Command = DeleteCommand
            });
            AddButton.Visibility = Cfg.Skins.SkinsList.Count < GetSkinIdentifiers().Length
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        /// <summary>
        /// Search a fake XML or JSON file for an attribute. Ignores parent/child relations. Treats nodes as attributes but returns empty.
        /// </summary>
        /// <param name="array">An array of strings (lines), read from a fake XML or JSON file</param>
        /// <param name="name">The name of the attribute</param>
        /// <returns>The value of the first matching attribute</returns>
        private static string GetFakeXmlJsonAttr(string[] array, string name)
        {
            Regex RXstartsWith = new($"^\"?{name}(\": | =)");
            string[] Lines = Array.FindAll(array, c => RXstartsWith.IsMatch(c.Trim().ToLower()));
            if (Lines.Any())
            {
                HsFormat = Lines[0].Trim()[0] == '"' ? ':' : '=';
                return Lines[0].Split(new[] { HsFormat }, 2)[1].TrimEnd(';').TrimEnd(',').Trim().Trim('"');
            }
            return string.Empty;
        }
        /// <summary>
        /// On save, add the new details to the source herostat.
        /// </summary>
        private void SaveSkinList()
        {
            if (HerostatPath is not null && Cfg.Skins.SkinsList.Any())
            {
                LoadedHerostat = File.ReadLines(HerostatPath).ToArray();
                bool IsMUA = Cfg.Dynamic.Game == "mua";
                if (HsFormat == 'x')
                {
                    XmlDocument XmlStat = new();
                    XmlStat.LoadXml(string.Join(Environment.NewLine, LoadedHerostat));
                    if (XmlStat.DocumentElement is XmlElement Stats && Stats.GetAttributeNode("skin") is XmlAttribute Skin)
                    {
                        Skin.Value = $"{Cfg.Skins.SkinsList[0].CharNum}{Cfg.Skins.SkinsList[0].Number}";
                        if (IsMUA) Stats.GetAttributeNode("skin_01_name").Value = Cfg.Skins.SkinsList[0].Name;
                        for (int i = 1; i < GetSkinIdentifiers().Length; i++)
                        {
                            Stats.RemoveAttribute(GetSkinIdentifiers()[i]);
                            if (IsMUA) Stats.RemoveAttribute($"{GetSkinIdentifiers()[i]}_name");
                        }
                        for (int i = 1; i < Cfg.Skins.SkinsList.Count; i++)
                        {
                            string AttrName = IsMUA
                                ? $"skin_0{i + 1}"
                                : Cfg.Skins.SkinsList[i].Name == "Main"
                                ? ""
                                : $"skin_{Cfg.Skins.SkinsList[i].Name}";
                            XmlAttribute Attr = XmlStat.CreateAttribute(AttrName);
                            Attr.Value = $"{Cfg.Skins.SkinsList[i].Number}";
                            _ = Stats.SetAttributeNode(Attr);
                            if (IsMUA)
                            {
                                XmlAttribute AttrNm = XmlStat.CreateAttribute($"skin_0{i + 1}_name");
                                AttrNm.Value = Cfg.Skins.SkinsList[i].Name;
                                _ = Stats.SetAttributeNode(AttrNm);
                            }
                        }
                        XmlStat.Save(HerostatPath);
                    }
                }
                else
                {
                    int Indx = Array.IndexOf(LoadedHerostat, LoadedHerostat.FirstOrDefault(l => l.Trim().Trim('"').ToLower().StartsWith("skin")));
                    if (Indx > -1)
                    {
                        // A skin should always be present, but in case it isn't and a child element with a skin attribut exists, the result will be corrupted, but the herostat is already corrupted in that case
                        string indent = new Regex(@"\S").Split(LoadedHerostat[Indx])[0];
                        int M = IsMUA ? 2 : 1;
                        for (int i = 0; i < GetSkinIdentifiers().Length * M; i++)
                        {
                            if (i < Cfg.Skins.SkinsList.Count * M)
                            {
                                string NewLine = !IsMUA || i % 2 == 0
                                    ? i == 0
                                        ? HsFormat == ':'
                                            ? $"\"skin\": \"{Cfg.Skins.SkinsList[i].CharNum}{Cfg.Skins.SkinsList[i].Number}\","
                                            : $"skin = {Cfg.Skins.SkinsList[i].CharNum}{Cfg.Skins.SkinsList[i].Number} ;"
                                        : HsFormat == ':'
                                            ? IsMUA
                                                ? $"\"skin_0{i / 2 + 1}\": \"{Cfg.Skins.SkinsList[i / 2].Number}\","
                                                : $"\"skin_{Cfg.Skins.SkinsList[i].Name}\": \"{Cfg.Skins.SkinsList[i].Number}\","
                                            : IsMUA
                                                ? $"skin_0{i / 2 + 1} = {Cfg.Skins.SkinsList[i / 2].Number} ;"
                                                : $"skin_{Cfg.Skins.SkinsList[i].Name} = {Cfg.Skins.SkinsList[i].Number} ;"
                                    : HsFormat == ':'
                                        ? $"\"skin_0{(i - 1) / 2 + 1}_name\": \"{Cfg.Skins.SkinsList[(i - 1) / 2].Name}\","
                                        : $"skin_0{(i - 1) / 2 + 1}_name = {Cfg.Skins.SkinsList[(i - 1) / 2].Name} ;";
                                if (LoadedHerostat[Indx + i].Trim().Trim('"').ToLower().StartsWith("skin"))
                                {
                                    LoadedHerostat[Indx + i] = indent + NewLine;
                                }
                                else
                                {
                                    List<string> TempLines = LoadedHerostat.ToList();
                                    TempLines.Insert(Indx + i, indent + NewLine);
                                    LoadedHerostat = TempLines.ToArray();
                                }
                            }
                            else
                            {
                                LoadedHerostat = LoadedHerostat.Where((vl, ix) => !vl.Trim().Trim('"').ToLower().StartsWith("skin") || ix != Indx + i).ToArray();
                                Indx--;
                            }
                        }
                    }
                    File.WriteAllLines(HerostatPath, LoadedHerostat);
                }
            }
        }

        private void LoadHerostatEngb()
        {
            // Once Raven-Formats can convert (using BW's converter and my new JSON to XML), we use json2xmlb to get XML data
            // Then, we use XML document and XML element to list all characters (all stats, attribute charactername)
        }
        /// <summary>
        /// Load the skin details when a character is selected
        /// </summary>
        private void FloatingCharacter_Changed(object sender, TextChangedEventArgs e) => LoadSkinList();
        /// <summary>
        /// Show the delete button on hover
        /// </summary>
        private void SkinTemplate_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Mouse or Microsoft.UI.Input.PointerDeviceType.Pen)
            {
                VisualStateManager.GoToState(sender as Control, "HoverButtonsShown", true);
            }
        }
        private void SkinTemplate_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(sender as Control, "HoverButtonsHidden", true);
        }
        /// <summary>
        /// Remove Skin Slot
        /// </summary>
        private void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter != null)
            {
                if (Cfg.Skins.SkinsList.FirstOrDefault(s => s.Number == (args.Parameter as string)) is SkinDetails STR)
                {
                    _ = Cfg.Skins.SkinsList.Remove(STR);
                    AddButton.Visibility = Cfg.Skins.SkinsList.Count < GetSkinIdentifiers().Length
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
        }
        /// <summary>
        /// Add Skin Slot
        /// </summary>
        private void AddSkinSlot_Click(object sender, RoutedEventArgs e)
        {
            int MaxSkinNum = 0;
            string[] AvailableXMLNames = GetSkinIdentifiers().Select(s => s == "skin" ? "Main" : s[5..]).ToArray();
            if (Cfg.Skins.SkinsList.Any())
            {
                AvailableXMLNames = AvailableXMLNames.Except(Cfg.Skins.SkinsList.Select(s => s.Name)).ToArray();
                _ = int.TryParse(Cfg.Skins.SkinsList.OrderBy(s => s.Number).Last().Number, out MaxSkinNum);
            }
            AddSkin(AddCharNum.Text, (MaxSkinNum + 1).ToString().PadLeft(2, '0'), Cfg.Dynamic.Game == "xml2" ? AvailableXMLNames[0] : "");
        }
        /// <summary>
        /// Save. There is no shortcut for now, as it conflicts with the OHS commands.
        /// </summary>
        private void SaveSkinDetails_Click(object sender, RoutedEventArgs e) => SaveSkinList();

        private void SkinNumber_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c)) || args.NewText.Length > 2;
        }
    }
}
