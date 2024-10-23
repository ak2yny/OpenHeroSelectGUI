using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Settings page for common settings
    /// </summary>
    public partial class Tab_Settings : Page
    {
        public ObservableCollection<string> SaveBackups { get; } = [];
        public Cfg Cfg { get; set; } = new();
        public string[] DataExt = [
            "English (engb)",
            "Italian (itab)",
            "French (freb)",
            "Spanish (spab)",
            "German (gerb)",
            "Russian (rusb)",
            "Polish (polb)",
            "None (xmlb)"
        ];

        public Tab_Settings()
        {
            InitializeComponent();
            ReadSaveBackups();
            PrepareSettings();
            GIPcheck();
        }
        /// <summary>
        /// Load the GUI of the current page with information from the settings (conditional settings).
        /// </summary>
        private void PrepareSettings()
        {
            GIPBox.Text = Cfg.GUI.GameInstallPath != "" ? Cfg.GUI.GameInstallPath : Cfg.OHS.GameInstallPath;
            if (Cfg.GUI.Game == "XML2" && RosterSizeToggle.Children.FirstOrDefault(r => r is RadioButton R
                && R.Content.ToString()?[..2] == $"{Cfg.XML2.RosterSize}") is RadioButton RB)
            {
                RB.IsChecked = true;
            }
            RHCard.Visibility = Cfg.Var.IsMua && Cfg.GUI.IsNotConsole ? Visibility.Visible : Visibility.Collapsed;
        }
        /// <summary>
        /// Load a list of all backed up saves.
        /// </summary>
        private void ReadSaveBackups()
        {
            SaveBackups.Clear();
            if (new DirectoryInfo(OHSpath.SaveFolder) is DirectoryInfo SF && SF.Exists)
            {
                foreach (DirectoryInfo f in SF.GetDirectories())
                {
                    if (!(f.Name is "Save" or "Screenshots")) SaveBackups.Add(f.Name);
                }
            }
        }
        /// <summary>
        /// If RH option is on, checks for the roster hack in the game folder and updates the RH message.
        /// </summary>
        private void UpdateRH()
        {
            RHInfo.IsOpen = false;
            if (RosterHackToggle.IsOn && Cfg.GUI.Game != "XML2")
            {
                string GamePath = Path.GetDirectoryName(OHSpath.MUAexe) ?? Cfg.OHS.GameInstallPath;
                string dinput = Path.Combine(GamePath, "dinput8.dll");
                if (File.Exists(dinput)) { try { dinput = File.ReadAllText(dinput); } catch { dinput = ""; } }
                RHInfo.Message = $"Roster hack (RH) not detected in '{GamePath}'. This message can be ignored, if the RH's installed in the actual game folder or if detection failed for another reason. MO2 users can browse for the actual game .exe path below, to keep this message from opening in the future. The RH fixes a crash when using more than 27 characters.";
                RHExpander.IsExpanded = RHInfo.IsOpen = !Directory.Exists(Path.Combine(GamePath, "plugins")) || !dinput.Contains("asi-loader");
            }
            Cfg.GUI.IsMo2 = Cfg.GUI.ActualGameExe != "" || InternalSettings.KnownModOrganizerExes.Contains(Cfg.OHS.ExeName, StringComparer.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Trim a <see cref="string"/> (<paramref name="Trim"/>) from the end of another <see cref="string"/> (<paramref name="inputText"/>), if found. By Shane Yu @ StackOverflow
        /// </summary>
        /// <returns>Trimmed <paramref name="inputText"/></returns>
        private static string TrimEnd(string inputText, string Trim, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
        {
            if (!string.IsNullOrEmpty(Trim))
            {
                while (!string.IsNullOrEmpty(inputText) && inputText.EndsWith(Trim, comparisonType))
                {
                    inputText = inputText[..^Trim.Length];
                }
            }
            return inputText;
        }
        /// <summary>
        /// Verifies the <paramref name="FixedLengthFN"/> filename for a fixed length defined by a <paramref name="Fallback"/> <see cref="string"/>. Must have same extension as <paramref name="Fallback"/>.
        /// </summary>
        /// <returns>The <paramref name="FixedLengthFN"/> (with <paramref name="Fallback"/> extension), or the <paramref name="Fallback"/> string if verification fails</returns>
        private static string FixedLength(string FixedLengthFN, string Fallback)
        {
            string NewName = Fallback.LastIndexOf('.') is int Dot && Dot > 0 && Fallback[Dot..] is string Ext
                ? TrimEnd(FixedLengthFN, Ext) + Ext
                : FixedLengthFN;
            return NewName.Length > Fallback.Length || string.IsNullOrWhiteSpace(FixedLengthFN) ? Fallback : NewName;
        }

        private async void MO2Browse()
        {
            if (await CfgCmd.BrowseFolder() is string GIP && GIP != "")
            {
                Cfg.OHS.GameInstallPath = GIP;
                GIPcheck();
            }
        }

        private void GIPcheck()
        {
            Warning.Message = (Cfg.OHS.GameInstallPath == "" ? "" : $"No 'data' folder in '{Cfg.OHS.GameInstallPath}'. ") + "Please browse for a valid game installation path or mod folder.";
            Warning.IsOpen = !Directory.Exists(Path.Combine(Cfg.OHS.GameInstallPath, "data"));
            UpdateRH();
        }
        // UI control handlers:
        private async void ExeBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (await CfgCmd.LoadDialogue(".exe") is string Exe && Exe != "")
            {
                Cfg.OHS.ExeName = Path.GetFileName(Exe);
                GIPBox.Text = Cfg.GUI.GameInstallPath = Path.GetDirectoryName(Exe) ?? "";
                if (Cfg.OHS.GameInstallPath == "")
                {
                    Cfg.OHS.GameInstallPath = Cfg.GUI.GameInstallPath;
                    GIPcheck();
                }
            }
        }

        private void Warning_CloseButtonClick(InfoBar sender, object args)
        {
            MO2Browse();
        }

        private void MO2BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            MO2Browse();
        }

        private async void HBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Cfg.OHS.HerostatFolder = OHSpath.TrimGameFolder(await CfgCmd.BrowseFolder());
        }

        private void FreeSavesButton_Click(object sender, RoutedEventArgs e)
        {
            SaveBkpFailed.IsOpen = !OHSpath.BackupSaves();
        }

        private void RestoreSaves_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RestoreSaves.SelectedValue != null)
            {
                string? Save = RestoreSaves.SelectedValue.ToString();
                if (!string.IsNullOrEmpty(Save))
                {
                    _ = OHSpath.MoveSaves("Save", $"AutoBackup-{DateTime.Now:yyMMdd-HHmmss}");
                    _ = OHSpath.MoveSaves(Save, "Save");
                    ReadSaveBackups();
                }
            }
        }

        private void OpenSaves_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(OHSpath.SaveFolder))
            {
                _ = Process.Start("explorer.exe", $"{OHSpath.SaveFolder}");
            }
        }

        private void RefreshSaves_Click(object sender, RoutedEventArgs e)
        {
            ReadSaveBackups();
        }
        /// <summary>
        /// Roster Hack switch: A message warns us about potentially missing roster hack files.
        /// </summary>
        private void RosterHack_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateRH();
        }
        /// <summary>
        /// Browse for the game folder with the roster hack, if using MO2.
        /// </summary>
        private async void BrowseRH_Click(object sender, RoutedEventArgs e)
        {
            Cfg.GUI.ActualGameExe = await CfgCmd.LoadDialogue(".exe") ?? "";
            UpdateRH();
        }
        /// <summary>
        /// Roster Size toggle: Set the observable settings from code behind.
        /// </summary>
        private void RS_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton RB
                && RB.Content.ToString() is string RS
                && int.TryParse(RS[..2], out int Limit))
            {
                Cfg.Roster.Total = Cfg.XML2.RosterSize = Limit;
                Cfg.Var.RosterRange = Enumerable.Range(1, Limit);
            }
        }

        private void Herostat_TextChanged(UIElement sender, LosingFocusEventArgs args)
        {
            HerostatName.Text = FixedLength(HerostatName.Text, "herostat");
            Cfg.OHS.HerostatName = $"{HerostatName.Text}.{DataExt[LanguageCode.SelectedIndex][^5..^1]}";
        }

        private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (LanguageCode.SelectedIndex > -1) { return; }
            Cfg.OHS.HerostatName = $"{HerostatName.Text}.{DataExt[LanguageCode.SelectedIndex][^5..^1]}";
        }

        private void NewGamePy_TextChanged(UIElement sender, LosingFocusEventArgs args)
        {
            NewGamePyName.Text = FixedLength(NewGamePyName.Text, "new_game.py");
        }

        private void CharHead_TextChanged(UIElement sender, LosingFocusEventArgs args)
        {
            CharactersHeadsPackageName.Text = FixedLength(CharactersHeadsPackageName.Text, "characters_heads.pkgb");
        }

        private void MannequinFolder_TextChanged(UIElement sender, LosingFocusEventArgs args)
        {
            MannequinFolder.Text = FixedLength(MannequinFolder.Text, "mannequin");
        }

        private void Charinfo_TextChanged(UIElement sender, LosingFocusEventArgs args)
        {
            CharinfoName.Text = FixedLength(CharinfoName.Text, "charinfo.xmlb");
        }

        private void TeamBonus_TextChanged(UIElement sender, LosingFocusEventArgs args)
        {
            TeamBonusName.Text = FixedLength(TeamBonusName.Text, "team_bonus");
        }
        /// <summary>
        /// Load the GUI configurations and set-up the controls for this control.
        /// </summary>
        private void SettingsCard_Loaded(object sender, RoutedEventArgs e)
        {
            if (Cfg.OHS.HerostatName is string HS && HS.LastIndexOf('.') is int DH && DH > 0 && DH < (HS.Length - 3))
            {
                HerostatName.Text = HS.Remove(DH);
                LanguageCode.SelectedIndex = Array.FindIndex(DataExt, w => w.IndexOf(HS[(DH + 1)..], StringComparison.CurrentCultureIgnoreCase) > -1);
            }
        }

        private void ResetPC_Click(object sender, RoutedEventArgs e)
        {
            ResetSettings();
            ExeBrowseButton_Click(sender, e);
        }

        private void ResetMO2_Click(object sender, RoutedEventArgs e)
        {
            ResetSettings();
            CfgSt.GUI.IsMo2 = Cfg.GUI.IsMo2 = true;
            MO2Browse();
        }

        private void ResetConsoles_Click(object sender, RoutedEventArgs e)
        {
            ResetSettings();
            Cfg.GUI.CopyStage = Cfg.GUI.FreeSaves = Cfg.GUI.IsNotConsole = false;
            Cfg.XML2.RosterSize = 19;
            RHCard.Visibility = Visibility.Collapsed;
            MO2Browse();
        }
        /// <summary>
        /// Reflect the settings from their default values
        /// </summary>
        private void ResetSettings()
        {
            bool MUA = Cfg.GUI.Game != "XML2";
            bool AC = Cfg.GUI.AvailChars;
            string Home = Cfg.GUI.Home;
            CfgSt.Var = new VariableSettings();
            CfgSt.Roster.Selected.Clear();
            GUIObject.Copy(new GUIsettings() { Game = MUA ? "MUA" : "XML2", Home = Home, AvailChars = AC }, Cfg.GUI);
            if (MUA) { GUIObject.Copy(new MUAsettings(), Cfg.MUA); }
            else { GUIObject.Copy(new XML2settings(), Cfg.XML2); }
            CfgSt.Roster.NumClash = CfgSt.GUI.IsMo2 = Cfg.GUI.IsMo2 = false;
            PrepareSettings();
        }
    }
}
