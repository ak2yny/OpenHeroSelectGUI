using Microsoft.UI.Xaml;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            UnhandledException += App_UnhandledException;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindow = new Main();
            MainWindow.Activate();
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Handle the exception here, e.Exception provides the exception details
            using StreamWriter sw = File.AppendText(Path.Combine(Directory.GetCurrentDirectory(), "error.log"));
            sw.WriteLine("");
            sw.Write(e.Exception);
            sw.Write(e.Message);
            e.Handled = true; // Set to true to indicate that the exception has been handled
            MainWindow?.Close();
        }

        public static Window? MainWindow { get; set; }
    }
}
