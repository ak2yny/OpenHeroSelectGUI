using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Settings;
using System;

namespace OpenHeroSelectGUI.Controls
{
    public sealed partial class AvailableTabs : UserControl
    {
        private bool ShowRosters { get; }
        private bool ShowNeedy { get; }

        private int PreviousIndex = -1;
        private int PreviousCharIndex = -1;
        private readonly string?[] previousFilter = new string[4]; // Filters are per instance (page)
        private string? PreviousFilter { get => previousFilter[PreviousIndex]; set => previousFilter[PreviousIndex] = value; }
        public bool IsCharacterTab => PreviousIndex is 0 or 2;

        public AvailableTabs()
        {
            ShowNeedy = CfgSt.GUI.Home.StartsWith("NavItem_XVoice");
            ShowRosters = !(ShowNeedy || CfgSt.GUI.Home.StartsWith("NavItem_SkinEditor"));
            CfgSt.Roster.AvailableInitialized = false;
            InitializeComponent();
            AvailChars.IsSelected = CfgSt.GUI.AvailChars switch
            {
                "false" or
                "AvailRstrs" => !(AvailRstrs.IsSelected = ShowRosters),
                "AvailTeams" => !(AvailTeams.IsSelected = ShowNeedy),
                "AvailNeedy" => !(AvailNeedy.IsSelected = ShowNeedy),
                _ => true,
            };
        }
        /// <summary>
        /// Updates the displayed available collection content according to the selected tab. Updates the search filter and saves the selected tab.
        /// </summary>
        private async void Available_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            int SelectedIndex = sender.Items.IndexOf(sender.SelectedItem);
            if (SelectedIndex == PreviousIndex) { return; }
            //if (AvailCollection.ContentTransitions[0] is EntranceThemeTransition t)
            //{ // are the sides correct?
            //    t.FromHorizontalOffset = SelectedIndex > PreviousIndex ? 400 : -400;
            //}
            if (PreviousIndex == -1) { PreviousIndex = SelectedIndex; }
            else
            {
                PreviousFilter = AvailSearch.Text;
                PreviousIndex = SelectedIndex;
                // Prevent the text changed handler from firing, by temporarily unsubscribing.
                AvailSearch.TextChanged -= Search_QuerySubmitted;
                AvailSearch.Text = IsCharacterTab ? PreviousFilter ?? "" : "";
                await System.Threading.Tasks.Task.Delay(50);
                AvailSearch.TextChanged += Search_QuerySubmitted;
            }
            switch (SelectedIndex)
            {
                case 0: AvailCollection.Content = new AvailableCharacters(ShowNeedy); break;
                case 1: AvailCollection.Content = new AvailableRosters(); break;
                case 2: AvailCollection.Content = new AvailableCharacters(ShowNeedy, true); break;
                case 3: AvailCollection.Content = new AvailableTeams(); break;
                default: break;
            }
            if (IsCharacterTab)
            {   // Reset the available characters if new or switching between the char tabs (shared collection).
                if ((CfgSt.Roster.AvailableFiltered.Count == 0 && AvailSearch.Text == "")
                    || CfgSt.Roster.Available.Length == 0) { CfgSt.Roster.PopulateAvailable(); }
                else if (SelectedIndex != PreviousCharIndex) { CfgSt.Roster.PopulateAvailable(AvailSearch.Text); }
                PreviousCharIndex = SelectedIndex;
            }
            CfgSt.GUI.AvailChars = sender.SelectedItem.Name;
        }
        /// <summary>
        /// Search while typing a string
        /// </summary>
        private void Search_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            switch (AvailCollection.Content)
            {
                case AvailableRosters AR:
                    AR.SearchAvailable(sender.Text); break;
                case AvailableTeams AT:
                    AT.LoadAvailable(sender.Text); break;
                //case AvailableCharacters:
                default:
                    CfgSt.Roster.PopulateAvailable(sender.Text); break;
            }
        }
        /// <summary>
        /// Window-wide page shortcut: F3 = search
        /// </summary>
        private void Search_Shortcut_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = !args.Handled && AvailSearch.FocusState != Microsoft.UI.Xaml.FocusState.Programmatic
                && AvailSearch.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        }
        /// <summary>
        /// Open file picker to select a herostat file and add it to the herostat folder and to the available characters.
        /// </summary>
        private async void BrowseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (AvailCollection.Content is AvailableCharacters AC)
            {
                Windows.Storage.Pickers.FileOpenPicker filePicker = new();
                filePicker.FileTypeFilter.Add("*");
                WinRT.Interop.InitializeWithWindow.Initialize(filePicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
                System.Collections.Generic.IReadOnlyList<Windows.Storage.StorageFile> Mods = await filePicker.PickMultipleFilesAsync();
                if (Mods != null)
                {
                    for (int i = 0; i < Mods.Count; i++)
                    {
                        AC.Install(Mods[i]);
                    }
                }
            }
        }
        /// <summary>
        /// Re-load the availables from disk
        /// </summary>
        private void BtnReload_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            switch (AvailCollection.Content)
            {
                case AvailableRosters AR:
                    AR.LoadAvailable(AvailSearch.Text); break;
                case AvailableTeams AT:
                    CfgSt.Roster.Teams.Clear();
                    BonusSerializer.Deserialize();
                    AT.LoadAvailable(AvailSearch.Text); break;
                //case AvailableCharacters AC:
                default:
                    CfgSt.Roster.AvailableInitialized = false;
                    CfgSt.Roster.PopulateAvailable();
                    break;
            }
        }
    }
}
