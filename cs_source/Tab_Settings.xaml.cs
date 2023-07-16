using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using static OpenHeroSelectGUI.Settings.CfgCommands;
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Settings page for common settings
    /// </summary>
    public partial class Tab_Settings : Page
    {
        public ObservableCollection<string> SaveBackups { get; } = new();
        public string? Herostat { get; set; }
        public Settings.Cfg Cfg { get; set; } = new();
        public Tab_Settings()
        {
            InitializeComponent();
            LoadCfg();
            ReadSaveBackups();
        }
        /// <summary>
        /// Load the GUI configurations and set-up the controls. WIP big time.
        /// </summary>
        private void LoadCfg()
        {
            if (Cfg.OHS != null && Cfg.OHS.HerostatName != null && Cfg.OHS.ExeName != null)
            {
                int Dot = Cfg.OHS.HerostatName.LastIndexOf(".");
                Herostat = Cfg.OHS.HerostatName.Remove(Dot);
                LanguageCode.SelectedItem = LanguageCode.FindName(Cfg.OHS.HerostatName.Substring(Dot + 1, 3));
                Dot = Cfg.OHS.ExeName.LastIndexOf(".");
                ExeName.Text = (Dot > 0) ? Cfg.OHS.ExeName.Remove(Dot) : (Cfg.Dynamic.Game == "xml2") ? "Xmen" : "Game";
            }
        }
        /// <summary>
        /// Load a list of all backed up saves.
        /// </summary>
        private void ReadSaveBackups()
        {
            SaveBackups.Clear();
            foreach (var f in Directory.GetDirectories(GetSaveFolder()))
            {
                string Save = Path.GetFileName(f);
                if (!(Save is "Save" or "Screenshots")) SaveBackups.Add(Save);
            }
        }
        /// <summary>
        /// Open folder dialogue. Currently there is no other page using it, so it's still here.
        /// </summary>
        private static async Task<string> BrowseFolder()
        {
            FolderPicker folderPicker = new();
            folderPicker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                return folder.Path;
            }
            return "";
        }
        /// <summary>
        /// Trim a string from the end of a string, if found. By Shane Yu @ StackOverflow
        /// </summary>
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
        /// Verifies the FixedLengthFN filename for a fixed length defined by a Fallback string.
        /// </summary>
        /// <returns>The FixedLengthFN with extension, or the Fallback string if verification fails</returns>
        private static string FixedLength(string FixedLengthFN, string Fallback)
        {
            string Ext = Fallback[Fallback.LastIndexOf(".")..];
            string NewName = TrimEnd(FixedLengthFN, Ext) + Ext;
            if (NewName.Length != Fallback.Length)
            {
                NewName = Fallback;
            }
            return NewName;
        }
        /// <summary>
        /// Show a warning dialogue box.
        /// </summary>
        private async void ShowWarning(string message)
        {
            WarningDialog.Content = message;
            await WarningDialog.ShowAsync();
        }
        // UI control handlers:
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Cfg.GUI.GameInstallPath = await BrowseFolder();
            if (!File.Exists(Path.Combine(Cfg.GUI.GameInstallPath, Cfg.OHS.ExeName)))
            {
                ShowWarning($"No EXE found in '{Cfg.GUI.GameInstallPath}'");
            }
            if (string.IsNullOrEmpty(Cfg.OHS.GameInstallPath))
            {
                Cfg.OHS.GameInstallPath = Cfg.GUI.GameInstallPath;
            }
        }
        private async void MO2BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Cfg.OHS.GameInstallPath = await BrowseFolder();
        }
        private void MO2ModFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Directory.Exists(Path.Combine(Cfg.OHS.GameInstallPath, "data")))
            {
                ShowWarning($"No data folder in '{Cfg.OHS.GameInstallPath}'");
            }
        }
        private async void HBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string Hsf = await BrowseFolder();
            string Game = Path.Combine(cdPath, Cfg.Dynamic.Game);
            Cfg.OHS.HerostatFolder = Hsf.StartsWith(Game) ?
                Hsf[(Game.Length + 1)..] :
                Hsf;
        }

        private void FreeSavesButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSaves("Save", $"{DateTime.Now:yyMMdd-HHmmss}");
        }

        private void RestoreSaves_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RestoreSaves.SelectedValue != null)
            {
                string? Save = RestoreSaves.SelectedValue.ToString();
                if (!string.IsNullOrEmpty(Save))
                {
                    MoveSaves("Save", $"AutoBackup-{DateTime.Now:yyMMdd-HHmmss}");
                    MoveSaves(Save, "Save");
                    ReadSaveBackups();
                }
            }
        }

        private void OpenSaves_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", @$"{GetSaveFolder()}");
        }

        private void RefreshSaves_Click(object sender, RoutedEventArgs e)
        {
            ReadSaveBackups();
        }

        private void EXE_TextChanged(object sender, TextChangedEventArgs e)
        {
            ExeName.Text = TrimEnd(ExeName.Text, ".exe");
            Cfg.OHS.ExeName = $"{ExeName.Text}.exe";
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
    }
}
