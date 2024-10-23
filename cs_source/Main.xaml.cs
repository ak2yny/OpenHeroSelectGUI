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
        private bool ShowClashes;

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
                NavView_Navigate(typeof(Tab_Settings), args.RecommendedNavigationTransitionInfo, false);
            }
            else if (args.SelectedItemContainer != null && args.SelectedItemContainer.Tag.ToString() is string TagName && Type.GetType(TagName) is Type navPageType)
            {
                bool forceNav = false;
                if (!sender.FooterMenuItems.Contains(args.SelectedItem) && args.SelectedItem is NavigationViewItem SI)  // prevent footer pages from becoming home
                {
                    forceNav = NavView_PrepGame(SI.Name[(SI.Name.LastIndexOf('_') + 1)..]);
                    CfgSt.GUI.Home = SI.Name;
                }
                NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo, forceNav);
            }
        }
        /// <summary>
        /// When loading a game tab, if the <paramref name="Game"/> changed, save and load settings and roster, otherwise update clashes, if clash option changed.
        /// </summary>
        /// <returns><see langword="True"/> if game changed, otherwise <see langword="false"/>.</returns>
        private bool NavView_PrepGame(string Game)
        {
            if (Game is not "MUA" and not "XML2") { return false; }
            if (CfgSt.GUI.Game != Game)
            {
                SaveError.IsOpen = !SaveSettings();
                CfgSt.GUI.Game = Game;
                LoadRoster();
                return true;
            }
            if (ShowClashes != CfgSt.GUI.ShowClashes)
            {
                ShowClashes = CfgSt.GUI.ShowClashes;
                if (!ShowClashes)
                {
                    for (int i = 0; i < CfgSt.Roster.Selected.Count; i++) { CfgSt.Roster.Selected[i].NumClash = false; }
                }
                UpdateClashes(false);
            }
            return false;
        }
        /// <summary>
        /// Navigation View: Initial commands. Load home page.
        /// </summary>
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = Directory.Exists(Path.Combine(CfgSt.OHS.GameInstallPath, "data"))
                ? NavView.MenuItems.Cast<NavigationViewItem>().Select(GetHome).FirstOrDefault(i => i is not null) is NavigationViewItem Home
                ? Home
                : NavView.MenuItems[0]
                : NavView.SettingsItem;
        }
        /// <summary>
        /// Recursively search <paramref name="ParentItem"/>'s MenuItems for an item that matches <see cref="GUIsettings.Home"/>.
        /// </summary>
        /// <returns>The first <see cref="NavigationViewItem"/> found, or <see langword="null"/> if not found.</returns>
        private static NavigationViewItem? GetHome(NavigationViewItem Item)
        {
            if (Item.Name == CfgSt.GUI.Home) { return Item; }
            return Item.MenuItems.Cast<NavigationViewItem>().FirstOrDefault(i => GetHome(i) is not null);
        }
        /// <summary>
        /// Navigation View: Change the page/tab according to the selected <paramref name="navPageType"/>.
        /// </summary>
        private void NavView_Navigate(Type navPageType, NavigationTransitionInfo transitionInfo, bool forceNav)
        {
            // Get the page type before navigation so you can prevent duplicate entries in the backstack.
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null && (!Equals(preNavPageType, navPageType) || forceNav))
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
            else if (CfgSt.Roster.Selected.Count < 1) { ShowWarning("No characters selected."); }
            else if (!File.Exists(Path.Combine(OHSpath.CD, "OpenHeroSelect.exe"))) { ShowWarning($"OpenHeroSelect.exe not found. Please re-install OHS into '{OHSpath.CD}'."); }
            else
            {
                OHSWarning.IsOpen = !(OHSRunning.IsOpen = true);
                int EC = 0;
                try
                {
                    await Task.Run(() =>
                    {
                        InstallStage(); // may fail silently
                        SaveBackup.IsOpen = CfgSt.GUI.FreeSaves && !OHSpath.BackupSaves();
                        FileWarning.IsOpen = MarvelModsXML.TeamBonusCopy();
                        EC = SaveSettingsMP()
                            ? Util.RunElevated("OpenHeroSelect.exe", (CfgSt.GUI.Game == "XML2") ? "-a -q -x" : "-a -q")
                            : 7;
                    }).WaitAsync(TimeSpan.FromMinutes(3));
                }
                catch
                {
                    EC = 6;
                }
                switch (EC)
                {
                    case 7:
                        ShowWarning($"OHS didn't run, because saving the settings failed. Make sure that the GUI has permission to write/overwrite '{OHSpath.CD}{OHSpath.Game}/config.ini'.");
                        break;
                    case 6:
                        ShowWarning("Timout reached. Try again, check the error.log, if it exists, check permissions for OHS and game/mod folders, and read the instructions.");
                        break;
                    case 5:
                        ShowWarning("OHS could not start. Try again or ask for help.");
                        break;
                    case > 0:
                        ShowWarning($"OHS hit an error. Check the error.log. Ask for help, if error.log doesn't exist in '{OHSpath.CD}'.");
                        _ = Process.Start("explorer.exe", $"/select, \"{Path.Combine(OHSpath.CD, "error.log")}\"");
                        break;
                    default:
                        OHSSuccess.IsOpen = !(OHSWarning.IsOpen = OHSRunning.IsOpen = false);
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        OHSSuccess.IsOpen = false;
                        break;
                }
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
            if (OHSpath.Game == "mua" && CfgSt.GUI.CopyStage && CfgSt.Var.SelectedStage is StageModel Stage)
            {
                string RiserPath = Path.Combine(OHSpath.CD, "stages", ".riser");
                string SelectedLayoutPath = Path.Combine(OHSpath.CD, "stages", CfgSt.GUI.Layout);
                if (Stage.Path is DirectoryInfo ModelFolder && ModelFolder.Exists && ModelFolder.EnumerateFiles("*.igb").FirstOrDefault() is FileInfo M)
                {
                    OHSpath.CopyToGame(M, Path.Combine("ui", "models"), "m_team_stage.igb");
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
        private static async Task<bool> SaveDialogue()
        {
            FileSavePicker savePicker = new();
            savePicker.FileTypeChoices.Add("Configuration File", [".ini"]);
            InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(App.MainWindow));
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                CfgSt.MUA.MenulocationsValue = CfgSt.OHS.RosterValue = file.DisplayName;
                return SaveSettings(file.Path, file.DisplayName);
            }
            return true;
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
            SaveError.IsOpen = !SaveSettings();
            FileWarning.IsOpen = !MarvelModsXML.TeamBonusSerializer(OHSpath.Team_bonus);
        }

        /// <summary>
        /// Save settings by defining an INI file.
        /// </summary>
        private async void BtnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveError.IsOpen = !await SaveDialogue();
        }

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
            _ = SaveSettings() && MarvelModsXML.TeamBonusSerializer(OHSpath.Team_bonus);
            if (!CfgSt.OHS.SaveTempFiles && Path.Combine(OHSpath.CD, "Temp") is string Temp && Directory.Exists(Temp))
            {
                try { Directory.Delete(Temp, true); } catch { } // prevents leftover process
            }
        }
    }
}
