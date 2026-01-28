using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace OpenHeroSelectGUI.Settings
{
    public partial class LocationButton : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsChecked { get; set; }

        public string NumberString { get; private set; } = "00";

        public int Number
        {
            get;
            set
            {
                NumberString = $"{value:00}";
                field = value;
            }
        }

        public Thickness Margin { get; set; }
    }
}
