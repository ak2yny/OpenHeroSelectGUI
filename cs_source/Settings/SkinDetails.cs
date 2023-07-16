using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Skin Details. Example: Skin 0301 > CharNum = "03", Number = "01", Name = "Modern".
    /// </summary>
    public class SkinDetails
    {
        public string CharNum = "";
        public string Number = "";
        public string Name = "";
        public StandardUICommand? Command;
    }
    public class SkinData
    {
        public ObservableCollection<SkinDetails> SkinsList { get; set; } = new ObservableCollection<SkinDetails>();

        public static SkinData Instance { get; set; } = new();
    }
}
