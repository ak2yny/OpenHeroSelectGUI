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
using Zsnd.Lib;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// X_Voice list page
    /// </summary>
    public sealed partial class Tab_XVoice : Page
    {
        public Cfg Cfg { get; set; } = new();
        public ObservableCollection<XVSound> FilteredXVSounds { get; set; } = [];
        private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };

        private string _intName = "";
        // Display and selection sample rates. Other rates are probably fully supported (but not shown).
        public uint[] SampleRates { get; } =
        [
            8000, 11025, 16000, 22050, 32000, 41000, 44100, 48000, 96000, 192000
        ];
        /// <summary>
        /// X_Voice list page
        /// </summary>
        public Tab_XVoice()
        {
            if (Lists.Sounds.Count == 0)
            {
                Cmd.ReadXVoice(Path.Combine(OHSpath.XvoiceDir, "x_voice.json"));
            }
            InitializeComponent();
            NotLoadedError.IsOpen = Lists.Sounds.Count == 0;
            _searchTimer.Tick += SearchTimer_Tick;
            Cfg.Var.PropertyChanged += FloatingCharacter_Changed;
        }
        /// <summary>
        /// Adds the <paramref name="Sound"/> to the <see cref="FilteredXVSounds"/> collection and the <see cref="Lists.Sounds"/> list.
        /// </summary>
        private void AddSound(XVSound Sound)
        {
            FilteredXVSounds.Add(Sound);
            Lists.Sounds.Add(Sound);
        }
        /// <summary>
        /// Adds a new sound to the <see cref="FilteredXVSounds"/> collection and the <see cref="Lists.Sounds"/> list.
        /// </summary>
        private void AddSound(int Index = -1, string TeamName = "")
        {
            bool IsChar = Available.IsCharacterTab;
            AddSound(new XVSound
            {
                SampleIndex = Index,
                IntName = IsChar ? _intName : TeamName,
                Pref = IsChar ? Events.LastCharPrefix : Events.XVprefix.TEAM
            });
            if (IsChar) { _ = Lists.XVInternalNames.Add(_intName); }
        }
        /// <summary>
        /// Adds a new audio <paramref name="Sample"/> to the x_voice package, converting and storing the file as needed & adds a new default sound, if the sample was added successfully.
        /// </summary>
        private void AddSampleSound(StorageFile Sample)
        {
            if (AddSample(Sample) is int Index && Index > -1)
            {
                AddSound(Index, Sample.DisplayName);
            }
        }
        /// <summary>
        /// Adds a new audio <paramref name="Sample"/> to the x_voice package, converting and storing the file as needed.
        /// </summary>
        /// <returns>The zero-based index of the newly added sample if successful; otherwise, -1 if the sample could not be added
        /// due to file incompatibility or if the maximum number of samples has been reached.</returns>
        private int AddSample(StorageFile Sample)
        {
            FileIncompatible.IsOpen = FileMaxReached.IsOpen = false;
            int NewIndex = Lists.Samples.Count;
            if (NewIndex > 0xFFFF)
            {
                FileMaxReached.IsOpen = true;
                return -1;
            }
            string New = Path.Combine(OHSpath.XvoiceDir, "new");
            JsonSample SampleInfo = new();
            try
            {
                _ = Directory.CreateDirectory(New);
                // WAV and VAG are headerless, DSP has special header, XML has original RIFF header, Xbox ADPCM is unhandled at this time
                if (ZsndConvert.From(Sample.FileType, Sample.Path, SampleInfo) is byte[] ConvertedFileBuffer && ConvertedFileBuffer.Length > 0) // "RIFF"
                {
                    File.WriteAllBytes(Path.Combine(New, Sample.Name), ConvertedFileBuffer);
                }
                else
                {
                    SampleInfo.Format = 106;
                    SampleInfo.Sample_rate = 22050;
                    _ = TemporaryPlayer.Play(Sample.Path, SampleInfo); // possibly use different check (with To), not playing the sound
                    File.Copy(Sample.Path, Path.Combine(New, Sample.Name), true);
                }
                SampleInfo.File = Path.Combine("new", Sample.Name);
                Lists.Samples.Add(SampleInfo);
                return NewIndex;
            }
            catch
            {
                FileIncompatible.IsOpen = true;
                return -1;
            }

        }
        /// <summary>
        /// Event of the hidden text box that tracks changes to <see cref="InternalObservables.FloatingCharacter"/>.
        /// </summary>
        private void FloatingCharacter_Changed(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Cfg.Var.FloatingCharacter is null) { return; }
            if (Cfg.Var.FloatingCharacter.StartsWith("common/team_bonus_", StringComparison.OrdinalIgnoreCase))
            {
                XVsearch.Text = Cfg.Var.FloatingCharacter;
            }
            else
            {
                _intName = Herostat.GetInternalName().ToUpper();
                XVsearch.Text = _intName;
            }
            // if (XVTabs.SelectedItem == XVTabs.MenuItems[0]) // is Characters
        }

        private void Available_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (Cfg.Var.FloatingCharacter is null) { return; }
            if (Available.IsCharacterTab) { AddSound(); }
            else { AddSound(TeamName: Cfg.Var.FloatingCharacter); }
        }

        private void AddSound_Click(object sender, RoutedEventArgs e)
        {
            AddSound();
        }

        private async void LoadX_Click(object sender, RoutedEventArgs e)
        {
            if (await CfgCmd.LoadDialogue(".zsm", ".zss", ".json") is string ZFile)
            {
                switch (Path.GetExtension(ZFile).ToLowerInvariant())
                {
                    case ".zsm" or ".zss":
                        _ = Cmd.LoadZsnd(ZFile, OHSpath.XvoiceDir);
                        break;
                    case ".json":
                        Cmd.ReadXVoice(ZFile);
                        break;
                }
                NotAnXvoice.IsOpen = !Path.GetFileName(ZFile).StartsWith("x_voice", StringComparison.OrdinalIgnoreCase);
                // !ZsndLists.Sounds.Any(s => s.Hash is not null && s.Hash.StartsWith("MENUS/CHARACTER/BREAK_", StringComparison.OrdinalIgnoreCase));
            }
            NotLoadedError.IsOpen = Lists.Sounds.Count == 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            NotAppliedError.IsOpen = false;
            if (Cmd.SaveJson(Path.Combine(OHSpath.XvoiceDir, "x_voice.json")) is string ErrorMsg)
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
                    string XVPath = Path.Combine(Directory.CreateDirectory($"{CfgSt.OHS.GameInstallPath}/sounds/eng/x/_").FullName, "x_voice.zss");
                    ErrorMsg = Cmd.WriteZsnd(XVPath, OHSpath.XvoiceDir);
                    SuccMsg = $"X_voice saved to {XVPath}";
                }
                catch
                {
                    ErrorMsg = $"Failed to write or to create sound directories in {CfgSt.OHS.GameInstallPath}";
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

        private void XVsearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            _searchTimer.Stop(); // Stop previous searches
            if (Lists.Sounds.Count == 0 || sender.Text == "") { FilteredXVSounds.Clear(); return; }
            if (sender.Text == _intName) { FilterXVSounds(S => S.IntName is not null && S.IntName.Equals(sender.Text, StringComparison.OrdinalIgnoreCase)); return; }
            if (sender.Text.StartsWith("common/team_bonus_", StringComparison.OrdinalIgnoreCase)) { FilterXVSounds(S => S.Hash.Equals(sender.Text, StringComparison.OrdinalIgnoreCase)); return; }
            SearchInProgress.Visibility = Visibility.Visible;
            SearchInProgress.IsActive = true;
            _searchTimer.Start();
        }

        private async void SearchTimer_Tick(object? sender, object e)
        {
            _searchTimer.Stop();
            await FilterXVSounds(XVsearch.Text);
            SearchInProgress.IsActive = false;
            SearchInProgress.Visibility = Visibility.Collapsed;
        }

        private Task FilterXVSounds(string Filter)
        {
            return Task.Run(() =>
            {
                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    for (int i = 0; i < Lists.Sounds.Count; i++)
                    {
                        XVSound S = Lists.Sounds[i];
                        if (!S.Hash.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                        {
                            _ = FilteredXVSounds.Remove(S);
                        }
                        else if (!FilteredXVSounds.Contains(S))
                        {
                            FilteredXVSounds.Add(S);
                        }
                    }
                });
            });
        }

        private void FilterXVSounds(Func<XVSound, bool> FilterMatches)
        {
            FilteredXVSounds.Clear();
            for (int i = 0; i < Lists.Sounds.Count; i++)
            {
                if (FilterMatches(Lists.Sounds[i])) { FilteredXVSounds.Add(Lists.Sounds[i]); }
            }
        }

        private void XVoiceList_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (Cfg.Var.SelectedSound is XVSound Sound && FilteredXVSounds.Remove(Sound))
            {
                _ = Lists.Sounds.Remove(Sound);
            }
        }

        private async void Flags_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement Control)
            {
                Cfg.Var.SelectedSound = (XVSound)Control.DataContext;
            }
            _ = await ViewSoundFlags.ShowAsync();
            // if (ContentDialogResult _ == ContentDialogResult.Primary)
            // {
            //     // do something;
            // }
        }

        private async void Sample_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement Control && Control.DataContext is XVSound XS)
            {
                Cfg.Var.SelectedSound = XS;
                if (XS.SampleIndex >= Lists.Samples.Count) { XS.SampleIndex = Lists.Samples.Count - 1; }
            }
            _ = await AddSampleDialog.ShowAsync();
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
            if (e.DataView.Contains(StandardDataFormats.StorageItems)
                && (await e.DataView.GetStorageItemsAsync())[0] is StorageFile Sample
                && AddSample(Sample) is int NewIndex && NewIndex > -1
                && Cfg.Var.SelectedSound is XVSound Sound)
            {
                Sound.SampleIndex = NewIndex;
                Sound.Sample = Sample.Name;
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
            else if (e.DataView.Properties["TeamBonus"] is Bonus TB)
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = $"Add {TB.Sound}";
            }
            else if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = $"Add sound(s)";
            }
        }

        private async void XVoiceList_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties["Character"] is not null) // is Character SC
            {
                // IntName = Herostat.GetInternalName(SC.Path);
                AddSound(new XVSound
                {
                    IntName = _intName,
                    Pref = Events.LastCharPrefix
                });
                _ = Lists.XVInternalNames.Add(_intName);
            }
            else if (e.DataView.Properties["TeamBonus"] is Bonus TB)
            {
                AddSound(new XVSound
                {
                    Pref = Events.XVprefix.TEAM,
                    Hash = TB.Sound?.Length > 18 ? TB.Sound : $"COMMON/TEAM_BONUS_{TB.Name}".Replace(' ', '_')
                });
            }
            else if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var Items = await e.DataView.GetStorageItemsAsync();
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i] is StorageFile Sample)
                    {
                        AddSampleSound(Sample);
                    }
                    else if (Items[i] is StorageFolder Dir)
                    {
                        foreach (StorageFile SubFile in (await Dir.GetFilesAsync()))
                        {
                            if (SubFile.ContentType == "audio/wav") { AddSampleSound(SubFile); }
                        }
                    }
                }
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            SoundNotPlayed.IsOpen = false;
            if (XVoiceList.SelectedItem is XVSound Sound && Lists.Samples[Sound.SampleIndex] is JsonSample S && S.File is string Filename)
            {
                SoundNotPlayed.IsOpen = !TemporaryPlayer.Play(Path.IsPathFullyQualified(Filename) ? Filename
                    : Path.Combine(OHSpath.XvoiceDir, Filename), S);
            }
        }

        private void Sample_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lists.Samples[SampleComboBox.SelectedIndex].Sample_rate;
            // SampleRateComboBox.SelectedIndex = SampleRates.IndexOf(ZsndLists.Samples[SampleComboBox.SelectedIndex].Sample_rate);
            SampleRateComboBox.SelectedItem = ((JsonSample)SampleComboBox.SelectedItem).Sample_rate;
        }

        private void SampleRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Lists.Samples[SampleComboBox.SelectedIndex].Sample_rate = (uint)SampleRateComboBox.SelectedItem;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Cfg.Var.PropertyChanged -= FloatingCharacter_Changed;
        }
    }
}
