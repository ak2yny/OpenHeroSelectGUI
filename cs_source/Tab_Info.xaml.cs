using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;

namespace OpenHeroSelectGUI
{
    public class GHAsset
    {
        public int size { get; set; }
        public int download_count { get; set; }
        public string? browser_download_url { get; set; }
    }

    public class GHRelease
    {
        public string? tag_name { get; set; }
        public bool draft { get; set; }
        public bool prerelease { get; set; }
        public string? updated_at { get; set; }
        public System.Collections.Generic.List<GHAsset>? assets { get; set; }
        public string? body { get; set; }
    }
    /// <summary>
    /// Info Page: Contains all information about the app, including credits, links, etc.
    /// </summary>
    public sealed partial class Tab_Info : Page
    {
        public Cfg Cfg { get; } = new();
        private HttpClient? client;
        private GHRelease? update_info;
        private System.Threading.CancellationTokenSource? cancelts;

        public Tab_Info()
        {
            InitializeComponent();
            Info_Version.Text = CfgCmd.GetVersionDescription();
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            Info_Progress.Visibility = Visibility.Visible;
            Info_Progress.IsIndeterminate = true;
            client = new();
            HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://api.github.com/repos/ak2yny/OpenHeroSelectGUI/releases/latest");
            requestMessage.Headers.Add("User-Agent", "OpenHeroSelectGUI");
            HttpResponseMessage response = await client.SendAsync(requestMessage).WaitAsync(TimeSpan.FromMinutes(1));
            UpdateFailedAccess.IsOpen = !response.IsSuccessStatusCode;
            if (response.IsSuccessStatusCode)
            {
                update_info = await response.Content.ReadFromJsonAsync<GHRelease>().WaitAsync(TimeSpan.FromMinutes(1));
                UpdateFailedRetrieve.IsOpen = update_info is null || update_info.assets is null || update_info.assets.Count == 0;
                if (!UpdateFailedRetrieve.IsOpen)
                {
                    UpdateIsCurrent.IsOpen = update_info!.prerelease || update_info.draft || $"vv{Info_Version.Text}" == update_info.tag_name;
                    if (!UpdateIsCurrent.IsOpen)
                    {
                        UpdateInfoTitle.Text = $"Update available {update_info.tag_name} ({update_info.updated_at})";
                        UpdateInfoBody.Text = update_info.body;
                        UpdateInfo.Visibility = Visibility.Visible;
                        Info_Progress.Visibility = Visibility.Collapsed;
                        return;
                    }
                }
            }
            Info_Progress.Visibility = Visibility.Collapsed;
            client.Dispose(); client = null;
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (client is null || update_info is null) { return; }
            cancelts?.Dispose(); cancelts = new();
            Info_Progress.IsIndeterminate = false;
            Info_Progress.Value = 0;
            Info_Progress.Visibility = CancelButton.Visibility = Visibility.Visible;
            UpdateButton.Visibility = Visibility.Collapsed;
            double factor = 95.0 / update_info.assets![0].size;
            IProgress<double> progress = new Progress<double>(value => Info_Progress.Value = 5.0 + value * factor);

            string Installer = Path.Combine(OHSpath.CD, "OHSGUI.exe");
            string InstallBat = Path.Combine(OHSpath.CD, "OHSGUI.bat");
            try
            {
                using Stream s = await client.GetStreamAsync(update_info.assets[0].browser_download_url).WaitAsync(TimeSpan.FromMinutes(1));
                using FileStream fs = new(Installer, FileMode.Create);
                await s.CopyToWithProgressAsync(fs, update_info.assets[0].size, progress, cancelts.Token);
                fs.Close();
                ContentDialog dialog = new()
                {
                    Title = "Install Update",
                    Content = $"The update has been downloaded to '{Installer}'. Click 'Install now' to save the settings, close the GUI and start the installation.",
                    PrimaryButtonText = "Install now",
                    CloseButtonText = "Install manually",
                    XamlRoot = XamlRoot
                };
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    File.WriteAllText(InstallBat,
                        $"cd \\d \"{OHSpath.CD}\"\n\"{Installer}\" -y -InstallPath=\"{OHSpath.CD}\" && if exist Temp (move OHSGUI.exe Temp) else (del OHSGUI.exe)\nstart OHSGUI/OpenHeroSelectGUI.exe\ndel OHSGUI.bat");
                    _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(InstallBat) { CreateNoWindow = true });
                    Application.Current.Exit();
                    return;
                }
            }
            catch (OperationCanceledException) { try { File.Delete(Installer); } catch { } }
            catch (Exception ex) { UpdateFailed.Message = ex.Message; UpdateFailed.IsOpen = true; } // any errors or cancel are handled by hiding and showing GUI elements
            UpdateButton.Visibility = Visibility.Visible;
            UpdateInfo.Visibility = CancelButton.Visibility = Visibility.Collapsed;
            client.Dispose(); cancelts.Dispose();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cancelts?.Cancel();
        }
    }
}
