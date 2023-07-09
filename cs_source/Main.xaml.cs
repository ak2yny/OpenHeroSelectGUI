using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;
using static OpenHeroSelectGUI.Settings.CharacterLists;
using static OpenHeroSelectGUI.Settings.InternalSettings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The main window contains the banner and navigation panel, as well as common elements.
    /// </summary>
    public sealed partial class Main : Window
    {
        public Main()
        {
            Activated += MainWindow_Activated;
            Cfg.LoadGuiSettings();
            Cfg.LoadOHSsettings();
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            //AppTitleTextBlock.Text = AppInfo.Current.DisplayInfo.DisplayName;
            //AppWindow.Resize(new SizeInt32(1150, 640));
        }
        /// <summary>
        /// Make the title dim when the app is not in focus
        /// </summary>
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            AppTitleTextBlock.Foreground = args.WindowActivationState == WindowActivationState.Deactivated
                ? new SolidColorBrush(Colors.Black)
                : new SolidColorBrush(Colors.AliceBlue);
        }
        /// <summary>
        /// Navigation View: Determine the selected page, when not already selected.
        /// </summary>
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected == true)
            {
                NavView_Navigate(typeof(Tab_Settings), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.SelectedItemContainer != null && args.SelectedItemContainer.Tag.ToString() is string TagName && Type.GetType(TagName) is Type navPageType)
            {
                if (navPageType == typeof(Tab_XML2) && GUIsettings.Instance.Game == "mua")
                {
                    Cfg.SaveSettings();
                    GUIsettings.Instance.Game = "xml2";
                    Cfg.LoadOHSsettings();
                    LoadRosterXML2();
                }
                else if (navPageType == typeof(Tab_MUA) && GUIsettings.Instance.Game == "xml2")
                {
                    Cfg.SaveSettings();
                    GUIsettings.Instance.Game = "mua";
                    Cfg.LoadOHSsettings();
                    LoadRosterMUA();
                }
                GUIsettings.Instance.Home = sender.MenuItems.IndexOf(args.SelectedItem);
                NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo);
            }
        }
        /// <summary>
        /// Navigation View: Initial commands. Load home page.
        /// </summary>
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[GUIsettings.Instance.Home];
        }
        /// <summary>
        /// Navigation View: Change the page/tab according to the selection.
        /// </summary>
        private void NavView_Navigate(Type navPageType, NavigationTransitionInfo transitionInfo)
        {
            // Get the page type before navigation so you can prevent duplicate entries in the backstack.
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null && !Equals(preNavPageType, navPageType))
            {
                ContentFrame.Navigate(navPageType, null, transitionInfo);
            }
        }
        /// <summary>
        /// Save dialogue.
        /// This seems useless: string n = file.DisplayName; if (n.EndsWith(file.FileType)) { n = n[..(n.Length - file.FileType.Length)]; }
        /// </summary>
        private async void SaveDialogue()
        {
            FileSavePicker savePicker = new();
            savePicker.FileTypeChoices.Add("Configuration File", new List<string>() { ".ini" });
            // window can be defined outside of void?
            Window window = new();
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WinRT.Interop.WindowNative.GetWindowHandle(window));
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                Cfg.SaveSettings(file.Path);
                GenerateCfgFiles(file.DisplayName);
            }
        }
        /// <summary>
        /// Generate the cfg files with the saved names.
        /// </summary>
        private void GenerateCfgFiles() => GenerateCfgFiles(OHSsettings.Instance.RosterValue, MUAsettings.Instance.MenulocationsValue);
        /// <summary>
        /// Generate the cfg files providing a roster value (filename). (MUA uses identical menulocation name.)
        /// </summary>
        private void GenerateCfgFiles(string rv) => GenerateCfgFiles(rv, rv);
        /// <summary>
        /// Generate the cfg files providing independent menulocation and roster values (filenames).
        /// </summary>
        private void GenerateCfgFiles(string rv, string mv)
        {
            if (Instance.Selected != null && Instance.Selected.Count > 0)
            {
                if (GUIsettings.Instance.Game == "mua" && !IsDefaultMV(mv))
                {
                    WriteCfg(Path.Combine(cdPath, "mua", "menulocations", $"{mv}.cfg"), 0);
                }
                if (!IsDefaultRV(rv))
                {
                    WriteCfg(Path.Combine(cdPath, GUIsettings.Instance.Game, "rosters", $"{rv}.cfg"), 2);
                }
            }
        }
        /// <summary>
        /// Write the OHS cfg files from the selected characters list.
        /// </summary>
        private void WriteCfg(string path, int p)
        {
            string? CFGpath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(CFGpath)) return;
            Directory.CreateDirectory(CFGpath);
            File.Create(path).Dispose();
            using StreamWriter sw = File.AppendText(path);
            for (int i = 0; i < Instance.Selected.Count; i++)
            {
                SelectedCharacter c = Instance.Selected[i];
                string? line = (p == 0) ?
                    c.Loc :
                    c.Starter ? $"*{c.Path}" : c.Unlock ? $"?{c.Path}" : c.Path;
                sw.WriteLine(line);
            }
        }
        /// <summary>
        /// Run OpenHeroSelect with the specified settings and backup saves if enabled.
        /// </summary>
        private void RunOHS()
        {
            InstallStage();
            if (File.Exists(Path.Combine(cdPath, "OpenHeroSelect.exe")))
            {
                string DP = Path.Combine(OHSsettings.Instance.GameInstallPath, "data");
                if (Directory.Exists(DP))
                {
                    Cfg.SaveSettings();
                    GenerateCfgFiles();
                    string arg = (GUIsettings.Instance.Game == "xml2") ?
                        "-a -x" :
                        "-a";
                    Util.RunElevated("OpenHeroSelect.exe", arg);
                    string elog = Path.Combine(cdPath, "error.log");
                    if (File.Exists(elog))
                    {
                        Process.Start("explorer.exe", $"/select, \"{elog}\"");
                    }
                }
                else
                {
                    ShowWarning($"Installation path not valid!\r\n'{DP}' not found.");
                }
            }
            else
            {
                ShowWarning("OpenHeroSelect.exe not found!");
            }
        }
        private void InstallStage()
        {
            string StagesPath = Path.Combine(cdPath, "stages");
            string RiserPath = Path.Combine(StagesPath, ".riser");
            string SelectedLayoutPath = Path.Combine(StagesPath, GUIsettings.Instance.Layout);
            CopyStageFile(DynamicSettings.Instance.SelectedModelPath, Path.Combine("ui", "models"), "m_team_stage.igb");
            CopyStageFile(SelectedLayoutPath, Path.Combine("ui", "menus"), "mlm_team_back.igb");
            CopyStageFile(SelectedLayoutPath, Path.Combine("ui", "menus"), "team_back.xmlb");
            if (DynamicSettings.Instance.Riser)
            {
                CopyRiserFile(RiserPath, Path.Combine("effects", "menu"), "riser.xmlb");
                CopyRiserFile(RiserPath, Path.Combine("models", "effects"), "riser.igb");
                CopyRiserFile(RiserPath, Path.Combine("packages", "generated", "maps", "package", "menus"), "team_back.pkgb");
            }
            else
            {
                CopyRiserFile(RiserPath, Path.Combine("packages", "generated", "maps", "package", "menus"), "team_back.bkp.pkgb", "team_back.pkgb");
            }
        }
        private void CopyStageFile(string folder, string GameFolder, string file) => CopyStageFile(folder, GameFolder, file, file);
        private void CopyStageFile(string folder, string GameFolder, string Source, string Target)
        {
            string TP = Path.Combine(OHSsettings.Instance.GameInstallPath, GameFolder);
            string SF = Path.Combine(folder, Source);
            if (folder != "" && File.Exists(SF))
            {
                Directory.CreateDirectory(TP);
                File.Copy(SF, Path.Combine(TP, Target), true );
            }
        }
        private void CopyRiserFile(string RiserPath, string GameFolder, string Source, string Target) => CopyStageFile(Path.Combine(RiserPath, GameFolder), GameFolder, Source, Target);
        private void CopyRiserFile(string RiserPath, string GameFolder, string file) => CopyStageFile(Path.Combine(RiserPath, GameFolder), GameFolder, file, file);
        /// <summary>
        /// Show a warning dialogue box.
        /// </summary>
        private async void ShowWarning(string message)
        {
            WarningDialog.Content = message;
            await WarningDialog.ShowAsync();
        }
        /// <summary>
        /// Run OpenHeroSelect with the specified settings and backup saves if enabled.
        /// </summary>
        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (GUIsettings.Instance.FreeSaves && OHSsettings.Instance.LaunchGame) Tab_Settings.MoveSaves("Save", $"{DateTime.Now:yyMMdd-HHmmss}");
            RunOHS();
            // if this waits until the game has closed (OHS doesn't wait AFAIK) then we can restore the saves after
        }
        /// <summary>
        /// Save settings to the default location, using config values for the CFG files.
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Cfg.SaveSettings();
            GenerateCfgFiles();
        }
        /// <summary>
        /// Save settings by defining an INI file.
        /// </summary>
        private void BtnSaveAs_Click(object sender, RoutedEventArgs e) => SaveDialogue();
        /// <summary>
        /// Load settings by picking an INI file.
        /// </summary>
        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            string? IniPath = await Cfg.LoadDialogue("ini");
            if (IniPath != null)
            {
                Cfg.LoadSettings(IniPath);
                if (GUIsettings.Instance.Game == "xml2")
                {
                    LoadRosterXML2();
                }
                else
                {
                    LoadRosterMUA();
                }
            }
        }
        /// <summary>
        /// Save to the default settings files, when the window closes (no roster).
        /// </summary>
        private void Window_Closed(object sender, WindowEventArgs args) => Cfg.SaveSettings();
    }
}
