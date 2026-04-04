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
            try
            {
                using StreamWriter sw = File.AppendText(Path.Combine(Directory.GetCurrentDirectory(), "error.log"));
                sw.Write($@"
====================================================
Logged  {System.DateTime.Now}
Version {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}
----------------------------------------------------
{e.Exception /*Provides details, less details: e.Message*/}
");
                sw.Close();
            }
            catch { }
            e.Handled = true; // Set to true to indicate that the exception has been handled
            MainWindow?.Close();
        }

        public static Window? MainWindow { get; set; }
    }
}
