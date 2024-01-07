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
using System.Threading.Tasks;
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
            LoadRoster();

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
                if (!NavView.FooterMenuItems.Contains(NavView.SelectedItem))  // prevent footer pages from becoming home
                {
                    if (navPageType == typeof(Tab_XML2) && GUIsettings.Instance.Game != "xml2")
                    {
                        SaveSettings();
                        GUIsettings.Instance.Game = "xml2";
                        LoadRoster();
                    }
                    else if (navPageType == typeof(Tab_MUA) && GUIsettings.Instance.Game != "mua")
                    {
                        SaveSettings();
                        GUIsettings.Instance.Game = "mua";
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
                _ = ContentFrame.Navigate(navPageType, null, transitionInfo);
            }
        }
        /// <summary>
        /// Run OpenHeroSelect with the specified settings and backup saves if enabled.
        /// </summary>
        private async void RunOHS()
        {
            if (string.IsNullOrEmpty(GUIsettings.Instance.Game)) { ShowWarning("MUA or XML2 not defined."); return; }
            if (File.Exists(Path.Combine(cdPath, "OpenHeroSelect.exe")))
            {
                string DP = Path.Combine(OHSsettings.Instance.GameInstallPath, "data");
                if (Directory.Exists(DP))
                {
                    OHSWarning.IsOpen = !(OHSRunning.IsOpen = true);
                    await Task.Run(() =>
                    {
                        SaveSettings();
                        if (GUIsettings.Instance.Game == "mua" && GUIsettings.Instance.CopyStage) { InstallStage(); }
                        MarvelModsXML.TeamBonusCopy();
                        if (GUIsettings.Instance.FreeSaves && OHSsettings.Instance.LaunchGame) { MoveSaves("Save", $"{DateTime.Now:yyMMdd-HHmmss}"); }
                        // if this waits until the game has closed (OHS doesn't wait AFAIK) then we can restore the saves after

                        string arg = (GUIsettings.Instance.Game == "xml2") ?
                            "-a -x" :
                            "-a";
                        Util.RunElevated("OpenHeroSelect.exe", arg);
                    });
                    string elog = Path.Combine(cdPath, "error.log");
                    if (File.Exists(elog))
                    {
                        ShowWarning("OHS hit an error. Check the error.log.");
                        _ = Process.Start("explorer.exe", $"/select, \"{elog}\"");
                    }
                    else
                    {
                        OHSSuccess.IsOpen = !(OHSWarning.IsOpen = OHSRunning.IsOpen = false);
                        await Task.Delay(3000);
                        OHSSuccess.IsOpen = false;
                    }
                }
                else
                {
                    ShowWarning($"Installation path not valid! '{DP}' not found.");
                }
            }
            else
            {
                ShowWarning("OpenHeroSelect.exe not found!");
            }
        }
        /// <summary>
        /// Show a warning info bar.
        /// </summary>
        private void ShowWarning(string message)
        {
            OHSWarning.Message = message;
            OHSWarning.IsOpen = !(OHSRunning.IsOpen = false);
        }
        private static void InstallStage()
        {
            string StagesPath = Path.Combine(cdPath, "stages");
            string RiserPath = Path.Combine(StagesPath, ".riser");
            string SelectedLayoutPath = Path.Combine(StagesPath, GUIsettings.Instance.Layout);
            if (DynamicSettings.Instance.SelectedStage is StageModel Stage)
            {
                if (Stage.Path is DirectoryInfo ModelFolder && ModelFolder.Exists && ModelFolder.EnumerateFiles("*.igb").Any())
                {
                    CopyGameFile(ModelFolder.FullName, Path.Combine("ui", "models"), ModelFolder.EnumerateFiles("*.igb").First().Name, "m_team_stage.igb");
                }

                CopyGameFile(SelectedLayoutPath, Path.Combine("ui", "menus"), "mlm_team_back.igb");
                CopyGameFile(Path.GetDirectoryName(MarvelModsXML.UpdateLayout(Path.Combine(SelectedLayoutPath, "team_back.xmlb"), Stage.Riser))!, Path.Combine("ui", "menus"), "team_back.xmlb");
                if (Stage.Riser)
                {
                    CopyGameFileWStrctr(RiserPath, Path.Combine("effects", "menu"), "riser.xmlb");
                    CopyGameFileWStrctr(RiserPath, Path.Combine("models", "effects"), "riser.igb");
                    CopyGameFileWStrctr(RiserPath, Path.Combine("packages", "generated", "maps", "package", "menus"), "team_back.pkgb");
                }
                else
                {
                    CopyGameFileWStrctr(RiserPath, Path.Combine("packages", "generated", "maps", "package", "menus"), "team_back.bkp.pkgb", "team_back.pkgb");
                }
            }
        }
        /// <summary>
        /// Save dialogue.
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
        private void BtnRun_Click(object sender, RoutedEventArgs e) => RunOHS();
        /// <summary>
        /// Save settings to the default location, using config values for the CFG files.
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            _ = MarvelModsXML.TeamBonusSerializer(MarvelModsXML.team_bonus);
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
            string? IniPath = await LoadDialogue(".ini");
            if (IniPath != null)
            {
                LoadSettings(IniPath);
                LoadRosterVal();
                if (NavView.SelectedItem.GetType() == typeof(Tab_Teams)) { _ = ContentFrame.Navigate(typeof(Tab_Teams), null); }
                // Note: This creates a duplicate in the back stack. The stack is currently unused.
            }
        }
        /// <summary>
        /// Save to the default settings files, when the window closes.
        /// </summary>
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            SaveSettings();
            _ = MarvelModsXML.TeamBonusSerializer(MarvelModsXML.team_bonus);
            if (Directory.Exists(Path.Combine(cdPath, "Temp")) && !OHSsettings.Instance.SaveTempFiles)
                Directory.Delete(Path.Combine(cdPath, "Temp"), true);
        }
    }
}
