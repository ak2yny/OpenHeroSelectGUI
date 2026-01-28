using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Available Character class for TreeView structure.
    /// </summary>
    internal class AvailableCharacter // : ObservableObject
    {
        internal Character Content { get; }

        internal ObservableCollection<AvailableCharacter> Children { get; set; } = [];

        internal ObservableCollection<AvailableCharacter> ParentCollection { get; }

        internal bool HasChildren => Children.Count != 0;

        //[ObservableProperty]
        //public partial bool IsExpanded { get; set; }

        public AvailableCharacter()
        {
            Content = new();
            ParentCollection = [];
        }
        public AvailableCharacter(ObservableCollection<AvailableCharacter> parent, Character content)
        {
            Content = content;
            ParentCollection = parent;
        }
        //public override string? ToString() => Content.ToString();
    }
    /// <summary>
    /// Character class with the relative <see cref="Path"/> to the herostat (from the herostat/xml folder) and the display <see cref="Name"/> (herostat file name).
    /// </summary>
    internal class Character
    {
        internal string? Path { get; set; }
        internal string? Name { get; set; }
    }
    /// <summary>
    /// Selected Character Class
    /// </summary>
    internal partial class SelectedCharacter : ObservableObject
    {
        internal string Loc { get; private set; } = "00";

        internal int LocNum
        {
            get;
            set
            {
                Loc = $"{value:00}";
                field = value;
            }
        }

        internal string? Character_Number { get; set; }

        internal string? Character_Name { get; set; }

        internal string? Path { get; set; }

        [ObservableProperty]
        internal partial bool Unlock { get; set; }

        [ObservableProperty]
        internal partial bool Starter { get; set; }

        [ObservableProperty]
        internal partial CSSEffect? Effect { get; set; }

        [ObservableProperty]
        internal partial bool NumClash { get; set; }
    }
    /// <summary>
    /// Skin Details. Example: Skin 0301 => CharNum = "03", Number = "01", Name = "Modern".
    /// </summary>
    internal class SkinDetails(string CN, int Num, string N, Microsoft.UI.Xaml.Input.StandardUICommand C)
    {
        internal string CharNum { get; set; } = CN;
        internal string Number { get; set; } = Num == -1 ? "" : $"{Num:00}";
        internal string Name { get; set; } = N;

        internal int Num = Num;
        internal Microsoft.UI.Xaml.Input.StandardUICommand Command = C;
    }
}
