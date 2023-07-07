using Microsoft.UI.Xaml;

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

            // Possibly add error handling with a function: UnhandledException += App_UnhandledException;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // WinUI Window with more convenient methods?? Enables theme selection.
            // public static WinUIEx.WindowEx Main { get; } = new Main();
            m_window = new Main();
            m_window.Activate();
            // Custom window startup properties.
            //m_window.AppWindow.Resize(new SizeInt32(1150, 640));
            //public static UIElement? AppTitlebar { get; set; }
        }

        private Window? m_window;
    }
}