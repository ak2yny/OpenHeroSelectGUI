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
        public string? Herostat { get; set; }
        public Cfg Cfg { get; set; } = new();

        public Tab_Settings()
        {
            InitializeComponent();
            LoadCfg();
            ReadSaveBackups();
        }
        /// <summary>
        /// Load the GUI configurations and set-up the controls.
        /// </summary>
        private void LoadCfg()
        {
            if (Cfg.OHS != null && Cfg.OHS.HerostatName != null && Cfg.OHS.ExeName != null)
            {
                int Dot = Cfg.OHS.HerostatName.LastIndexOf('.');
                Herostat = Cfg.OHS.HerostatName.Remove(Dot);
                LanguageCode.SelectedItem = LanguageCode.FindName(Cfg.OHS.HerostatName.Substring(Dot + 1, 3));
                Dot = Cfg.OHS.ExeName.LastIndexOf('.');
                ExeName.Text = (Dot > 0) ? Cfg.OHS.ExeName.Remove(Dot) : Cfg.GUI.Game == "xml2" ? "Xmen" : "Game";
            }
        }
        /// <summary>
        /// Load a list of all backed up saves.
        /// </summary>
        private void ReadSaveBackups()
        {
            SaveBackups.Clear();
            foreach (var f in Directory.GetDirectories(OHSpath.SaveFolder))
            {
                string Save = Path.GetFileName(f);
                if (!(Save is "Save" or "Screenshots")) SaveBackups.Add(Save);
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
            int Dot = Fallback.LastIndexOf('.');
            string Ext = Dot == -1 ? "" : Fallback[Dot..];
            FixedLengthFN = TrimEnd(FixedLengthFN, Ext) + Ext;
            return FixedLengthFN.Length == Fallback.Length ? FixedLengthFN : Fallback;
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
                    OHSpath.MoveSaves("Save", $"AutoBackup-{DateTime.Now:yyMMdd-HHmmss}");
                    OHSpath.MoveSaves(Save, "Save");
                    ReadSaveBackups();
                }
            }
        }

        private void OpenSaves_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start("explorer.exe", @$"{OHSpath.SaveFolder}");
        }

        private void RefreshSaves_Click(object sender, RoutedEventArgs e)
        {
            ReadSaveBackups();
        }

        private void Herostat_TextChanged(UIElement sender, LosingFocusEventArgs args)
        {
            if (HerostatName.Text.Length != 8)
            {
                HerostatName.Text = "herostat";
            }
            Cfg.OHS.HerostatName = $"{HerostatName.Text}.{((ComboBoxItem)LanguageCode.SelectedItem).Name}b";
        }

        private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Cfg.OHS.HerostatName = $"{HerostatName.Text}.{((ComboBoxItem)LanguageCode.SelectedItem).Name}b";
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
            if (MannequinFolder.Text.Length != "mannequin".Length)
            {
                MannequinFolder.Text = "mannequin";
            }
        }

        private void Charinfo_TextChanged(UIElement sender, LosingFocusEventArgs args)
        {
            CharinfoName.Text = FixedLength(CharinfoName.Text, "charinfo.xmlb");
        }

        private void TeamBonus_TextChanged(UIElement sender, LosingFocusEventArgs args)
        {
            TeamBonusName.Text = FixedLength(TeamBonusName.Text, "team_bonus");
        }
    }
}
