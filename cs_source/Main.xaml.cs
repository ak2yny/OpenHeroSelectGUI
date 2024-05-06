using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static OpenHeroSelectGUI.Settings.CfgCmd;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The main window contains the banner and navigation panel, as well as common elements.
    /// </summary>
    public sealed partial class Main : Window
    {
        private int ShowClashes;

        public Main()
        {
            Activated += MainWindow_Activated;
            LoadGuiSettings();
            ShowClashes = CfgSt.GUI.ShowClashes;
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
            if (args.IsSettingsSelected)
            {
                NavView_Navigate(typeof(Tab_Settings), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.SelectedItemContainer != null && args.SelectedItemContainer.Tag.ToString() is string TagName && Type.GetType(TagName) is Type navPageType)
            {
                if (!NavView.FooterMenuItems.Contains(NavView.SelectedItem))  // prevent footer pages from becoming home
                {
                    if (navPageType == typeof(Tab_XML2)) { NavView_PrepGameTab("xml2"); }
                    else if (navPageType == typeof(Tab_MUA)) { NavView_PrepGameTab("mua"); }
                    CfgSt.GUI.Home = sender.MenuItems.IndexOf(args.SelectedItem);
                }
                NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo);
            }
        }
        /// <summary>
        /// When loading a game tab, if the <paramref name="Game"/> changed, save and load settings and roster, otherwise update clashes, if clash option changed.
        /// </summary>
        private void NavView_PrepGameTab(string Game)
        {
            if (CfgSt.GUI.Game != Game)
            {
                SaveSettings();
                CfgSt.GUI.Game = Game;
                LoadRoster();
            }
            else if (ShowClashes != CfgSt.GUI.ShowClashes)
            {
                ShowClashes = CfgSt.GUI.ShowClashes;
                if ((Game == "xml2" && ShowClashes != 1) || ShowClashes == 2)
                {
                    for (int i = 0; i < CfgSt.Roster.Selected.Count; i++) { CfgSt.Roster.Selected[i].NumClash = false; }
                }
                UpdateClashes(false);
            }
        }
        /// <summary>
        /// Navigation View: Initial commands. Load home page.
        /// </summary>
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = Directory.Exists(Path.Combine(CfgSt.OHS.GameInstallPath, "data")) ? NavView.MenuItems[CfgSt.GUI.Home] : NavView.SettingsItem;
        }
        /// <summary>
        /// Navigation View: Change the page/tab according to the selected <paramref name="navPageType"/>.
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
            if (!Directory.Exists(Path.Combine(CfgSt.OHS.GameInstallPath, "data")))
            {
                NavView.SelectedItem = NavView.SettingsItem;
            }
            else if (string.IsNullOrEmpty(CfgSt.GUI.Game)) { ShowWarning("MUA or XML2 not defined."); }
            else if (File.Exists(Path.Combine(OHSpath.CD, "OpenHeroSelect.exe")))
            {
                OHSWarning.IsOpen = !(OHSRunning.IsOpen = true);
                int EC = 0;
                await Task.Run(() =>
                {
                    SaveSettingsMP();
                    InstallStage();
                    MarvelModsXML.TeamBonusCopy();
                    if (CfgSt.GUI.FreeSaves) { OHSpath.BackupSaves(); }

                    EC = Util.RunElevated("OpenHeroSelect.exe", (CfgSt.GUI.Game == "xml2")
                        ? "-a -q -x"
                        : "-a -q");
                });
                switch (EC)
                {
                    case 5:
                        ShowWarning($"OHS could not start. Please stop it if it's running or re-install OHS into '{OHSpath.CD}'.");
                        break;
                    case > 0:
                        ShowWarning("OHS hit an error. Check the error.log.");
                        _ = Process.Start("explorer.exe", $"/select, \"{Path.Combine(OHSpath.CD, "error.log")}\"");
                        break;
                    default:
                        OHSSuccess.IsOpen = !(OHSWarning.IsOpen = OHSRunning.IsOpen = false);
                        await Task.Delay(3000);
                        OHSSuccess.IsOpen = false;
                        break;
                }
            }
            else
            {
                ShowWarning("OpenHeroSelect.exe not found!");
            }
        }
        /// <summary>
        /// Show a warning <paramref name="message"/> in an <see cref="InfoBar"/>.
        /// </summary>
        private void ShowWarning(string message)
        {
            OHSWarning.Message = message;
            OHSWarning.IsOpen = !(OHSRunning.IsOpen = false);
        }
        /// <summary>
        /// Install the stage files to the game/mod folder
        /// </summary>
        private static void InstallStage()
        {
            if (CfgSt.GUI.Game == "mua" && CfgSt.GUI.CopyStage && CfgSt.Var.SelectedStage is StageModel Stage)
            {
                string RiserPath = Path.Combine(OHSpath.CD, "stages", ".riser");
                string SelectedLayoutPath = Path.Combine(OHSpath.CD, "stages", CfgSt.GUI.Layout);
                if (Stage.Path is DirectoryInfo ModelFolder && ModelFolder.Exists && ModelFolder.EnumerateFiles("*.igb").FirstOrDefault() is FileInfo M)
                {
                    OHSpath.CopyToGame(ModelFolder.FullName, Path.Combine("ui", "models"), M.Name, "m_team_stage.igb");
                }

                OHSpath.CopyToGame(SelectedLayoutPath, Path.Combine("ui", "menus"), "mlm_team_back.igb");
                OHSpath.CopyToGame(Path.GetDirectoryName(MarvelModsXML.UpdateLayout(Path.Combine(SelectedLayoutPath, "team_back.xmlb"), Stage.Riser)), Path.Combine("ui", "menus"), "team_back.xmlb");
                if (Stage.Riser)
                {
                    OHSpath.CopyToGameRel(RiserPath, Path.Combine("effects", "menu"), "riser.xmlb");
                    OHSpath.CopyToGameRel(RiserPath, Path.Combine("models", "effects"), "riser.igb");
                    OHSpath.CopyToGameRel(RiserPath, Path.Combine("packages", "generated", "maps", "package", "menus"), "team_back.pkgb");
                }
                else
                {
                    OHSpath.CopyToGameRel(RiserPath, Path.Combine("packages", "generated", "maps", "package", "menus"), "team_back.bkp.pkgb", "team_back.pkgb");
                }
            }
        }
        /// <summary>
        /// Save dialogue.
        /// </summary>
        private static async void SaveDialogue()
        {
            FileSavePicker savePicker = new();
            savePicker.FileTypeChoices.Add("Configuration File", [".ini"]);
            InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(App.MainWindow));
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                CfgSt.MUA.MenulocationsValue = CfgSt.OHS.RosterValue = file.DisplayName;
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
            _ = MarvelModsXML.TeamBonusSerializer(OHSpath.Team_bonus);
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
                if (NavView.SelectedItem.GetType() == typeof(Tab_Teams)) { _ = ContentFrame.Navigate(typeof(Tab_Teams)); }
                // Note: This creates a duplicate in the back stack. The stack is currently unused.
            }
        }
        /// <summary>
        /// Save to the default settings files, when the window closes.
        /// </summary>
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            SaveSettings();
            _ = MarvelModsXML.TeamBonusSerializer(OHSpath.Team_bonus);
            if (!CfgSt.OHS.SaveTempFiles && Path.Combine(OHSpath.CD, "Temp") is string Temp && Directory.Exists(Temp))
            {
                Directory.Delete(Temp, true);
            }
        }
    }
}
