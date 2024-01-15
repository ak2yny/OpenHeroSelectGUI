using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;

namespace OpenHeroSelectGUI.Settings
{
    public class Character
    {
        public string? Path { get; set; }
        public string? Name { get; set; }

        public override string ToString()
        {
            return Name is null ? "" : Name;
        }
    }
    /// <summary>
    /// Skin Details. Example: Skin 0301 => CharNum = "03", Number = "01", Name = "Modern".
    /// </summary>
    public class SkinDetails
    {
        public string CharNum = "";
        public string Number = "";
        public string Name = "";
        public StandardUICommand? Command;
    }
    public class TeamBonus
    {
        public string? Name { get; set; }
        public string? Descbonus { get; set; }
        public string? Sound { get; set; }
        public string? Skinset { get; set; }
        public ObservableCollection<TeamMember>? Members { get; set; }
        public StandardUICommand? Command;
    }
    public class TeamMember
    {
        public string? Name { get; set; }
        public string? Skin { get; set; }
    }
}
