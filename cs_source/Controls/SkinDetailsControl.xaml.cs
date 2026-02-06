using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenHeroSelectGUI.Controls
{
    /// <summary>
    /// The Skin details pane that shows skin details. Depends on Floating character and can be added to other pages.
    /// </summary>
    public sealed partial class SkinDetailsControl : UserControl
    {
        internal HStats Stats { get; set; } = new();
        public Cfg Cfg { get; set; } = new();
        internal Messages Msg { get; set; } = new();

        private bool AlchemyWarningClosed; // Could possibly use a saved setting, if I figure out a nice way to prompt the user.

        public SkinDetailsControl()
        {
            InitializeComponent();
            MannequinType.Text = Cfg.GUI.IsXml2 ? "character selection portrait" : "mannequin";
            IEnumerable<string> TargetPaths = OHSpath.ModFolders.Select(static d => Path.GetFileName(d)); // Array is unsafe!
            string SkinMOdName = Cfg.GUI.SkinModName.Length > 0 || Cfg.OHS.GameInstallPath.Length == 0
                ? Cfg.GUI.SkinModName
                : OHSpath.GetName(Cfg.OHS.GameInstallPath);
            TargetPath.ItemsSource = TargetPaths.Any() ? TargetPaths : [Cfg.OHS.GameInstallPath];
            TargetPath.SelectedIndex = TargetPaths.Index().FirstOrDefault(x => x.Item == SkinMOdName).Index;
            // Note: `Array.IndexOf([.. TargetPaths], search) is int i && i > 0` is ever so slightly faster (on short arrays), but more memory intensive.
            // Note: Skins are always loaded from file when navigated (except if nothing selected) - intended, to read external changes
            if (Cfg.Var.FloatingCharacter is not null) { LoadHerostat(); }
            Cfg.Var.PropertyChanged += FloatingCharacter_Changed;
        }
        /// <summary>
        /// Load the herostat from <see cref="Cfg.Var.FloatingCharacter"/> in <see cref="Stats"/>.SkinList.
        /// </summary>
        private void LoadHerostat()
        {
            SkinInstallerButtons.Visibility = Visibility.Collapsed;
            SelectedSkinNumber.Text = "";
            try { Stats.Load(Cfg.Var.FloatingCharacter!); } catch { return; }
            InstallMannequin.Visibility = InstallMannequinB.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Combine <see cref="OHSsettings.GameInstallPath"/> + <see cref="TargetPath"/> info with <paramref name="GamePath"/> and <paramref name="Name"/> and create the necessary folders.
        /// </summary>
        /// <returns>The combined full target path to the "<paramref name="Name"/>.igb" file, if a <see cref="TargetPath"/>'s selected and no exception occurred; otherwise <see langword="null"/> .</returns>
        private string? TargetIgb(string GamePath, string Name)
        {
            try
            {
                return TargetPath.SelectedItem.ToString() is string TP
                    ? Path.Combine(Directory.CreateDirectory(Path.Combine(
                            TP == Cfg.OHS.GameInstallPath ? "" : OHSpath.GetParent(Cfg.OHS.GameInstallPath) ?? "",
                            TP, GamePath)).FullName,
                        $"{Name}.igb") : null;
            }
            catch { return null; }
        }
        /// <summary>
        /// Check all skins in the <see cref="Stats"/>.SkinsList for packages and, if not found, clone a package if available. Shows message if not found and not available for cloning.
        /// </summary>
        private void CheckPackages()
        {
            IEnumerable<string> ExistingPkgFolders = []; try { ExistingPkgFolders = OHSpath.FoldersWpkg; } catch { }
            bool AllFound = ExistingPkgFolders.Any();
            if (AllFound && TargetPath.SelectedItem.ToString() is string TP)
            {
                TP = Path.Combine(TP == Cfg.OHS.GameInstallPath ? "" : OHSpath.GetParent(Cfg.OHS.GameInstallPath) ?? "", TP);
                _ = Directory.CreateDirectory(OHSpath.Packages(TP)); // create packages folder to be on the safe side
                for (int i = 0; i < Stats.SkinsList.Count; i++)
                {
                    AllFound = MarvelModsXML.ClonePackage(ExistingPkgFolders, Stats.SkinsList[i], Stats.InternalName, TP) && AllFound;
                }
            }
            Msg.SE_WarnPkg.IsOpen = !AllFound;
        }
        /// <summary>
        /// Browse for an IGB file and install it to <paramref name="GamePath"/> in the game/mod files, renaming it to the selected <see cref="Skins"/> number (plus number <paramref name="N"/> if given and optional <paramref name="Prefix"/>).
        /// </summary>
        private async void InstallIGB(string GamePath, string? N = null, string Prefix = "")
        {
            if (Skins.SelectedItem is SkinDetails Skin && await CfgCmd.LoadDialogue(".igb") is string IGB && IGB != "")
            {
                FixSkins(IGB, GamePath, $"{Prefix}{Skin.CharNum}{N ?? Skin.Number}");
            }
        }
        /// <summary>
        /// Hex edit hud head <paramref name="IGB"/> files, using the <paramref name="Number"/>, to make them compatible with NPC.
        /// </summary>
        /// <returns><see langword="True"/>, if edited successfully; otherwise <see langword="false"/>.</returns>
        private static bool FixHUDs(string IGB, string Number)
        {
            return File.Exists(IGB) && Opt.Write(Opt.Combine([Opt.StatTex]))
                && Util.RunDosCommnand(Alchemy.Optimizer!, $"\"{IGB}\" \"{Path.Combine(OHSpath.Temp, "temp.igb")}\" \"{Alchemy.INI}\"") is string Stats
                && FixHUDs(IGB, Number, Stats.Split(Environment.NewLine).Where(s => s.Contains("IG_GFX_TEXTURE_FORMAT_")));
        }
        /// <summary>
        /// Hex edit hud head <paramref name="IGB"/> files, using the <paramref name="Number"/> and information from <paramref name="TextureLines"/>, to make them compatible with NPC.
        /// </summary>
        /// <returns><see langword="True"/>, if edited successfully; otherwise <see langword="false"/>.</returns>
        private static bool FixHUDs(string IGB, string Number, IEnumerable<string> TextureLines)
        {
            if (!TextureLines.Any()) { return false; } // There is no texture, the hud head won't work (throw an error?)
            IEnumerable<string> all = TextureLines.Select(l => l.Split('|')[0].Trim());
            int maxLen = all.Min(t => t.Length) - 4;
            string main = (TextureLines.FirstOrDefault(l => !char.IsAsciiDigit(l.Trim()[^1]) || l.Trim()[^1] == '0')
                ?? TextureLines.First()).Split('|')[0].Trim();
            int di, mi; if (maxLen < Number.Length || (di = main.IndexOf('_') + 1) > (mi = di + maxLen - Number.Length - 1)) { return false; }
            return Util.HexEdit([.. all.Select(l => l[..maxLen])], $"{Number}_{main[di..mi]}", IGB);
        }
        /// <summary>
        /// Platform enum values, corresponding with the selected platform
        /// </summary>
        private enum Platform
        {
            MUA_NotSel = -1,
            MUA_PC_2006 = 0,
            MUA_PS2 = 1,
            MUA_PS3 = 2,
            MUA_PS4 = 3,
            MUA_PSP = 4,
            MUA_Wii = 5,
            MUA_XBOX = 6,
            MUA_XENO = 7,
            MUA_XOne = 8, // and Steam
            XML2_NotSel = 9,
            XML2_PC = 10,
            XML2_GCUB = 11,
            XML2_PS2 = 12,
            XML2_PSP = 13,
            XML2_XBOX = 14
        }
        /// <summary>
        /// Alchemy version enum with hex version ID/number and corresponding known actual version
        /// </summary>
        private enum AlchemyV : byte
        {
            AV_Unknown = 0,
            AV_2_5 = 4,
            AV_3_2 = 6,
            AV_3_5 = 8,
            AV_5 = 9,
        }
        /// <summary>
        /// Apply Alchemy optimizations to make the <paramref name="SourceIGB"/> compatible if possible and show compatibility information. Performs a crash if target directory can't be written to.
        /// Copy <paramref name="SourceIGB"/> to [gameInstallPath]/<paramref name="GamePath"/>/<paramref name="Name"/>.igb.
        /// </summary>
        private void FixSkins(string SourceIGB, string GamePath, string Name)
        {
            SkinInfo.Visibility = Visibility.Collapsed;
            FileInfo? SIGB; try { SIGB = new(SourceIGB); } catch { SIGB = null; }
            if (SIGB is null || !SIGB.Exists) { Msg.SE_Error.Message = $"'{SourceIGB}' not found, or not accessible."; return; }
            string? IGB = TargetIgb(GamePath, Name);
            if (IGB is null) { Msg.SE_Error.Message = $"Failed to ensure the existence of the directory for '{GamePath}{Name}.igb'."; return; }

            Platform Plat = (Platform)(Cfg.GUI.IsXml2
                ? XML2platforms.SelectedIndex + 10
                : MUAplatforms.SelectedIndex);
            if (Plat is Platform.MUA_NotSel or Platform.XML2_NotSel) { Plat = (Platform)((int)Plat + 1); }
            int PAV = Plat is Platform.MUA_PC_2006 or Platform.MUA_PS3 or Platform.MUA_PS4 or Platform.MUA_XENO or Platform.MUA_XOne // NextGen
                ? 9
                : Plat is Platform.MUA_PSP or Platform.MUA_Wii or Platform.XML2_PSP
                ? 8
                : 6;
            bool BadAV = false, BadSkin = false, BadGeo = false, BadTex = false, BadSize = false, ConvGeo = false;

            GeometryFormats.Text = "";
            string? igSkin = null;
            bool IsSkin = GamePath.StartsWith("actors");
            if (Alchemy.GetSkinStats(SourceIGB) is string Stats)
            {
                string[] StatLines = Stats.Split(Environment.NewLine);
                IEnumerable<string> GeometryLines = StatLines.Where(s => s.Contains("igGeometryAttr")).Select(s => s.Split('|')[2].Trim());
                IEnumerable<string[]> TextureLines = StatLines.Where(s => s.Contains("IG_GFX_TEXTURE_FORMAT_")).Select(s => s.Split('|'));
                IEnumerable<string> TexFormats = TextureLines.Select(s => s[3].Trim()).Distinct()
                    .Select(s => s.Length > 22 && s[..22] == "IG_GFX_TEXTURE_FORMAT_" ? s[22..] : s);
                string[]? BiggestTex = TextureLines.OrderByDescending(s => int.Parse(s[1]) * int.Parse(s[2])).FirstOrDefault();

                AlchemyV AV;
                using FileStream fs = File.OpenRead(SourceIGB);
                fs.Position = 0x2C;
                try { AV = (AlchemyV)fs.ReadByte(); } catch { AV = AlchemyV.AV_Unknown; }

                BadAV = (int)AV > PAV;
                string v = AV.ToString();
                AlchemyVersion.Text = v.Length > 3 ? v[3..].Replace('_', '.') : v;

                BadSize = (Plat is Platform.MUA_Wii && fs.Length > 600E+3)
                    || ((Plat is Platform.MUA_PSP or Platform.XML2_PSP) && fs.Length > 150E+3)
                    || (((int)Plat > 10 || Plat is Platform.MUA_PS2 or Platform.MUA_XBOX) && fs.Length > 300E+3);
                FileSize.Text = $"{fs.Length} bytes";

                // Wii (Plat 5) is buggy when attr2 was added with Alchemy 5 and XML2 PSP doesn't support any Alchemy 5. They require native (Alch 3.5) attr2 however.
                BadGeo = PAV > 6 && Plat is not Platform.MUA_PC_2006 && GeometryFormats.Text.Contains("1_5");
                ConvGeo = BadGeo && !(Plat is Platform.XML2_PSP or Platform.MUA_Wii or Platform.MUA_PC_2006);
                BadGeo = BadGeo || (PAV == 6 && GeometryFormats.Text.Contains('2'));
                GeometryFormats.Text = string.Join(", ", GeometryLines.Distinct());
                VertexCount.Text = StatLines.Where(s => s.Contains(" vertex total: ", StringComparison.OrdinalIgnoreCase)).Max(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries)[3]);
                GeometryCount.Text = GeometryLines.Count().ToString();

                string[] IncompatibleTexs = Plat is Platform.MUA_PC_2006 or Platform.XML2_PC
                    ? ["PSP", "GAMECUBE"]
                    : Plat is Platform.MUA_PS2 or Platform.XML2_PS2
                    ? ["PSP", "GAMECUBE", "DXT"]
                    : Plat is Platform.MUA_PSP or Platform.XML2_PSP
                    ? ["GAMECUBE", "DXT"]
                    : Plat is Platform.MUA_Wii
                    ? ["PSP", "_X_"]
                    : Plat is Platform.XML2_GCUB
                    ? ["PSP"]
                    : Plat is Platform.MUA_XOne
                    ? ["PSP", "GAMECUBE", "888", "_X_"]
                    : ["PSP", "GAMECUBE", "_X_"]; // PS3, PS4, Xbox, X360/XENO
                BadTex = TexFormats.Any(t => IncompatibleTexs.Contains(t));
                TextureFormats.Text = string.Join(", ", TexFormats);
                BiggestTexture.Text = BiggestTex is null ? "" : $"{BiggestTex[1].Trim()} x {BiggestTex[2].Trim()}";
                TextureCount.Text = TextureLines.Count().ToString();
                MipMaps.Text = TextureLines.Count(s => s[4].Contains("Mip", StringComparison.OrdinalIgnoreCase) && s[4].Trim()[^1] != '0').ToString();

                igSkinName.Text = igSkin = IsSkin ? Alchemy.IntName(Stats) : "";
                BadSkin = IsSkin && igSkin != Name;
            }
            Cfg.GUI.SkinModName = $"{TargetPath.SelectedItem}"; // only saved for skins
            bool HexEdit = BadSkin && !(string.IsNullOrWhiteSpace(igSkin) || igSkin.StartsWith("Bip01"));
            Msg.SE_Info.Message = $"Replaced '{IGB}'.";
            Msg.SE_Info.IsOpen = File.Exists(IGB);

            bool optimized = (ConvGeo || HexEdit || !GamePath.StartsWith("ui"))
                && Alchemy.CopySkin(SIGB, IGB, Name, PAV, ConvGeo, igSkin, HexEdit);
            BadGeo = !(optimized && ConvGeo) && BadGeo;

            if (!optimized)
            {
                try { _ = SIGB.CopyTo(IGB, true); }
                catch { Msg.SE_Error.Message = $"Failed to install skin to '{IGB}'."; return; }
            }
            optimized = optimized || (HexEdit && Util.HexEdit([igSkin ?? "igActor01Appearance"], Name, IGB));
            if (GamePath == "hud" && Name.Length > 4) { _ = FixHUDs(IGB, Name[^(Name[^5] is '_' ? 4 : 5)..]); }

            if (optimized && HexEdit)
            {
                igSkinName.Text = Name;
                BadSkin = false;
            }
            Microsoft.UI.Xaml.Media.SolidColorBrush Red = new(Microsoft.UI.Colors.Red);
            Microsoft.UI.Xaml.Media.SolidColorBrush Green = new(Microsoft.UI.Colors.Green);
            AlchemyVersion.Foreground = AlchemyVersionT.Foreground = BadAV ? Red : Green;
            igSkinName.Foreground = igSkinNameT.Foreground = BadSkin ? Red : Green;
            GeometryFormats.Foreground = GeometryFormatsT.Foreground = BadGeo ? Red : Green;
            TextureFormats.Foreground = TextureFormatsT.Foreground = BadTex ? Red : Green;
            FileSize.Foreground = FileSizeT.Foreground = BadSize ? Red : Green;
            SkinInfo.Visibility = Visibility.Visible;
            Msg.SE_Warning.Message = $"'{SourceIGB}' is possibly incompatible and can't be optimized or optimization has failed. Check the IGB statistics";
            Msg.SE_Warning.IsOpen = BadAV || BadSkin || BadGeo || BadTex || BadSize;
            ShowSuccess($"Skin installed to '{IGB}'.");
            CheckPackages();
        }
        /// <summary>
        /// Calculate Emma Frosts Diamond Form number for <paramref name="SkinNumber"/>.
        /// </summary>
        /// <returns>Matching Diamond Form number or 99, if invalid.</returns>
        private static int GetDiamondFormNumber(int N)
        {
            int LN = N % 10;
            return LN == 9 && N < 90
                ? N + 1
                : LN is < 5 and > 0
                ? N + 4
                : 99;
        }
        /// <summary>
        /// Load the skin details when a character is selected
        /// </summary>
        private void FloatingCharacter_Changed(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        { if (Cfg.Var.FloatingCharacter is not null && e.PropertyName == "FloatingCharacter") { LoadHerostat(); } }
        /// <summary>
        /// Show the delete button on hover
        /// </summary>
        private void SkinTemplate_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Mouse or Microsoft.UI.Input.PointerDeviceType.Pen && Stats.SkinsList.Count > 1)
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
        /// Add a Skin Slot
        /// </summary>
        private void AddSkinSlot_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Cfg.Var.FloatingCharacter) || Stats.CharNum == "") { return; }
            if (Cfg.GUI.IsXml2) { Stats.FixXML2names(true); } // Should never happen
            else
            {
                Stats.AddSkin(Stats.AnySkins ? Stats.SkinsList.Max(static s => s.Num) + 1 : 0, "");
            }
            Skins.SelectedIndex = Stats.SkinsList.Count - 1;
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
        /// Restrict skin number input (Note: Not using NumberBox, because it can't display "" and it's too wide)
        /// </summary>
        private void SkinNumber_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Length > 2 || args.NewText.Any(static c => !char.IsDigit(c));
        }
        /// <summary>
        /// Ensure two digit number
        /// </summary>
        private void SkinNumber_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (sender is TextBox SN && SN.Text.Length != 0)
            {
                int N = Stats.SkinsList[Skins.SelectedIndex].Num = int.Parse(SN.Text);
                SN.Text = SN.Text.PadLeft(2, '0');
                if (CfgSt.GUI.IsXml2 && Skins.SelectedIndex == 0) { Stats.UpdateMannequinNumber(N); }
            }
        }
        /// <summary>
        /// Save. There is no shortcut for now, as it conflicts with the OHS commands.
        /// </summary>
        private void SaveSkinDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Stats.Save();
                ShowSuccess("Skin information saved.");
                CheckPackages();
            }
            catch (Exception x) { Msg.SE_Error.Message = "Skin information could not be saved. " + x.Message; }
        }
        /// <summary>
        /// Load the installation commands when a skin is selected
        /// </summary>
        private void Skins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SkinInstallerButtons.Visibility = Visibility.Collapsed;
            if (Skins.SelectedItem is SkinDetails Skin && Skin.Num != -1
                && Path.IsPathFullyQualified(Cfg.OHS.GameInstallPath)) // Again, this is not consistent with the relative path that the settings allow.
            {
                SkinInfo.Visibility = Visibility.Collapsed;
                SkinInstallerButtons.Visibility = Visibility.Visible;
                Head3D_Browse.IsEnabled = HudHeadBrowse.IsEnabled = true;
                SelectedSkinNumber.Text = SkinNumber.Text = Skin.Number;
                if (Alchemy.Optimizer is null && !AlchemyWarningClosed) { AlchemyWarning.IsOpen = Alchemy.Reset(); }
            }
        }

        private void Skins_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (Skins.SelectedIndex > -1) // -1 should not be possible
            {
                Stats.RemoveSkinAt(Skins.SelectedIndex);
            }
            args.Handled = true;
        }

        private void Skins_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args) => Stats.FixXML2names();
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
                if (HUD.Exists && TargetIgb("hud", $"hud_head_{Skin.CharNum}{Skin.Number}") is string HUDT)
                {
                    _ = HUD.CopyTo(HUDT, true);
                    _ = FixHUDs(HUDT, $"{Skin.CharNum}{Skin.Number}");
                    HudHeadBrowse.IsEnabled = false;
                }
                if (Cfg.GUI.IsXml2 && H3D.Exists && TargetIgb(Path.Combine("ui", "hud", "characters"), $"{Skin.CharNum}{Skin.Number}") is string H3DT)
                {
                    _ = H3D.CopyTo(H3DT, true);
                    Head3D_Browse.IsEnabled = false;
                }
            }
        }

        private void InstallHuds_Click(object sender, RoutedEventArgs e) => InstallIGB("hud", Prefix: "hud_head_");

        private void Install3DHead_Click(object sender, RoutedEventArgs e) => InstallIGB(Path.Combine("ui", "hud", "characters"));

        private void InstallMannequin_Click(object sender, RoutedEventArgs e)
        {
            InstallIGB(Path.Combine("ui", "models", Cfg.GUI.IsXml2 ? "characters" : Cfg.MUA.MannequinFolder), MannequinNumber.Text);
        }

        private void DiamondForm_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ToolTip Tt && Skins.SelectedItem is SkinDetails Skin)
            {
                Tt.Content = $"Browse for Emma Frost's Diamond Form skin to install as {Skin.CharNum}{GetDiamondFormNumber(Skin.Num):00}.igb - Works for other characters that use this system";
            }
        }

        private void FlameOn_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ToolTip Tt && Skins.SelectedItem is SkinDetails Skin)
            {
                Tt.Content = $"Browse for Human Torch's Flame On skin to install as {Skin.CharNum}{Skin.Num + 10}.igb - Works for other characters that use this system";
            }
        }

        private void InstallEmmaSkin_Click(object sender, RoutedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin && GetDiamondFormNumber(Skin.Num) is int N && N < 99)
            {
                InstallIGB("actors", $"{N:00}");
            }
        }

        private void InstallHumanTorch_Click(object sender, RoutedEventArgs e)
        {
            if (Skins.SelectedItem is SkinDetails Skin && Skin.Num + 10 is int N && N < 100)
            {
                InstallIGB("actors", $"{N}");
            }
        }

        private async void InstallExtraSkin_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await EnterSkinNumber.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(ExtraSkinNumber.Text))
            {
                InstallIGB("actors", ExtraSkinNumber.Text);
            }
        }
        /// <summary>
        /// Show a success <paramref name="message"/> in an <see cref="InfoBar"/> for 5 seconds.
        /// </summary>
        private async void ShowSuccess(string message)
        {
            Msg.SE_Success.Message = message;
            await System.Threading.Tasks.Task.Delay(5000);
            Msg.SE_Success.IsOpen = false;
        }

        private void SkinDetails_Unloaded(object sender, RoutedEventArgs e)
        {
            Cfg.Var.PropertyChanged -= FloatingCharacter_Changed;
        }

        private void AlchemyWarning_CloseButtonClick(InfoBar sender, object args)
        {
            AlchemyWarningClosed = true;
        }
    }
}
