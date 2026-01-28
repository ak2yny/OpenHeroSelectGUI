using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;

namespace OpenHeroSelectGUI.Controls
{
    public sealed partial class TextInputDialog : ContentDialog
    {
        public string InputText => NewName.Text;

        private bool _enter;

        public TextInputDialog()
        {
            InitializeComponent();
        }

        public async Task<ContentDialogResult> ShowAsync(string ReplaceText = "")
        {
            if (ReplaceText.Length > 0) { NewName.Text = ReplaceText; NewName.SelectAll(); }
            _enter = false;
            ContentDialogResult result = await base.ShowAsync();
            return _enter ? ContentDialogResult.Secondary : result;
        }

        private void NewName_Entered(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Hide();
            _enter = true;
            args.Handled = true;
        }
    }
}
