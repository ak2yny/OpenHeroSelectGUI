using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Xml;
using static OpenHeroSelectGUI.Settings.CfgCommands;
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
        private string? HerostatPath, InternalName;
        private string[]? LoadedHerostat;
        private static char HsFormat = ' ';
        private readonly StandardUICommand DeleteCommand = new(StandardUICommandKind.Delete);
        public Cfg Cfg { get; set; } = new();
        public SkinDetailsPage()
        {
            DeleteCommand.ExecuteRequested += DeleteCommand_ExecuteRequested;

            InitializeComponent();
            Skins.AllowDrop = Skins.CanReorderItems = Cfg.GUI.Game == "mua";
            LoadSkinList();
        }
        /// <summary>
        /// Load skin details from a herostat with a single stat only.
        /// </summary>
        private void LoadSkinList()
        {
            SkinInstaller.Visibility = Visibility.Collapsed;
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
                HsFormat = LoadedHerostat[0].Trim()[0];
                if (HsFormat == '<')
                {
                    XmlDocument XmlStat = new();
                    XmlStat.LoadXml(string.Join(Environment.NewLine, LoadedHerostat));
                    if (XmlStat.DocumentElement is XmlElement Stats && Stats.HasAttribute("skin"))
                    {
                        CharacterName.Text = Stats.GetAttribute("charactername");
                        InternalName = Stats.GetAttribute("name");
                        string CN = Stats.GetAttribute("skin")[..^2];
                        for (int i = 0; i < GetSkinIdentifiers().Length; i++)
                        {
                            if (Stats.HasAttribute(GetSkinIdentifiers()[i]))
                            {
                                string SkinName = Cfg.GUI.Game == "xml2"
                                    ? i == 0
                                        ? "Main"
                                        : GetSkinIdentifiers()[i][5..]
                                    : i == 0
                                        ? Stats.GetAttribute("skin_01_name")
                                        : Stats.GetAttribute($"{GetSkinIdentifiers()[i]}_name");
                                AddSkin(CN, Stats.GetAttribute(GetSkinIdentifiers()[i])[^2..], SkinName);
                            }
                        }
                    }
                }
                else
                {
                    CharacterName.Text = GetFakeXmlJsonAttr(LoadedHerostat, "charactername");
                    InternalName = GetFakeXmlJsonAttr(LoadedHerostat, "name");
                    string CN = GetFakeXmlJsonAttr(LoadedHerostat, "skin")[..^2];
                    for (int i = 0; i < GetSkinIdentifiers().Length; i++)
                    {
                        string Skn = GetFakeXmlJsonAttr(LoadedHerostat, GetSkinIdentifiers()[i]);
                        if (!string.IsNullOrEmpty(Skn))
                        {
                            string SkinName = Cfg.GUI.Game == "xml2"
                                ? i == 0
                                    ? "Main"
                                    : GetSkinIdentifiers()[i][5..]
                                : i == 0
                                    ? GetFakeXmlJsonAttr(LoadedHerostat, "skin_01_name")
                                    : GetFakeXmlJsonAttr(LoadedHerostat, $"{GetSkinIdentifiers()[i]}_name");
                            AddSkin(CN, Skn[^2..], SkinName);
                        }
                    }
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
            LoadedHerostat = File.ReadLines(HerostatPath).ToArray();
            bool IsMUA = Cfg.GUI.Game == "mua";
            if (HsFormat == '<')
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
        /// <summary>
        /// Calculate the mannequin number, based on the first skin.
        /// </summary>
        /// <returns>Number of the first skin if XML2 and > 9, else "01"</returns>
        private string GetMqNum() => Cfg.GUI.Game == "mua" || int.Parse(Cfg.Skins.SkinsList.First().Number) < 10 ? "01" : Cfg.Skins.SkinsList.First().Number;
        /// <summary>
        /// Check all skins in the SkinsList for packages and, if not found, clone a package if available. Shows message if not found and not available for cloning.
        /// </summary>
        private void CheckPackages()
        {
            bool MissingPkg = false;
            string[] PkgSourceFolders = { Cfg.GUI.GameInstallPath, Cfg.OHS.GameInstallPath };
            if (Directory.Exists(Cfg.OHS.GameInstallPath) && Directory.GetParent(Cfg.OHS.GameInstallPath) is DirectoryInfo MO2 && MO2.Name == "mods")
            {
                PkgSourceFolders = PkgSourceFolders.Concat(MO2.EnumerateDirectories().Select(d => d.FullName)).ToArray();
            }
            PkgSourceFolders = PkgSourceFolders.Select(f => Path.Combine(f, "packages", "generated", "characters")).ToArray();
            for (int i = 0; i < Cfg.Skins.SkinsList.Count; i++)
            {
                SkinDetails Skin = Cfg.Skins.SkinsList[i];

                if (PkgSourceFolders.Any(f => File.Exists(Path.Combine(f, $"{InternalName}_{Skin.CharNum}{Skin.Number}.pkgb"))))
                    continue;

                string? SourcePkg = PkgSourceFolders.Where(f => Directory.Exists(f))
                                                    .Select(f => Directory.EnumerateFiles(f, $"{InternalName}_{Skin.CharNum}??.pkgb").FirstOrDefault())
                                                    .FirstOrDefault(p => !string.IsNullOrEmpty(p));
                if (SourcePkg is null)
                {
                    MissingPkg = true;
                }
                else
                {
                    MarvelModsXML.ClonePackage(SourcePkg, InternalName, Skin.CharNum, Skin.Number);
                }
            }
            if (MissingPkg) ShowWarning("Missing packages. HUD head and powers might not show correctly.");
        }
        /// <summary>
        /// Show a warning dialogue box.
        /// </summary>
        private async void ShowWarning(string message)
        {
            WarningDialog.Content = message;
            await WarningDialog.ShowAsync();
        }
        /// <summary>
        /// Browse for an IGB file and install it to the target path and (name without extension). Then analyze the IGB files for certain properties (meant for skins).
        /// </summary>
        private async void InstallSkinFiles(string GamePath, string TargetName) => FixSkins(await LoadDialogue(".igb"), GamePath, TargetName);
        /// <summary>
        /// Apply Alchemy and other optimizations to make skins compatible if possible and show compatibility information.
        /// Provide game folders and file name without extension. Will look in GameInstallPath using .igb extension.
        /// </summary>
        private void FixSkins(string GamePath, string Name) => FixSkins(Path.Combine(Cfg.OHS.GameInstallPath, GamePath, $"{Name}.igb"), GamePath, Name);
        /// <summary>
        /// Apply Alchemy and other optimizations to make skins compatible if possible and show compatibility information.
        /// Provide full path for source and game folders and file name for target (saves to gameInstallPath).
        /// </summary>
        private void FixSkins(string? SourceIGB, string GamePath, string Name)
        {
            if (AlchemyRoot is null || SourceIGB is null) return;
            string IGB = Path.Combine(Directory.CreateDirectory(Path.Combine(Cfg.OHS.GameInstallPath, GamePath)).FullName, $"{Name}.igb");
            File.WriteAllText(Alchemy_ini, GetSkinInfo);
            string Stats = Util.RunDosCommnand(Path.Combine(AlchemyRoot, "bin", "sgOptimizer.exe"), $"\"{SourceIGB}\" \"{IGB}\" \"{Alchemy_ini}\"");
            if (string.IsNullOrEmpty(Stats)) return;

            string[] StatLines = Stats.Split(Environment.NewLine);
            string[] GeometryLines = StatLines.Where(s => s.Contains("igGeometryAttr")).Select(s => s.Split('|')[2].Trim()).ToArray();
            IEnumerable<string> TextureLines = StatLines.Where(s => s.Contains("IG_GFX_TEXTURE_FORMAT_"));
            int[] TexSizeProds = TextureLines.Select(s => int.Parse(s.Split('|')[1]) * int.Parse(s.Split('|')[2])).ToArray();
            string[] BiggestTex = TextureLines.ToArray()[Array.IndexOf(TexSizeProds, TexSizeProds.Max())].Split('|');

            SolidColorBrush Red = new(Colors.Red);
            SolidColorBrush Green = new(Colors.Green);
            int Plat = Cfg.GUI.Game == "mua"
                ? MUAplatforms.SelectedIndex
                : XML2platforms.SelectedIndex + 10;

            using FileStream fs = new(IGB, FileMode.Open);
            fs.Position = 0x2C;
            string AV;
            try { AV = string.Format("{0:X}", fs.ReadByte()); } catch { AV = "unknown"; }
            bool AVisN = int.TryParse(AV, out int AVN);
            AlchemyVersion.Text = AV == "4"
                ? "2.5"
                : AV == "6"
                ? "3.2"
                : AV == "8"
                ? "3.5"
                : AV == "9"
                ? "5"
                : "unknown";
            AlchemyVersion.Foreground = AlchemyVersionT.Foreground =
                (Plat > 9 || Plat is 1 or 6) && AVisN && AVN > 6
                ? Red
                : Green;

            FileInfo IGBFI = new(IGB);
            FileSize.Text = $"{IGBFI.Length} bytes";
            FileSize.Foreground = FileSizeT.Foreground =
                (Plat == 5 && IGBFI.Length > 600000)
                || ((Plat > 10 || Plat is 1 or 4 or 6) && IGBFI.Length > 300000)
                ? Red
                : Green;

            // Wii (Plat 5) is buggy when attr2 was added with Alchemy 5 and PSP doesn't seem to work. They support native (Alch 3.5) attr2 however.
            GeometryFormats.Text = string.Join(", ", GeometryLines.Distinct());
            GeometryFormats.Foreground = GeometryFormatsT.Foreground =
                ((Plat is 1 or 6 or 10 or 11 or 12 or 14) && GeometryFormats.Text.Contains('2'))
                || ((Plat is 4 or 13) && GeometryFormats.Text.Contains("1_5"))
                ? Red
                : Green;
            VertexCount.Text = StatLines.Where(s => s.Contains(" vertex total: ", StringComparison.OrdinalIgnoreCase)).Max(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries)[3]);
            GeometryCount.Text = GeometryLines.Length.ToString();

            TextureFormats.Text = string.Join(", ", TextureLines.Select(s => s.Split('|')[3].Trim()).Distinct()).Replace("IG_GFX_TEXTURE_FORMAT_", "");
            string IncompatibleTexs = Plat is 4 or 13
                ? "GAMECUBE DXT"
                : Plat is 1 or 12
                ? "PSP GAMECUBE DXT"
                : Plat is 2 or 3
                ? "PSP GAMECUBE _X_"
                : Plat is 5
                ? "PSP _X_"
                : Plat is 11
                ? "PSP"
                : "PSP GAMECUBE";
            TextureFormats.Foreground = TextureFormatsT.Foreground =
                IncompatibleTexs.Split().Any(i => TextureFormats.Text.Contains(i))
                ? Red
                : Green;
            BiggestTexture.Text = $"{BiggestTex[1].Trim()} x {BiggestTex[2].Trim()}";
            TextureCount.Text = TextureLines.Count().ToString();
            MipMaps.Text = TextureLines.Any(s => s.Split('|')[4].Contains("Mip", StringComparison.OrdinalIgnoreCase)).ToString();

            string? igSkinLine = StatLines.FirstOrDefault(s => new Regex(@"ig.*Matrix.*Select", RegexOptions.IgnoreCase).IsMatch(s));
            igSkinName.Text = string.IsNullOrEmpty(igSkinLine) ? "" : igSkinLine.Split('|')[0].Trim();
            igSkinName.Foreground = igSkinNameT.Foreground =
                (Plat is 1 or 6 or 10 or 11 or 12 or 14) && igSkinName.Text != Name
                ? Red
                : Green;

            int i = 0;
            List<string> Opt = new();
            if ((Plat is 2 or 3 or 7 or 8) && GeometryFormats.Text.Contains("1_5"))  // convert to attr2
            {
                i++;
                Opt.Add(AlchemyCGA(i));
            }
            if (Plat is 0 or 2 or 3 or 7 or 8)  // (there are issues sometimes if gc is already applied)
            {
                i++;
                Opt.Add(AlchemyGGC(i));
            }
            if ((Plat is not 1 or 6 or 10 or 11 or 12 or 14) && igSkinName.Text != Name)
            {
                i++;
                Opt.Add(AlchemyRen(i, igSkinName.Text, Name));
                igSkinName.Text = Name;
            }
            if (i > 0)
            {
                File.WriteAllLines(Alchemy_ini, Opt.Prepend(AlchemyHead(i)));
                _ = Util.RunDosCommnand(Path.Combine(AlchemyRoot, "bin", "sgOptimizer.exe"), $"\"{IGB}\" \"{IGB}\" \"{Alchemy_ini}\"");
            }

            SkinInfo.Visibility = Visibility.Visible;
        }
        private static int GetDiamondFormNumber(string SkinNumber)
        {
            int LN = int.Parse(SkinNumber[^1].ToString());
            return LN == 9
                ? int.Parse(SkinNumber) + 1
                : 5 > LN && LN > 0
                ? int.Parse(SkinNumber) + 4
                : 99;
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
            if (e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Mouse or Microsoft.UI.Input.PointerDeviceType.Pen && Cfg.Skins.SkinsList.Count > 1)
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
                    if (Cfg.GUI.Game == "xml2" && STR.Name == "Main") return;
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
            AddSkin(AddCharNum.Text, (MaxSkinNum + 1).ToString().PadLeft(2, '0'), Cfg.GUI.Game == "xml2" ? AvailableXMLNames[0] : "");
        }
        /// <summary>
        /// Restrict skin number input
        /// </summary>
        private void SkinNumber_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c)) || args.NewText.Length > 2;
        }
        /// <summary>
        /// Ensure two digit number
        /// </summary>
        private void SkinNumber_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (sender is TextBox SN)
            {
                SN.Text = SN.Text.PadLeft(2, '0');
            }
        }
        /// <summary>
        /// Save. There is no shortcut for now, as it conflicts with the OHS commands.
        /// </summary>
        private void SaveSkinDetails_Click(object sender, RoutedEventArgs e)
        {
            if (HerostatPath is not null && Cfg.Skins.SkinsList.Any())
            {
                SaveSkinList();
                CheckPackages();
            }
        }
        /// <summary>
        /// Load the installation commands when a skin is selected
        /// </summary>
        private void Skins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin)
            {
                XML2platforms.Visibility = ThreeDHeadBrowse.Visibility = Cfg.GUI.Game == "mua"
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                MUAplatforms.Visibility = Cfg.GUI.Game == "xml2"
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                SkinInfo.Visibility = Visibility.Collapsed;
                HudHeadBrowse.Visibility = SkinInstaller.Visibility = Visibility.Visible;
                InstallSkins.Text = $"Install skin {Skin.CharNum}{Skin.Number}";
                InstallMannequin.Text = $"Install select item {Skin.CharNum}{GetMqNum()}";
            }
        }

        private async void InstallSkins_Click(object sender, RoutedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin)
            {
                string? IGB = await LoadDialogue(".igb");
                if (IGB is null) return;
                string IGBpath = Path.GetDirectoryName(IGB)!;
                string IGBbase = Path.GetFileNameWithoutExtension(IGB);
                CopyGameFile(IGBpath, "actors", Path.GetFileName(IGB), $"{Skin.CharNum}{Skin.Number}.igb");
                if (File.Exists(Path.Combine(IGBpath, $"hud_head_{IGBbase}.igb")))
                {
                    CopyGameFile(IGBpath, "hud", $"hud_head_{IGBbase}.igb", $"hud_head_{Skin.CharNum}{Skin.Number}.igb");
                    HudHeadBrowse.Visibility = Visibility.Collapsed;
                }
                if (Cfg.GUI.Game == "xml2" && File.Exists(Path.Combine(IGBpath, $"{IGBbase} (3D Head).igb")))
                {
                    CopyGameFile(IGBpath, Path.Combine("ui", "hud", "characters"), $"{IGBbase} (3D Head).igb", $"{Skin.CharNum}{Skin.Number}.igb");
                    ThreeDHeadBrowse.Visibility = Visibility.Collapsed;
                }
                FixSkins(Path.Combine(Cfg.OHS.GameInstallPath, "actors"), $"{Skin.CharNum}{Skin.Number}");
            }
        }

        private void InstallHuds_Click(object sender, RoutedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin)
            {
                InstallSkinFiles("hud", $"hud_head_{Skin.CharNum}{Skin.Number}");
            }
        }

        private void Install3DHead_Click(object sender, RoutedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin)
            {
                InstallSkinFiles(Path.Combine("ui", "hud", "characters"), $"{Skin.CharNum}{Skin.Number}");
            }
        }

        private void InstallMannequin_Click(object sender, RoutedEventArgs e)
        {
            SkinDetails Skin = Cfg.Skins.SkinsList.First();
            string MqFolder = Cfg.GUI.Game == "mua"
                ? Cfg.MUA.MannequinFolder
                : "characters";
            InstallSkinFiles(Path.Combine("ui", "models", MqFolder), $"{Skin.CharNum}{GetMqNum()}");
        }

        private void DiamondForm_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ToolTip Tt && Skins.SelectedItem is SkinDetails Skin)
            {
                Tt.Content = $"Browse for Emma Frost's Diamond Form skin to install as {Skin.CharNum}{GetDiamondFormNumber(Skin.Number)}.igb - Works for other characters that use this system";
            }
        }

        private void FlameOn_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ToolTip Tt && Skins.SelectedItem is SkinDetails Skin)
            {
                Tt.Content = $"Browse for Human Torch's Flame On skin to install as {Skin.CharNum}{int.Parse(Skin.Number) + 10}.igb - Works for other characters that use this system";
            }
        }

        private void InstallEmmaSkin_Click(object sender, RoutedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin)
            {
                InstallSkinFiles("actors", $"{Skin.CharNum}{GetDiamondFormNumber(Skin.Number)}");
            }
        }

        private void InstallHumanTorch_Click(object sender, RoutedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin)
            {
                InstallSkinFiles("actors", $"{Skin.CharNum}{int.Parse(Skin.Number) + 10}");
            }
        }

        private async void InstallExtraSkin_Click(object sender, RoutedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin)
            {
                await EnterSkinNumber.ShowAsync();
                InstallSkinFiles("actors", $"{Skin.CharNum}{ExtraSkinNumber.Text}");
            }
        }
    }
}