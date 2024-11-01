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
            if (Cfg.GUI.Game == "XML2" && Cfg.GUI.SkinsDragEnabled) { Skins.CanDragItems = true; }
            Skins.AllowDrop = Skins.CanReorderItems = Cfg.GUI.Game != "XML2" || Cfg.GUI.SkinsDragEnabled;
            string[] TargetPaths = OHSpath.ModFolders.Select(d => d.Name).ToArray();
            TargetPath.ItemsSource = TargetPaths.Length == 0 ? [Cfg.OHS.GameInstallPath] : TargetPaths;
            TargetPath.SelectedIndex = Array.IndexOf(TargetPaths, Cfg.GUI.SkinModName == "" ? new DirectoryInfo(Cfg.OHS.GameInstallPath).Name : Cfg.GUI.SkinModName) is int i && i > 0 ? i : 0;
            LoadSkinList();
            // Note: Skins are always loaded from file when navigated (except if nothing selected) - intended, to read external changes
        }
        /// <summary>
        /// Load skin details from a herostat with a single stat only.
        /// </summary>
        private void LoadSkinList()
        {
            SkinInstaller.Visibility = SkinInstallerH.Visibility = Visibility.Collapsed;
            Cfg.Roster.SkinsList.Clear();
            AddCharNum.Text = "";
            if (Herostat.Load(Cfg.Var.FloatingCharacter) is string[] LoadedHerostat)
            {
                if (Cfg.Var.HsFormat == '<')
                {
                    if (GUIXML.ElementFromString(string.Join(Environment.NewLine, LoadedHerostat)) is XmlElement Stats
                        && Stats.GetAttribute("skin") is string Skin && Skin.Length > 3)
                    {
                        CharacterName.Text = Stats.GetAttribute("charactername");
                        InternalName = Stats.GetAttribute("name");
                        for (int i = 0; i < SkinIdentifiers.Length; i++)
                        {
                            string Skn = Stats.GetAttribute(SkinIdentifiers[i]);
                            if (Skn.Length > 1 || Cfg.GUI.Game == "XML2")
                            {
                                string SkinName = Cfg.GUI.Game == "XML2"
                                    ? i == 0
                                        ? "Main"
                                        : XML2Skins[i - 1]
                                    : i == 0
                                        ? Stats.GetAttribute("skin_01_name")
                                        : Stats.GetAttribute($"{SkinIdentifiers[i]}_name");
                                AddSkin(Skin[..^2], Skn.Length > 1 ? Skn[^2..] : "", SkinName);
                            }
                        }
                    }
                }
                else if (Formats.GetAttr(LoadedHerostat, "skin") is string Skin && Skin.Length > 3)
                {
                    CharacterName.Text = Formats.GetAttr(LoadedHerostat, "charactername");
                    InternalName = Formats.GetAttr(LoadedHerostat, "name");
                    for (int i = 0; i < SkinIdentifiers.Length; i++)
                    {
                        string Skn = Formats.GetAttr(LoadedHerostat, SkinIdentifiers[i]);
                        if (Skn.Length > 1 || Cfg.GUI.Game == "XML2")
                        {
                            string SkinName = Cfg.GUI.Game == "XML2"
                                ? i == 0
                                    ? "Main"
                                    : XML2Skins[i - 1]
                                : i == 0
                                    ? Formats.GetAttr(LoadedHerostat, "skin_01_name")
                                    : Formats.GetAttr(LoadedHerostat, $"{SkinIdentifiers[i]}_name");
                            AddSkin(Skin[..^2], Skn.Length > 1 ? Skn[^2..] : "", SkinName);
                        }
                    }
                }
            }
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

                bool IsMUA = Cfg.GUI.Game != "XML2";
                if (!IsMUA)
                {
                    List<SkinDetails> XML2skins = [.. Cfg.Roster.SkinsList];
                    Cfg.Roster.SkinsList.Clear();
                    for (int i = 0; i < XML2skins.Count; i++)
                    {
                        if (XML2skins[i].Number == "") { continue; }
                        AddSkin(XML2skins[i].CharNum, XML2skins[i].Number, Cfg.Roster.SkinsList.Count == 0 ? "Main" : XML2Skins[i - 1]);
                    }
                }
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
        /// Build a skin detail
        /// </summary>
        /// <returns>Skin "<paramref name="CharacterNumber"/><paramref name="Number"/> - <paramref name="SkinName"/>".</returns>
        private SkinDetails Skin(string CharacterNumber, string Number, string SkinName)
        {
            return new SkinDetails
            {
                CharNum = CharacterNumber,
                Number = Number,
                Name = SkinName,
                Command = DeleteCommand
            };
        }
        /// <summary>
        /// Add Skin "<paramref name="CharacterNumber"/><paramref name="Number"/> - <paramref name="SkinName"/>" to the display list
        /// </summary>
        private void AddSkin(string CharacterNumber, string Number, string SkinName)
        {
            AddCharNum.Text = CharacterNumber;
            Cfg.Roster.SkinsList.Add(Skin(CharacterNumber, Number, SkinName));
            AddButton.Visibility = Cfg.Roster.SkinsList.Count < SkinIdentifiers.Length
                ? Visibility.Visible
                : Visibility.Collapsed;
            SaveSkinDetails.IsEnabled = true;
        }
        /// <summary>
        /// Remove Skin from Cfg.Roster.SkinsList at <paramref name="i"/>ndex. (Count must be more)
        /// </summary>
        /// <param name="i"></param>
        private void RemoveSkin(int i)
        {
            if (Cfg.GUI.Game == "XML2" && Cfg.Roster.SkinsList.Count > i && i > 0)
            {
                Cfg.Roster.SkinsList[i] = Skin(AddCharNum.Text, "", XML2Skins[i - 1]);
            }
            else { Cfg.Roster.SkinsList.RemoveAt(i); }
        }
        /// <summary>
        /// Fix the XML2 Cfg.Roster.SkinsList's skin names to be in the correct order and fill missing skins with empty entries.
        /// </summary>
        private void FixXML2names(bool Add = false)
        {
            List<SkinDetails> XML2skins = [.. Cfg.Roster.SkinsList];
            Cfg.Roster.SkinsList.Clear();
            for (int i = -1; i < XML2Skins.Length; i++)
            {
                string Name = i < 0 ? "Main" : XML2Skins[i];
                Cfg.Roster.SkinsList.Add(Skin(AddCharNum.Text,
                    !Add ? XML2skins[i + 1].Number : XML2skins.FirstOrDefault(s => s.Name == Name) is SkinDetails S ? S.Number : "",
                    Name));
            }
            AddButton.Visibility = Visibility.Collapsed;
            SaveSkinDetails.IsEnabled = true;
        }
        /// <summary>
        /// Combine <see cref="Cfg.OHS.GameInstallPath"/> + <see cref="TargetPath"/> info with <paramref name="GamePath"/> and <paramref name="Name"/> and create the necessary folders.
        /// </summary>
        /// <returns>The combined target path (.igb file) or <see cref="null"/> if no <see cref="TargetPath"/>'s selected.</returns>
        private string? TargetIgb(string GamePath, string Name)
        {
            return TargetPath.SelectedItem.ToString() is string TP
                ? Path.Combine(
                    Directory.CreateDirectory(Path.Combine(TP != Cfg.OHS.GameInstallPath
                        && new DirectoryInfo(Cfg.OHS.GameInstallPath).Parent?.GetDirectories(TP) is DirectoryInfo[] TPF
                        && TPF.Length > 0 ? TPF[0].FullName : TP, GamePath)).FullName,
                    $"{Name}.igb")
                : null;
        }
        /// <summary>
        /// Calculate the mannequin number, based on the first skin.
        /// </summary>
        /// <returns>Number as <see cref="string"/> of the first skin if XML2 and bigger than 10, otherwise "01".</returns>
        private string MqNum => Cfg.GUI.Game == "XML2" && int.Parse(Cfg.Roster.SkinsList.First().Number) > 10 ? Cfg.Roster.SkinsList.First().Number : "01";
        /// <summary>
        /// Check all skins in the SkinsList for packages and, if not found, clone a package if available. Shows message if not found and not available for cloning.
        /// </summary>
        private void CheckPackages()
        {
            string TargetPath = TargetIgb("", "") is string TP ? TP[..^5] : Cfg.OHS.GameInstallPath;
            bool AllFound = true;
            for (int i = 0; i < Cfg.Roster.SkinsList.Count; i++)
            {
                AllFound = MarvelModsXML.ClonePackage(OHSpath.FoldersWpkg, Cfg.Roster.SkinsList[i], InternalName, TargetPath) && AllFound;
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
        /// Hex edit hud head <paramref name="IGB"/> files, using the number from <paramref name="Name"/>, to make them compatible with NPC.
        /// </summary>
        /// <returns><see langword="True"/>, if edited successfully, otherwise <see langword="false"/>.</returns>
        private bool FixHUDs(string IGB, string Name)
        {
            return Opt.Write([.. Opt.Head(1), .. Opt.StatTex(1)], IGB)
                && Util.RunDosCommnand(Alchemy.Optimizer!, $"\"{IGB}\" \"{Path.Combine(OHSpath.Temp, "temp.igb")}\" \"{Alchemy.INI}\"") is string Stats
                && FixHUDs(IGB, Name, Stats.Split(Environment.NewLine).Where(s => s.Contains("IG_GFX_TEXTURE_FORMAT_")));
        }
        /// <summary>
        /// Hex edit hud head <paramref name="IGB"/> files, using the number from <paramref name="Name"/> and information from <paramref name="TextureLines"/>, to make them compatible with NPC.
        /// </summary>
        /// <returns><see langword="True"/>, if edited successfully, otherwise <see langword="false"/>.</returns>
        private bool FixHUDs(string IGB, string Name, IEnumerable<string> TextureLines)
        {
            if (TextureLines.Any())
            {
                string[] all = [.. TextureLines.Select(l => l.Split('|')[0].Trim())];
                int maxLen = all.Min(t => t.Length) - 4;
                string main = (TextureLines.FirstOrDefault(l => !char.IsAsciiDigit(l.Trim()[^1]) || l.Trim()[^1] == '0')
                    ?? TextureLines.First()).Split('|')[0].Trim();
                return Util.HexEdit([.. all.Select(l => l[..maxLen])],
                    (Name.Split('_')[^1] + "_" + main.Split('_', 2)[1])[..maxLen],
                    IGB);
            }
            return false;
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
            int Plat = Cfg.GUI.Game == "XML2"
                ? XML2platforms.SelectedIndex + 10
                : MUAplatforms.SelectedIndex;
            int PAV = Plat is < 1 or 2 or 3 or 7 or 8
                ? 9
                : Plat is 4 or 5 or 13
                ? 8
                : 6;

            SolidColorBrush Red = new(Colors.Red);
            SolidColorBrush Green = new(Colors.Green);

            GeometryFormats.Text = "";
            string? igSkin = null;
            bool IsSkin = GamePath.StartsWith("actors");
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
                    || ((Plat is > 10 or 1 or 4 or 6) && fs.Length > 300000)
                    ? Red
                    : Green;

                // Wii (Plat 5) is buggy when attr2 was added with Alchemy 5 and XML2 PSP doesn't support any Alchemy 5. They require native (Alch 3.5) attr2 however.
                GeometryFormats.Text = string.Join(", ", GeometryLines.Distinct());
                GeometryFormats.Foreground = GeometryFormatsT.Foreground =
                    (PAV == 6 && GeometryFormats.Text.Contains('2'))
                    || (PAV > 6 && Plat > 0 && GeometryFormats.Text.Contains("1_5"))
                    ? Red
                    : Green;
                VertexCount.Text = StatLines.Where(s => s.Contains(" vertex total: ", StringComparison.OrdinalIgnoreCase)).Max(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries)[3]);
                GeometryCount.Text = GeometryLines.Count().ToString();

                TextureFormats.Text = string.Join(", ", TextureLines.Select(s => s.Split('|')[3].Trim()).Distinct()).Replace("IG_GFX_TEXTURE_FORMAT_", "");
                string[] IncompatibleTexs = Plat is < 1 or 10 or 9
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
                MipMaps.Text = TextureLines.Select(s => s.Split('|')[4]).Count(s => s.Contains("Mip", StringComparison.OrdinalIgnoreCase) && s.Trim()[^1] != '0').ToString();

                igSkinName.Text = igSkin = IsSkin ? Alchemy.IntName(StatLines) : "";
                igSkinName.Foreground = igSkinNameT.Foreground = IsSkin && igSkin != Name ? Red : Green;

                SkinInfo.Visibility = Visibility.Visible;
            }
            FileInfo SIGB = new(SourceIGB);
            if (SIGB.Exists && TargetIgb(GamePath, Name) is string IGB)
            {
                bool ConvGeo = Plat is 2 or 3 or 4 or 7 or 8 && GeometryFormats.Text.Contains("1_5");
                bool HexEdit = IsSkin && !(string.IsNullOrWhiteSpace(igSkin) || igSkin == Name || igSkin.StartsWith("Bip01"));
                Cfg.Var.SE_Msg_Info = new MessageItem { Message = $"Replaced '{IGB}'.", IsOpen = File.Exists(IGB) };

                bool optimized = (ConvGeo || HexEdit || !GamePath.StartsWith("ui"))
                    && Alchemy.CopySkin(SIGB, IGB, Name, PAV, ConvGeo, igSkin, HexEdit);
                if (optimized && ConvGeo) { GeometryFormats.Foreground = GeometryFormatsT.Foreground = Green; }

                try
                {
                    optimized = optimized || (SIGB.CopyTo(IGB, true) is not null
                        && HexEdit && Util.HexEdit([igSkin ?? "igActor01Appearance"], Name, IGB));
                    if (GamePath == "hud") { _ = FixHUDs(IGB, Name); }
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
                Cfg.GUI.SkinModName = TargetPath.SelectedItem.ToString() ?? "";
            }
            else if (!string.IsNullOrWhiteSpace(SourceIGB)) { ShowError($"'{SourceIGB}' not found."); }
            CheckPackages();
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
            if (args.Parameter is string Number
                && !(Cfg.GUI.Game == "XML2" && Number == ""))
            {
                for (int i = 0; i < Cfg.Roster.SkinsList.Count; i++)
                {
                    if (Cfg.Roster.SkinsList[i].Number == Number)
                    {
                        RemoveSkin(i);
                        break;
                    }
                }
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
            if (string.IsNullOrEmpty(Cfg.Var.FloatingCharacter) || string.IsNullOrEmpty(AddCharNum.Text)) { return; }
            if (Cfg.GUI.Game == "XML2") { FixXML2names(true); }
            else
            {
                AddSkin(AddCharNum.Text,
                    Cfg.Roster.SkinsList.Any()
                    && int.TryParse(Cfg.Roster.SkinsList.OrderBy(s => s.Number).Last().Number, out int MaxSkinNum)
                    ? (MaxSkinNum + 1).ToString().PadLeft(2, '0')
                    : "00", "");
            }
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
            SkinInstaller.Visibility = SkinInstallerH.Visibility = Visibility.Collapsed;
            if (Skins.SelectedItem is SkinDetails Skin && Skin.Number != "" && Path.IsPathFullyQualified(Cfg.OHS.GameInstallPath)) // Again, this is not consistent with the relative path that the settings allow.
            {
                SkinInfo.Visibility = Visibility.Collapsed;
                HudHeadBrowse.Visibility = SkinInstaller.Visibility = SkinInstallerH.Visibility = Visibility.Visible;
                SelectedSkinNumber.Text = $"{Skin.CharNum}{Skin.Number}";
                InstallSkins.Text = $"Install skin {Skin.CharNum}{Skin.Number}:";
                InstallMannequin.Text = $"Install {(Cfg.GUI.Game == "XML2" ? "character selection portrait" : "mannequin")} {Skin.CharNum}{MqNum}:";
            }
        }

        private void Skins_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (Cfg.GUI.Game == "XML2" && Cfg.Roster.SkinsList.Count > Skins.SelectedIndex && Skins.SelectedIndex > -1)
            {
                RemoveSkin(Skins.SelectedIndex);
            }
        }

        private void Skins_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args) => FixXML2names();
        /*
        For Skin swapping:
        1. make a global variable: private int before;
        2. make a DragItemsStarting event to set the before index:
            before = args.Items[0] is SkinDetails SD ? Cfg.Roster.SkinsList.IndexOf(SD) : -1;
        3. change the DragItemsCompleted event:
        {
            int after = args.Items[0] is SkinDetails SD ? Cfg.Roster.SkinsList.IndexOf(SD) : -1;
            if (after == before || after < 0 || before < 0) { return; }
            List<SkinDetails> Sorted = [.. Cfg.Roster.SkinsList.OrderBy(s => Array.IndexOf(XML2Skins, s.Name))];
            for (int i = 0; i < Sorted.Count; i++)
            {
                Cfg.Roster.SkinsList[i] = Skin(
                    Sorted[i].CharNum,
                    i == after ? Sorted[before].Number : i == before ? Sorted[after].Number : Sorted[i].Number,
                    Sorted[i].Name);
            }
        }
        */
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
                if (HUD.Exists && TargetIgb("hud", $"hud_head_{Skin.CharNum}{Skin.Number}.igb") is string HUDT)
                {
                    _ = HUD.CopyTo(HUDT, true);
                    _ = FixHUDs(HUDT, $"{Skin.CharNum}{Skin.Number}");
                    HudHeadBrowse.Visibility = Visibility.Collapsed;
                }
                if (Cfg.GUI.Game == "XML2" && H3D.Exists && TargetIgb(Path.Combine("ui", "hud", "characters"), $"{Skin.CharNum}{Skin.Number}.igb") is string H3DT)
                {
                    _ = H3D.CopyTo(H3DT, true);
                    Head3D_Browse.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void InstallHuds_Click(object sender, RoutedEventArgs e) => InstallIGB("hud", "", "hud_head_");

        private void Install3DHead_Click(object sender, RoutedEventArgs e) => InstallIGB(Path.Combine("ui", "hud", "characters"));

        private void InstallMannequin_Click(object sender, RoutedEventArgs e)
        {
            InstallIGB(Path.Combine("ui", "models", Cfg.GUI.Game == "XML2" ? "characters" : Cfg.MUA.MannequinFolder), MqNum);
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
            Cfg.Var.SE_Msg_Error = new MessageItem
            {
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
