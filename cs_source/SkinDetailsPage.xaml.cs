using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml;
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The Skin details pane that shows skin details. Depends on Floating character and can be added to other pages.
    /// </summary>
    public sealed partial class SkinDetailsPage : Page
    {
        private string? InternalName;
        private readonly StandardUICommand DeleteCommand = new(StandardUICommandKind.Delete);
        public Cfg Cfg { get; set; } = new();

        public SkinDetailsPage()
        {
            DeleteCommand.ExecuteRequested += DeleteCommand_ExecuteRequested;

            InitializeComponent();
            Skins.AllowDrop = Skins.CanReorderItems = Cfg.GUI.Game != "xml2";
            LoadSkinList();
            // Note: Skins are always loaded from file when navigated (except if nothing selected) - intended, to read external changes
        }
        /// <summary>
        /// Load skin details from a herostat with a single stat only.
        /// </summary>
        private void LoadSkinList()
        {
            SkinInstaller.Visibility = Visibility.Collapsed;
            Cfg.Roster.SkinsList.Clear();
            AddCharNum.Text = "";
            if (Herostat.Load(Cfg.Var.FloatingCharacter) is string[] LoadedHerostat)
            {
                if (Cfg.Var.HsFormat == '<')
                {
                    if (GUIXML.ElementFromString(string.Join(Environment.NewLine, LoadedHerostat)) is XmlElement Stats && Stats.HasAttribute("skin"))
                    {
                        CharacterName.Text = Stats.GetAttribute("charactername");
                        InternalName = Stats.GetAttribute("name");
                        string CN = Stats.GetAttribute("skin")[..^2];
                        for (int i = 0; i < SkinIdentifiers.Length; i++)
                        {
                            if (Stats.HasAttribute(SkinIdentifiers[i]))
                            {
                                string SkinName = Cfg.GUI.Game == "xml2"
                                    ? i == 0
                                        ? "Main"
                                        : SkinIdentifiers[i][5..]
                                    : i == 0
                                        ? Stats.GetAttribute("skin_01_name")
                                        : Stats.GetAttribute($"{SkinIdentifiers[i]}_name");
                                AddSkin(CN, Stats.GetAttribute(SkinIdentifiers[i])[^2..], SkinName);
                            }
                        }
                    }
                }
                else
                {
                    CharacterName.Text = Formats.GetAttr(LoadedHerostat, "charactername");
                    InternalName = Formats.GetAttr(LoadedHerostat, "name");
                    string CN = Formats.GetAttr(LoadedHerostat, "skin")[..^2];
                    for (int i = 0; i < SkinIdentifiers.Length; i++)
                    {
                        string Skn = Formats.GetAttr(LoadedHerostat, SkinIdentifiers[i]);
                        if (!string.IsNullOrEmpty(Skn))
                        {
                            string SkinName = Cfg.GUI.Game == "xml2"
                                ? i == 0
                                    ? "Main"
                                    : XML2Skins[i - 1]
                                : i == 0
                                    ? Formats.GetAttr(LoadedHerostat, "skin_01_name")
                                    : Formats.GetAttr(LoadedHerostat, $"{SkinIdentifiers[i]}_name");
                            AddSkin(CN, Skn[^2..], SkinName);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Add Skin "<paramref name="CharacterNumber"/><paramref name="Number"/> - <paramref name="SkinName"/>" to the display list
        /// </summary>
        private void AddSkin(string CharacterNumber, string Number, string SkinName)
        {
            AddCharNum.Text = CharacterNumber;
            Cfg.Roster.SkinsList.Add(new SkinDetails
            {
                CharNum = CharacterNumber,
                Number = Number,
                Name = SkinName,
                Command = DeleteCommand
            });
            AddButton.Visibility = Cfg.Roster.SkinsList.Count < SkinIdentifiers.Length
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        /// <summary>
        /// On save, add the new details to the source herostat.
        /// </summary>
        /// <returns><see langword="True"/>, if written successfully, otherwise <see langword="false"/>.</returns>
        private bool SaveSkinList()
        {
            if (Cfg.Var.HsPath is FileInfo HP)
            {
                string[] LoadedHerostat = File.ReadLines(HP.FullName).ToArray();

                bool IsMUA = Cfg.GUI.Game != "xml2";
                if (Cfg.Var.HsFormat == '<')
                {
                    XmlDocument XmlStat = new();
                    XmlStat.LoadXml(string.Join(Environment.NewLine, LoadedHerostat));
                    if (XmlStat.DocumentElement is XmlElement Stats && Stats.GetAttributeNode("skin") is XmlAttribute Skin)
                    {
                        Skin.Value = $"{Cfg.Roster.SkinsList[0].CharNum}{Cfg.Roster.SkinsList[0].Number}";
                        if (IsMUA && Stats.GetAttributeNode("skin_01_name") is XmlAttribute SknNm) { SknNm.Value = Cfg.Roster.SkinsList[0].Name; }
                        for (int i = 1; i < SkinIdentifiers.Length; i++)
                        {
                            Stats.RemoveAttribute(SkinIdentifiers[i]);
                            if (IsMUA) Stats.RemoveAttribute($"{SkinIdentifiers[i]}_name");
                        }
                        for (int i = 1; i < Cfg.Roster.SkinsList.Count; i++)
                        {
                            string AttrName = IsMUA
                                ? $"skin_0{i + 1}"
                                : Cfg.Roster.SkinsList[i].Name == "Main"
                                ? ""
                                : $"skin_{Cfg.Roster.SkinsList[i].Name}";
                            XmlAttribute Attr = XmlStat.CreateAttribute(AttrName);
                            Attr.Value = $"{Cfg.Roster.SkinsList[i].Number}";
                            _ = Stats.SetAttributeNode(Attr);
                            if (IsMUA)
                            {
                                XmlAttribute AttrNm = XmlStat.CreateAttribute($"skin_0{i + 1}_name");
                                AttrNm.Value = Cfg.Roster.SkinsList[i].Name;
                                _ = Stats.SetAttributeNode(AttrNm);
                            }
                        }
                        try { XmlStat.Save(HP.FullName); return true; } catch { return false; }
                    }
                }
                else
                {
                    int Indx = Array.IndexOf(LoadedHerostat, LoadedHerostat.FirstOrDefault(l => l.Trim().Trim('"').StartsWith("skin", StringComparison.OrdinalIgnoreCase)));
                    if (Indx > -1)
                    {
                        // A skin should always be present, but in case it isn't and a child element with a skin attribut exists, the result will be corrupted, but the herostat is already corrupted in that case
                        string indent = LoadedHerostat[Indx][..^LoadedHerostat[Indx].TrimStart().Length];
                        int M = IsMUA ? 2 : 1;
                        for (int i = 0; i < SkinIdentifiers.Length * M; i++)
                        {
                            if (i < Cfg.Roster.SkinsList.Count * M)
                            {
                                string NewLine = !IsMUA || i % 2 == 0
                                    ? i == 0
                                        ? Cfg.Var.HsFormat == ':'
                                            ? $"\"skin\": \"{Cfg.Roster.SkinsList[i].CharNum}{Cfg.Roster.SkinsList[i].Number}\","
                                            : $"skin = {Cfg.Roster.SkinsList[i].CharNum}{Cfg.Roster.SkinsList[i].Number} ;"
                                        : Cfg.Var.HsFormat == ':'
                                            ? IsMUA
                                                ? $"\"skin_0{i / 2 + 1}\": \"{Cfg.Roster.SkinsList[i / 2].Number}\","
                                                : $"\"skin_{Cfg.Roster.SkinsList[i].Name}\": \"{Cfg.Roster.SkinsList[i].Number}\","
                                            : IsMUA
                                                ? $"skin_0{i / 2 + 1} = {Cfg.Roster.SkinsList[i / 2].Number} ;"
                                                : $"skin_{Cfg.Roster.SkinsList[i].Name} = {Cfg.Roster.SkinsList[i].Number} ;"
                                    : Cfg.Var.HsFormat == ':'
                                        ? $"\"skin_0{(i - 1) / 2 + 1}_name\": \"{Cfg.Roster.SkinsList[(i - 1) / 2].Name}\","
                                        : $"skin_0{(i - 1) / 2 + 1}_name = {Cfg.Roster.SkinsList[(i - 1) / 2].Name} ;";
                                if (LoadedHerostat[Indx + i].Trim().Trim('"').StartsWith("skin", StringComparison.OrdinalIgnoreCase))
                                {
                                    LoadedHerostat[Indx + i] = indent + NewLine;
                                }
                                else
                                {
                                    List<string> TempLines = [.. LoadedHerostat];
                                    TempLines.Insert(Indx + i, indent + NewLine);
                                    LoadedHerostat = [.. TempLines];
                                }
                            }
                            else
                            {
                                LoadedHerostat = LoadedHerostat.Where((vl, ix) => !vl.Trim().Trim('"').StartsWith("skin", StringComparison.OrdinalIgnoreCase) || ix != Indx + i).ToArray();
                                Indx--;
                            }
                        }
                    }
                    try { File.WriteAllLines(HP.FullName, LoadedHerostat); return true; } catch { return false; }
                }
            }
            return false;
        }
        /// <summary>
        /// Calculate the mannequin number, based on the first skin.
        /// </summary>
        /// <returns>Number as <see cref="string"/> of the first skin if XML2 and bigger than 10, otherwise "01".</returns>
        private string MqNum => Cfg.GUI.Game == "xml2" && int.Parse(Cfg.Roster.SkinsList.First().Number) > 10 ? Cfg.Roster.SkinsList.First().Number : "01";
        /// <summary>
        /// Check all skins in the SkinsList for packages and, if not found, clone a package if available. Shows message if not found and not available for cloning.
        /// </summary>
        private void CheckPackages()
        {
            bool AllFound = true;
            string[] PkgSourceFolders = OHSpath.GetFoldersWpkg();
            for (int i = 0; i < Cfg.Roster.SkinsList.Count; i++)
            {
                AllFound = MarvelModsXML.ClonePackage(PkgSourceFolders, Cfg.Roster.SkinsList[i], InternalName) && AllFound;
            }
            Cfg.Var.SE_Msg_WarnPkg = !AllFound;
        }
        /// <summary>
        /// Browse for an IGB file and install it to <paramref name="GamePath"/> in the game/mod files, renaming it to the selected <see cref="Skins"/> number (plus number <paramref name="N"/> if given and optional <paramref name="Prefix"/>).
        /// </summary>
        private async void InstallIGB(string GamePath, string N = "", string Prefix = "")
        {
            if (Skins.SelectedItem is SkinDetails Skin && await CfgCmd.LoadDialogue(".igb") is string IGB && IGB != "")
            {
                FixSkins(IGB, GamePath, $"{Prefix}{Skin.CharNum}{(N == "" ? Skin.Number : N)}");
            }
        }
        /// <summary>
        /// Apply Alchemy optimizations to make the <paramref name="SourceIGB"/> compatible if possible and show compatibility information. Performs a crash if target directory can't be written to.
        /// Copy <paramref name="SourceIGB"/> to [gameInstallPath]/<paramref name="GamePath"/>/<paramref name="Name"/>.igb.
        /// </summary>
        private void FixSkins(string SourceIGB, string GamePath, string Name)
        {
            // Platforms:
            // 0 = MUA PC 2006
            // 1 = MUA PS2
            // 2 = MUA PS3
            // 3 = MUA PS4
            // 4 = MUA PSP
            // 5 = MUA Wii
            // 6 = MUA Xbox
            // 7 = MUA Xbox 360
            // 8 = MUA Xbox One, Steam
            // 10 = XML2 PC
            // 11 = XML2 Gamecube
            // 12 = XML2 PS2
            // 13 = XML2 PSP
            // 14 = XML2 Xbox
            int Plat = Cfg.GUI.Game == "xml2"
                ? XML2platforms.SelectedIndex + 10
                : MUAplatforms.SelectedIndex;
            int PAV = Plat is 0 or 2 or 3 or 7 or 8
                ? 9
                : Plat is 4 or 5 or 13
                ? 8
                : 6;

            SolidColorBrush Red = new(Colors.Red);
            SolidColorBrush Green = new(Colors.Green);

            GeometryFormats.Text = "";
            string? igSkin = null;
            if (Alchemy.GetSkinStats(SourceIGB) is string Stats)
            {
                string[] StatLines = Stats.Split(Environment.NewLine);
                IEnumerable<string> GeometryLines = StatLines.Where(s => s.Contains("igGeometryAttr")).Select(s => s.Split('|')[2].Trim()).ToArray();
                IEnumerable<string> TextureLines = StatLines.Where(s => s.Contains("IG_GFX_TEXTURE_FORMAT_"));
                int[] TexSizeProds = TextureLines.Select(s => int.Parse(s.Split('|')[1]) * int.Parse(s.Split('|')[2])).ToArray();
                string[] BiggestTex = TextureLines.ToArray()[Array.IndexOf(TexSizeProds, TexSizeProds.Max())].Split('|');

                int AV;
                using FileStream fs = new(SourceIGB, FileMode.Open, FileAccess.Read);
                fs.Position = 0x2C;
                try { AV = fs.ReadByte(); } catch { AV = 0; }

                AlchemyVersion.Text = AV == 4
                    ? "2.5"
                    : AV == 6
                    ? "3.2"
                    : AV == 8
                    ? "3.5"
                    : AV == 9
                    ? "5"
                    : "unknown";
                AlchemyVersion.Foreground = AlchemyVersionT.Foreground =
                    AV > PAV
                    ? Red
                    : Green;

                FileSize.Text = $"{fs.Length} bytes";
                FileSize.Foreground = FileSizeT.Foreground =
                    (Plat == 5 && fs.Length > 600000)
                    || ((Plat > 10 || Plat is 1 or 4 or 6) && fs.Length > 300000)
                    ? Red
                    : Green;

                // Wii (Plat 5) is buggy when attr2 was added with Alchemy 5 and XML2 PSP doesn't support any Alchemy 5. They require native (Alch 3.5) attr2 however.
                GeometryFormats.Text = string.Join(", ", GeometryLines.Distinct());
                GeometryFormats.Foreground = GeometryFormatsT.Foreground =
                    (PAV == 6 && GeometryFormats.Text.Contains('2'))
                    || (PAV > 6 && Plat != 0 && GeometryFormats.Text.Contains("1_5"))
                    ? Red
                    : Green;
                VertexCount.Text = StatLines.Where(s => s.Contains(" vertex total: ", StringComparison.OrdinalIgnoreCase)).Max(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries)[3]);
                GeometryCount.Text = GeometryLines.Count().ToString();

                TextureFormats.Text = string.Join(", ", TextureLines.Select(s => s.Split('|')[3].Trim()).Distinct()).Replace("IG_GFX_TEXTURE_FORMAT_", "");
                string[] IncompatibleTexs = Plat is 0 or 10
                    ? ["PSP", "GAMECUBE"]
                    : Plat is 1 or 12
                    ? ["PSP", "GAMECUBE", "DXT"]
                    : Plat is 4 or 13
                    ? ["GAMECUBE", "DXT"]
                    : Plat is 5
                    ? ["PSP", "_X_"]
                    : Plat is 11
                    ? ["PSP"]
                    : Plat is 8
                    ? ["PSP", "GAMECUBE", "888", "_X_"]
                    : ["PSP", "GAMECUBE", "_X_"];
                TextureFormats.Foreground = TextureFormatsT.Foreground =
                    IncompatibleTexs.Any(i => TextureFormats.Text.Contains(i))
                    ? Red
                    : Green;
                BiggestTexture.Text = $"{BiggestTex[1].Trim()} x {BiggestTex[2].Trim()}";
                TextureCount.Text = TextureLines.Count().ToString();
                MipMaps.Text = TextureLines.Any(s => s.Split('|')[4].Contains("Mip", StringComparison.OrdinalIgnoreCase)).ToString();

                igSkinName.Text = igSkin = Alchemy.IntName(StatLines);
                igSkinName.Foreground = igSkinNameT.Foreground = igSkin == Name ? Green : Red;

                SkinInfo.Visibility = Visibility.Visible;
            }
            FileInfo SIGB = new(SourceIGB);
            if (SIGB.Exists)
            {
                bool ConvGeo = Plat is 2 or 3 or 4 or 7 or 8 && GeometryFormats.Text.Contains("1_5");
                bool HexEdit = !(string.IsNullOrWhiteSpace(igSkin) || igSkin == Name || igSkin.StartsWith("Bip01") || GamePath.StartsWith("hud") || GamePath.StartsWith("ui"));
                string IGB = Path.Combine(Directory.CreateDirectory(Path.Combine(Cfg.OHS.GameInstallPath, GamePath)).FullName, $"{Name}.igb");
                Cfg.Var.SE_Msg_Info = new MessageItem { Message = $"Replaced '{IGB}'.", IsOpen = File.Exists(IGB) };

                bool optimized = (ConvGeo || HexEdit || !GamePath.StartsWith("ui"))
                    && Alchemy.CopySkin(SIGB, IGB, Name, PAV, ConvGeo, igSkin, HexEdit);
                if (optimized && ConvGeo) { GeometryFormats.Foreground = GeometryFormatsT.Foreground = Green; }

                try
                {
                    optimized = optimized || (SIGB.CopyTo(IGB, true) is not null
                        && HexEdit && Util.HexEdit(igSkin ?? "igActor01Appearance", Name, IGB));
                }
                catch { optimized = false; }

                if (optimized && HexEdit)
                {
                    igSkinName.Text = Name;
                    igSkinName.Foreground = igSkinNameT.Foreground = Green;
                }
                Cfg.Var.SE_Msg_Warning = new MessageItem
                {
                    Message = $"'{SourceIGB}' is possibly incompatible and can't be optimized or optimization has failed. Check the IGB statistics",
                    IsOpen = new[] { AlchemyVersion, FileSize, GeometryFormats, TextureFormats, igSkinName }.Any(t => t.Foreground == Red)
                };
                ShowSuccess($"Skin installed to '{IGB}'.");
            }
            else if (!string.IsNullOrWhiteSpace(SourceIGB)) { ShowError($"'{SourceIGB}' not found."); }
        }
        /// <summary>
        /// Calculate Emma Frosts Diamond Form number for <paramref name="SkinNumber"/>.
        /// </summary>
        /// <returns>Matching Diamond Form number or 99, if invalid.</returns>
        private static int GetDiamondFormNumber(string SkinNumber)
        {
            int N = int.Parse(SkinNumber);
            int LN = N % 10;
            return LN == 9 && N < 99
                ? N + 1
                : LN is < 5 and > 0
                ? N + 4
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
            if (e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Mouse or Microsoft.UI.Input.PointerDeviceType.Pen && Cfg.Roster.SkinsList.Count > 1)
            {
                VisualStateManager.GoToState(sender as Control, "HoverButtonsShown", true);
            }
        }
        /// <summary>
        /// Hide the delete button
        /// </summary>
        private void SkinTemplate_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(sender as Control, "HoverButtonsHidden", true);
        }
        /// <summary>
        /// Remove Skin Slot
        /// </summary>
        private void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter != null
                && Cfg.Roster.SkinsList.FirstOrDefault(s => s.Number == (args.Parameter as string)) is SkinDetails STR
                && !(Cfg.GUI.Game == "xml2" && STR.Name == "Main"))
            {
                _ = Cfg.Roster.SkinsList.Remove(STR);
                AddButton.Visibility = Cfg.Roster.SkinsList.Count < SkinIdentifiers.Length
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
        /// <summary>
        /// Add a Skin Slot
        /// </summary>
        private void AddSkinSlot_Click(object sender, RoutedEventArgs e)
        {
            int MaxSkinNum = 0;
            string[] AvailableXMLNames = SkinIdentifiers.Select(s => s == "skin" ? "Main" : s[5..]).ToArray();
            if (Cfg.Roster.SkinsList.Any())
            {
                AvailableXMLNames = AvailableXMLNames.Except(Cfg.Roster.SkinsList.Select(s => s.Name)).ToArray();
                _ = int.TryParse(Cfg.Roster.SkinsList.OrderBy(s => s.Number).Last().Number, out MaxSkinNum);
            }
            AddSkin(AddCharNum.Text, (MaxSkinNum + 1).ToString().PadLeft(2, '0'), Cfg.GUI.Game == "xml2" ? AvailableXMLNames[0] : "");
            Skins.SelectedIndex = Cfg.Roster.SkinsList.Count - 1;
        }
        /// <summary>
        /// Enable selection of items when clicking on text boxes
        /// </summary>
        private void FocusToSelect(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement Control)
            {
                Skins.SelectedItem = Control.DataContext;
            }
        }
        /// <summary>
        /// Restrict skin number input
        /// </summary>
#pragma warning disable CA1822 // Mark members as static
        private void SkinNumber_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
#pragma warning restore CA1822 // Mark members as static
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c)) || args.NewText.Length > 2;
        }
        /// <summary>
        /// Ensure two digit number
        /// </summary>
#pragma warning disable CA1822 // Mark members as static
        private void SkinNumber_LosingFocus(UIElement sender, LosingFocusEventArgs args)
#pragma warning restore CA1822 // Mark members as static
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
            if (Cfg.Var.HsPath is not null && Cfg.Roster.SkinsList.Any())
            {
                if (SaveSkinList()) { ShowSuccess("Skin information saved."); } else { ShowError("Skin information could not be saved."); }
                CheckPackages();
            }
        }
        /// <summary>
        /// Load the installation commands when a skin is selected
        /// </summary>
        private void Skins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin && Path.IsPathFullyQualified(Cfg.OHS.GameInstallPath)) // Again, this is not consistent with the relative path that the settings allow.
            {
                XML2platforms.Visibility = ThreeDHeadBrowse.Visibility = Cfg.GUI.Game != "xml2"
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                MUAplatforms.Visibility = Cfg.GUI.Game == "xml2"
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                SkinInfo.Visibility = Visibility.Collapsed;
                HudHeadBrowse.Visibility = SkinInstaller.Visibility = Visibility.Visible;
                InstallSkins.Text = $"Install skin {Skin.CharNum}{Skin.Number}";
                InstallMannequin.Text = $"Install select item {Skin.CharNum}{MqNum}";
            }
        }

        private void Skins_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (Skins.SelectedItem is SkinDetails Skin)
            {
                _ = Cfg.Roster.SkinsList.Remove(Skin);
            }
        }
        /// <summary>
        /// Browse for an IGB file and install it to actors in the game/mod files, renaming it to the selected <see cref="Skins"/> name.
        /// This is separate from InstallIGB, because it tries to install the hud heads as well.
        /// </summary>
        private async void InstallSkins_Click(object sender, RoutedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin && await CfgCmd.LoadDialogue(".igb") is string IGB && IGB != "")
            {
                FixSkins(IGB, "actors", $"{Skin.CharNum}{Skin.Number}");
                string IGBpath = Path.GetDirectoryName(IGB)!;
                string IGBbase = Path.GetFileNameWithoutExtension(IGB);
                FileInfo HUD = new(Path.Combine(IGBpath, $"hud_head_{IGBbase}.igb"));
                FileInfo H3D = new(Path.Combine(IGBpath, $"{IGBbase} (3D Head).igb"));
                // Note: heads are not not optimized. They may not work, if the compatibility is not given. Hex-editing is not necessary.
                if (HUD.Exists)
                {
                    OHSpath.CopyToGame(HUD, "hud", $"hud_head_{Skin.CharNum}{Skin.Number}.igb");
                    HudHeadBrowse.Visibility = Visibility.Collapsed;
                }
                if (Cfg.GUI.Game == "xml2" && H3D.Exists)
                {
                    OHSpath.CopyToGame(H3D, Path.Combine("ui", "hud", "characters"), $"{Skin.CharNum}{Skin.Number}.igb");
                    ThreeDHeadBrowse.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void InstallHuds_Click(object sender, RoutedEventArgs e) => InstallIGB("hud", "", "hud_head_");

        private void Install3DHead_Click(object sender, RoutedEventArgs e) => InstallIGB(Path.Combine("ui", "hud", "characters"));

        private void InstallMannequin_Click(object sender, RoutedEventArgs e)
        {
            InstallIGB(Path.Combine("ui", "models", Cfg.GUI.Game == "mua" ? Cfg.MUA.MannequinFolder : "characters"), MqNum);
        }

        private void DiamondForm_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ToolTip Tt && Skins.SelectedItem is SkinDetails Skin)
            {
                Tt.Content = $"Browse for Emma Frost's Diamond Form skin to install as {Skin.CharNum}{GetDiamondFormNumber(Skin.Number).ToString().PadLeft(2, '0')}.igb - Works for other characters that use this system";
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
            if (Skins.SelectedItem is SkinDetails Skin && GetDiamondFormNumber(Skin.Number) is int N && N < 99)
            {
                InstallIGB("actors", $"{N.ToString().PadLeft(2, '0')}");
            }
        }

        private void InstallHumanTorch_Click(object sender, RoutedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin && int.Parse(Skin.Number) + 10 is int N && N < 100)
            {
                InstallIGB("actors", $"{N}");
            }
        }

        private async void InstallExtraSkin_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await EnterSkinNumber.ShowAsync();
            if (result == ContentDialogResult.Primary && string.IsNullOrEmpty(ExtraSkinNumber.Text))
            {
                InstallIGB("actors", ExtraSkinNumber.Text);
            }
        }
        /// <summary>
        /// Show an error <paramref name="message"/> in an <see cref="InfoBar"/>.
        /// </summary>
        private void ShowError(string message)
        {
            Cfg.Var.SE_Msg_Error = new MessageItem {
                Message = message,
                IsOpen = true
            };
        }
        /// <summary>
        /// Show a success <paramref name="message"/> in an <see cref="InfoBar"/> for 5 seconds.
        /// </summary>
        private async void ShowSuccess(string message)
        {
            Cfg.Var.SE_Msg_Success = new MessageItem
            {
                Message = message,
                IsOpen = true
            };
            await Task.Delay(5000);
            Cfg.Var.SE_Msg_Success = new MessageItem() { IsOpen = false };
        }
    }
}
