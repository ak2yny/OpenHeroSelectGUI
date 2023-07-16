using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static OpenHeroSelectGUI.Settings.CfgCommands;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;
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
            LoadGuiSettings();

            InitializeComponent();

            AppWindow m_AppWindow = GetAppWindowForCurrentWindow();
            m_AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            //AppTitleTextBlock.Text = AppInfo.Current.DisplayInfo.DisplayName;
            //AppWindow.Resize(new SizeInt32(1150, 640));
        }
        /// <summary>
        /// Get the app window (this is not the main window or any other window)
        /// </summary>
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
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
                if (navPageType != typeof(Tab_Info))  // exclude footer pages from becoming home
                {
                    if (navPageType == typeof(Tab_XML2) && DynamicSettings.Instance.Game != "xml2")
                    {
                        if (DynamicSettings.Instance.Game == "mua") SaveSettings();
                        DynamicSettings.Instance.Game = "xml2";
                        LoadRoster();
                    }
                    else if (navPageType == typeof(Tab_MUA) && DynamicSettings.Instance.Game != "mua")
                    {
                        if (DynamicSettings.Instance.Game == "xml2") SaveSettings();
                        DynamicSettings.Instance.Game = "mua";
                        LoadRoster();
                    }
                    GUIsettings.Instance.Home = sender.MenuItems.IndexOf(args.SelectedItem);
                }
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
                    SaveSettings();
                    string arg = (DynamicSettings.Instance.Game == "xml2") ?
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
        /// <summary>
        /// Show a warning dialogue box.
        /// </summary>
        private async void ShowWarning(string message)
        {
            WarningDialog.Content = message;
            await WarningDialog.ShowAsync();
        }
        private static void InstallStage()
        {
            string StagesPath = Path.Combine(cdPath, "stages");
            string RiserPath = Path.Combine(StagesPath, ".riser");
            string SelectedLayoutPath = Path.Combine(StagesPath, GUIsettings.Instance.Layout);
            DirectoryInfo ModelFolder = new(DynamicSettings.Instance.SelectedModelPath);
            if (ModelFolder.Exists && ModelFolder.EnumerateFiles("*.igb").Any())
            {
                CopyStageFile(DynamicSettings.Instance.SelectedModelPath, Path.Combine("ui", "models"), ModelFolder.EnumerateFiles("*.igb").First().Name);
            }
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
        private static void CopyStageFile(string folder, string GameFolder, string file) => CopyStageFile(folder, GameFolder, file, file);
        private static void CopyStageFile(string folder, string GameFolder, string Source, string Target)
        {
            string TP = Path.Combine(OHSsettings.Instance.GameInstallPath, GameFolder);
            string SF = Path.Combine(folder, Source);
            if (folder != "" && File.Exists(SF))
            {
                Directory.CreateDirectory(TP);
                File.Copy(SF, Path.Combine(TP, Target), true );
            }
        }
        private static void CopyRiserFile(string RiserPath, string GameFolder, string Source, string Target) => CopyStageFile(Path.Combine(RiserPath, GameFolder), GameFolder, Source, Target);
        private static void CopyRiserFile(string RiserPath, string GameFolder, string file) => CopyStageFile(Path.Combine(RiserPath, GameFolder), GameFolder, file, file);
        /// <summary>
        /// Save dialogue.
        /// This seems useless: string n = file.DisplayName; if (n.EndsWith(file.FileType)) { n = n[..(n.Length - file.FileType.Length)]; }
        /// </summary>
        private static async void SaveDialogue()
        {
            FileSavePicker savePicker = new();
            savePicker.FileTypeChoices.Add("Configuration File", new List<string>() { ".ini" });
            InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(App.MainWindow));
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                MUAsettings.Instance.MenulocationsValue = OHSsettings.Instance.RosterValue = file.DisplayName;
                SaveSettings(file.Path, file.DisplayName);
            }
        }
        /// <summary>
        /// Run OpenHeroSelect with the specified settings and backup saves if enabled.
        /// </summary>
        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (GUIsettings.Instance.FreeSaves && OHSsettings.Instance.LaunchGame) MoveSaves("Save", $"{DateTime.Now:yyMMdd-HHmmss}");
            RunOHS();
            // if this waits until the game has closed (OHS doesn't wait AFAIK) then we can restore the saves after
        }
        /// <summary>
        /// Save settings to the default location, using config values for the CFG files.
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e) => SaveSettings();
        /// <summary>
        /// Save settings by defining an INI file.
        /// </summary>
        private void BtnSaveAs_Click(object sender, RoutedEventArgs e) => SaveDialogue();
        /// <summary>
        /// Load settings by picking an INI file.
        /// </summary>
        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            string? IniPath = await LoadDialogue(".ini");
            if (IniPath != null)
            {
                LoadSettings(IniPath);
                LoadRosterVal();
            }
        }
        /// <summary>
        /// Save to the default settings files, when the window closes.
        /// </summary>
        private void Window_Closed(object sender, WindowEventArgs args) => SaveSettings();
    }
}
