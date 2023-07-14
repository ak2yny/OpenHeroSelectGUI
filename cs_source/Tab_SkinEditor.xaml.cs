using Microsoft.UI.Xaml.Controls;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The Skin details tab that uses the skin details pane (page) to show skin details.
    /// </summary>
    public sealed partial class Tab_SkinEditor : Page
    {
        public Tab_SkinEditor()
        {
            InitializeComponent();
            _ = AvailableCharacters.Navigate(typeof(AvailableCharacters));
            _ = SkinDetailsPage.Navigate(typeof(SkinDetailsPage));
        }
    }
}
