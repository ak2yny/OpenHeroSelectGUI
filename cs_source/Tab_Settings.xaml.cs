using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

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
        }
        /// <summary>
        /// Load the GUI configurations and set-up the controls.
        /// </summary>
        private void LoadCfg()
        {
            if (Cfg.OHS != null)
            {
                if (Cfg.OHS.HerostatName is string HS && HS.LastIndexOf('.') is int DH && DH > 0 && DH < (HS.Length - 3))
                {
                    HerostatName.Text = HS.Remove(DH);
                    LanguageCode.SelectedIndex = Array.FindIndex(DataExt, w => w.Contains(HS[(DH + 1)..]));
                }
            }
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
        // UI control handlers:
        private async void ExeBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (await CfgCmd.LoadDialogue(".exe") is string Exe)
            {
                FileInfo? ExeFile = new(Exe);
                Cfg.OHS.ExeName = ExeFile.Name;
                Cfg.GUI.GameInstallPath = ExeFile.FullName[..(ExeFile.FullName.Length - ExeFile.Name.Length - 1)];
                if (string.IsNullOrEmpty(Cfg.OHS.GameInstallPath))
                {
                    Cfg.OHS.GameInstallPath = Cfg.GUI.GameInstallPath;
                }
            }
        }
        private async void MO2BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Warning.IsOpen = false;
            Cfg.OHS.GameInstallPath = await CfgCmd.BrowseFolder();
        }
        private void MO2ModFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Directory.Exists(Path.Combine(Cfg.OHS.GameInstallPath, "data")))
            {
                Warning.Message = $"No 'data' folder in '{Cfg.OHS.GameInstallPath}'";
                Warning.IsOpen = true;
            }
        }
        private async void HBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Cfg.OHS.HerostatFolder = OHSpath.TrimGameFolder(await CfgCmd.BrowseFolder());
        }

        private void FreeSavesButton_Click(object sender, RoutedEventArgs e)
        {
            OHSpath.BackupSaves();
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
                _ = Process.Start("explorer.exe", @$"{OHSpath.SaveFolder}");
            }
        }

        private void RefreshSaves_Click(object sender, RoutedEventArgs e)
        {
            ReadSaveBackups();
        }

        private void Herostat_TextChanged(UIElement sender, LosingFocusEventArgs args)
        {
            HerostatName.Text = FixedLength(HerostatName.Text, "herostat");
            Cfg.OHS.HerostatName = HerostatName.Text + DataExt[LanguageCode.SelectedIndex][^5..^1];
        }

        private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (LanguageCode.SelectedIndex > -1) { return; }
            Cfg.OHS.HerostatName = HerostatName.Text + DataExt[LanguageCode.SelectedIndex][^5..^1];
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

        private void SettingsCard_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCfg();
        }
    }
}
