using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace OpenHeroSelectGUI.Controls
{
    // Binding Content Property to Controls, doesn't work with explicitly defined SettingsCardContent.Content
    [ContentProperty(Name = "Controls")]
    public sealed partial class SettingsCardContent : UserControl
    {
        public object Controls
        {
            get => GetValue(ControlsProperty);
            set => SetValue(ControlsProperty, value);
        }

        public static readonly DependencyProperty ControlsProperty =
            DependencyProperty.Register(nameof(Controls), typeof(object), typeof(SettingsCardContent), null);

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCardContent), null);

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsCardContent), null);

        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(object), typeof(SettingsCardContent), null);

        public SettingsCardContent()
        {
            InitializeComponent();
        }
    }
}
