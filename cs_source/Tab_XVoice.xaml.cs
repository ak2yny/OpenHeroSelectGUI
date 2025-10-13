using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Zsnd_UI.lib;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Tab_XVoice : Page
    {
        public Cfg Cfg { get; set; } = new();
        public ObservableCollection<XVSound> FilteredXVSounds { get; set; } = [];
        string IntName { get; set; } = "";

        public Tab_XVoice()
        {
            if (ZsndLists.Sounds.Count == 0)
            {
                ZsndCmd.ReadXVoice(Path.Combine(OHSpath.XvoiceDir, "x_voice.json"));
            }
            InitializeComponent();
            if (Cfg.Roster.Teams.Count == 0)
            {
                MarvelModsXML.TeamBonusDeserializer(new StandardUICommand(StandardUICommandKind.Delete));
            }
            _ = AvailableCharactersOrTeams.Navigate(typeof(AvailableCharacters));
            NotLoadedError.IsOpen = ZsndLists.Sounds.Count == 0;
        }

        private void FloatingCharacter_Changed(object sender, TextChangedEventArgs e)
        {
            if (Cfg.Var.FloatingCharacter is null) { return; }
            if (Cfg.Var.FloatingCharacter.StartsWith("common/team_bonus_", StringComparison.OrdinalIgnoreCase))
            {
                XVsearch.Text = Cfg.Var.FloatingCharacter;
            }
            else
            {
                IntName = Herostat.GetInternalName().ToUpper();
                XVsearch.Text = IntName;
            }
            // if (XVTabs.SelectedItem == XVTabs.MenuItems[0]) // is Characters
        }
        /// <summary>
        /// Navigation View: Determine the selected tab, when not already selected.
        /// </summary>
        private void XVTabs_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            XVsearch.Text = "";
            Type? navPageType = args.SelectedItemContainer != null && args.SelectedItemContainer.Tag.ToString() is string TagName
                ? Type.GetType(TagName)
                : null;
            // Only navigate if the selected page isn't currently loaded.
            _ = navPageType is not null && !Equals(AvailableCharactersOrTeams.CurrentSourcePageType, navPageType)
                && AvailableCharactersOrTeams.Navigate(navPageType, null, args.RecommendedNavigationTransitionInfo);
        }

        private async void LoadX_Click(object sender, RoutedEventArgs e)
        {
            if (await CfgCmd.LoadDialogue(".zsm", ".zss", ".json") is string ZFile)
            {
                switch (Path.GetExtension(ZFile).ToLowerInvariant())
                {
                    case ".zsm" or ".zss":
                        _ = ZsndCmd.LoadZsnd(ZFile, OHSpath.XvoiceDir, IsXvoice: true);
                        break;
                    case ".json":
                        ZsndCmd.ReadXVoice(ZFile);
                        break;
                }
                NotAnXvoice.IsOpen = !Path.GetFileName(ZFile).StartsWith("x_voice", StringComparison.OrdinalIgnoreCase);
                // !ZsndLists.Sounds.Any(s => s.Hash is not null && s.Hash.StartsWith("MENUS/CHARACTER/BREAK_", StringComparison.OrdinalIgnoreCase));
            }
            NotLoadedError.IsOpen = ZsndLists.Sounds.Count == 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            NotAppliedError.IsOpen = false;
            if (ZsndCmd.SaveJson(Path.Combine(OHSpath.XvoiceDir, "x_voice.json")) is string ErrorMsg)
            {
                NotAppliedError.Message = ErrorMsg;
                NotAppliedError.IsOpen = true;
            }
        }

        private async void Apply_Click(object sender, RoutedEventArgs e)
        {
            ApplyRunning.IsOpen = !(AppliedSuccess.IsOpen = NotAppliedError.IsOpen = ApplyButton.IsEnabled = false);
            string? ErrorMsg = null;
            string? SuccMsg = null;
            await Task.Run(() =>
            {
                try
                {
                    string XPath = Path.Combine(Directory.CreateDirectory(Path.Combine(CfgSt.OHS.GameInstallPath, "sounds", "eng", "x", "_")).FullName, "x_voice.zss");
                    ErrorMsg = ZsndCmd.WriteZsnd(XPath, OHSpath.XvoiceDir);
                    SuccMsg = $"X_voice saved to {XPath}";
                }
                catch
                {
                    ErrorMsg = $"Failed to write to create sound directories in {CfgSt.OHS.GameInstallPath}";
                }
            }).WaitAsync(TimeSpan.FromMinutes(1));
            ApplyButton.IsEnabled = !(ApplyRunning.IsOpen = false);
            if (ErrorMsg is null)
            {
                AppliedSuccess.Message = SuccMsg;
                AppliedSuccess.IsOpen = true;
                await Task.Delay(TimeSpan.FromSeconds(3));
                AppliedSuccess.IsOpen = false;
            }
            else
            {
                NotAppliedError.Message = ErrorMsg;
                NotAppliedError.IsOpen = true;
            }
        }

        private void XVsearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) => FilterXVSounds(sender.Text);

        private void FilterXVSounds(string Filter)
        {
            SearchTooMany.IsOpen = false;
            if (ZsndLists.Sounds.Count > 0)
            {
                FilteredXVSounds.Clear();
                if (Filter != "")
                {
                    bool IsTeam = Filter.StartsWith("common/team_bonus_", StringComparison.OrdinalIgnoreCase);
                    for (int i = 0; i < ZsndLists.Sounds.Count; i++)
                    {
                        XVSound S = (XVSound)ZsndLists.Sounds[i];
                        if (Filter == IntName
                            ? S.IntName!.Equals(Filter, StringComparison.OrdinalIgnoreCase)
                            : IsTeam
                            ? S.Hash!.Equals(Filter, StringComparison.OrdinalIgnoreCase)
                            : S.Hash!.Contains(Filter, StringComparison.CurrentCultureIgnoreCase))
                        {
                            FilteredXVSounds.Add(S);
                        }
                        if (FilteredXVSounds.Count > 200) { break; }
                    }
                    SearchTooMany.IsOpen = FilteredXVSounds.Count > 198;
                }
            }
        }

        private void XVoiceList_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (XVoiceList.SelectedItem is XVSound Sound && FilteredXVSounds.Remove(Sound))
            {
                _ = ZsndLists.Sounds.Remove(Sound);
            }
        }

        private async void Flags_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement Control)
            {
                XVoiceList.SelectedItem = Control.DataContext;
            }
            _ = await ViewSoundFlags.ShowAsync();
            // if (ContentDialogResult _ == ContentDialogResult.Primary)
            // {
            //     // do something;
            // }
        }

        private async void Sample_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement Control)
            {
                XVoiceList.SelectedItem = Control.DataContext;
            }
            if (XVoiceList.SelectedItem is XVSound XS && XS.SampleIndex >= ZsndLists.Samples.Count)
            {
                XS.SampleIndex = ZsndLists.Samples.Count - 1;
            }
            _ = await AddSample.ShowAsync();
        }

        private void SampleDropArea_DragEnter(object sender, DragEventArgs e)
        {
            SampleDropArea.Visibility = Visibility.Visible;
        }

        private void SampleDropAreaBG_DragLeave(object sender, DragEventArgs e)
        {
            SampleDropArea.Visibility = Visibility.Collapsed;
        }

        private async void SampleDropAreaBG_DragOver(object sender, DragEventArgs e)
        {
            e.DragUIOverride.IsCaptionVisible = true;
            e.AcceptedOperation = DataPackageOperation.Copy;
            if (e.DataView.Contains(StandardDataFormats.StorageItems)
                && (await e.DataView.GetStorageItemsAsync())[0] is StorageFile Sample)
            {
                e.DragUIOverride.Caption = $"Add {Sample.Name}";
                SampleDropArea.Visibility = Visibility.Visible;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private async void SampleDropAreaBG_Drop(object sender, DragEventArgs e)
        {
            FileIncompatible.IsOpen = false;
            int NewIndex = ZsndLists.Samples.Count;
            if (NewIndex > 0xFFFF)
            {
                FileMaxReached.IsOpen = true;
            }
            else
            {
                FileMaxReached.IsOpen = false;
                JsonSample SampleInfo = new();
                try
                {
                    // WAV and VAG are headerless, DSP has special header, XML has original RIFF header, Xbox ADPCM is unhandled at this time
                    if (e.DataView.Contains(StandardDataFormats.StorageItems)
                        && (await e.DataView.GetStorageItemsAsync())[0] is StorageFile Sample
                        && ZsndConvert.From(Sample.FileType, Sample.Path, SampleInfo) is byte[] ConvertedFileBuffer && ConvertedFileBuffer.Length > 0)
                    {
                        string New = Path.Combine(OHSpath.XvoiceDir, "new");
                        _ = Directory.CreateDirectory(New);
                        File.WriteAllBytes(Path.Combine(New, Sample.Name), ConvertedFileBuffer);
                        SampleInfo.File = Path.Combine("new", Sample.Name);
                        ZsndLists.Samples.Add(SampleInfo);
                        if (XVoiceList.SelectedItem is XVSound Sound)
                        {
                            Sound.SampleIndex = NewIndex;
                            Sound.Sample = Sample.Name;
                        }
                    }
                    else
                    {
                        FileIncompatible.IsOpen = true;
                    }
                }
                catch
                {
                    FileIncompatible.IsOpen = true;
                }
            }
            SampleDropArea.Visibility = Visibility.Collapsed;
        }

        private void XVoiceList_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties["Character"] is Character SC)
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = $"Add {SC.Name}";
            }
            else if (e.DataView.Properties["TeamBonus"] is TeamBonus TB)
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = $"Add {TB.Sound}";
            }
        }

        private void XVoiceList_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties["Character"] is not null) // is Character SC
            {
                // IntName = Herostat.GetInternalName(SC.Path);
                FilteredXVSounds.Add(new XVSound
                {
                    IntName = IntName,
                    Pref = ZsndEvents.XVprefix.AN,
                    SampleIndex = ZsndLists.Samples.Count
                });
                ZsndLists.Sounds.Add(FilteredXVSounds[^1]);
                _ = ZsndLists.XVInternalNames.Add(IntName);
            }
            else if (e.DataView.Properties["TeamBonus"] is TeamBonus TB)
            {
                FilteredXVSounds.Add(new XVSound
                {
                    Pref = ZsndEvents.XVprefix.TEAM,
                    Hash = TB.Sound?.Length > 18 ? TB.Sound : $"COMMON/TEAM_BONUS_{TB.Name}".Replace(' ', '_'),
                    SampleIndex = ZsndLists.Samples.Count
                });
                ZsndLists.Sounds.Add(FilteredXVSounds[^1]);
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            SoundNotPlayed.IsOpen = false;
            if (XVoiceList.SelectedItem is XVSound Sound && ZsndLists.Samples[Sound.SampleIndex] is JsonSample S && S.File is string Filename)
            {
                SoundNotPlayed.IsOpen = !TemporaryPlayer.Play(Path.IsPathFullyQualified(Filename) ? Filename
                    : Path.Combine(OHSpath.XvoiceDir, Filename), S);
            }
        }
    }
}
