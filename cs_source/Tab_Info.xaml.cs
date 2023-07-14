using Microsoft.UI.Xaml.Controls;
using OpenHeroSelectGUI.Settings;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Info Page: Contains all information about the app, including credits, links, etc.
    /// </summary>
    public sealed partial class Tab_Info : Page
    {
        public Cfg Cfg { get; } = new();
        public Tab_Info()
        {
            InitializeComponent();
        }
    }
}
