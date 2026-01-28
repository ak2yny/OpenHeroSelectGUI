using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Class with <see cref="Message"/> (<see cref="string"/>) and <see cref="IsOpen"/> (<see cref="bool"/>) properties for message binding, <see cref="ObservableObject"/>.
    /// </summary>
    internal partial class MessageItem : ObservableObject
    {
        [ObservableProperty]
        internal partial string? Message { get; set; }
        [ObservableProperty]
        internal partial bool IsOpen { get; set; }
        partial void OnMessageChanged(string? value) => IsOpen = !string.IsNullOrWhiteSpace(value);
    }
    /// <summary>
    /// <see cref="MessageItem"/>s for sharing among pages.
    /// </summary>
    internal partial class StaticMessages
    {
        internal static MessageItem SE_Error { get; set; } = new();
    
        internal static MessageItem SE_Info { get; set; } = new();
    
        internal static MessageItem SE_Success { get; set; } = new();
    
        internal static MessageItem SE_Warning { get; set; } = new();
    
        internal static MessageItem SE_WarnPkg { get; set; } = new();
    }
    /// <summary>
    /// <see cref="MessageItem"/>s for binding, so their message can be changed more dynamically.
    /// </summary>
    internal class Messages
    {
        internal MessageItem SE_Error { get; set; } = StaticMessages.SE_Error;

        internal MessageItem SE_Info { get; set; } = StaticMessages.SE_Info;

        internal MessageItem SE_Success { get; set; } = StaticMessages.SE_Success;

        internal MessageItem SE_Warning { get; set; } = StaticMessages.SE_Warning;

        internal MessageItem SE_WarnPkg { get; set; } = StaticMessages.SE_WarnPkg;
    }
}
