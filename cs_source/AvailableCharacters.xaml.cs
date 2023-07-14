using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using static OpenHeroSelectGUI.Settings.CfgCommands;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The tree view with all files in the herostat folder
    /// </summary>
    public sealed partial class AvailableCharacters : Page
    {
        public Cfg Cfg { get; set; } = new();
        public AvailableCharacters()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            PopulateAvailable();
        }
        /// <summary>
        /// Load characters from disk
        /// </summary>
        private void PopulateAvailable()
        {
            DirectoryInfo folder = new(GetHerostatFolder());
            if (folder.Exists)
            {
                Cfg.Roster.Available = folder.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Select(f => Path.ChangeExtension(f.FullName, null)
                    .Replace(folder.FullName, string.Empty)
                    .Replace('\\', '/')
                    .TrimStart('/'))
                    .Distinct()
                    .ToImmutableSortedSet()
                    .ToArray();
                PopulateAvailable(Cfg.Roster.Available);
            }
            else
            {
                Available NA = new() { Character = new Character() { Name = "Drop herostats here to install", Path = "" } };
                Cfg.Roster.AvailableCharacterList.Clear();
                Cfg.Roster.AvailableCharacterList.Add(NA);
            }
        }
        /// <summary>
        /// Generate Tree View Data from a files list
        /// </summary>
        private void PopulateAvailable(string[] AvailableList)
        {
            Available Root = new();
            for (int i = 0; i < AvailableList.Length; i++)
            {
                PopulateAvailable(Root, AvailableList[i], AvailableList[i]);
            }
            Cfg.Roster.AvailableCharacterList.Clear();
            for (int i = 0; i < Root.Children.Count; i++)
            {
                Cfg.Roster.AvailableCharacterList.Add(Root.Children[i]);
            }
        }
        /// <summary>
        /// Generate the Tree View structure from a file path
        /// </summary>
        public static void PopulateAvailable(Available Parent, string RemainingPath, string PathInfo)
        {
            string[] Node = RemainingPath.Split(new[] { '/' }, 2);
            Available? child = Parent.Children.SingleOrDefault(x => x.Character.Name == Node[0]);
            if (child == null)
            {
                string PathToAdd = (Node.Length > 1) ? "" : PathInfo;
                Character CharInfo = new()
                {
                    Name = Node[0],
                    Path = PathToAdd
                };
                child = new Available() { Character = CharInfo, Children = new ObservableCollection<Available>() };
                Parent.Children.Add(child);
            }
            if (Node.Length > 1) PopulateAvailable(child, Node[1], PathInfo);
        }
        /// <summary>
        /// Search typing
        /// </summary>
        private void TVsearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (Cfg.Roster.Available is not null)
            {
                IEnumerable<string> Filtered = Cfg.Roster.Available.Where(a => a.Contains(sender.Text, StringComparison.InvariantCultureIgnoreCase));
                PopulateAvailable(Filtered.ToArray());
            }
        }
        /// <summary>
        /// Add a herostat to disk, and re-load the available characters from disk.
        /// </summary>
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string? Herostat = await LoadDialogue("*");
            if (Herostat != null)
            {
                AddHerostat(Herostat);
                PopulateAvailable();
            }
        }
        /// <summary>
        /// Reload the available characters from disk
        /// </summary>
        private void BtnReload_Click(object sender, RoutedEventArgs e) => PopulateAvailable();
        /// <summary>
        /// Define the allowed drop info
        /// </summary>
        private void AvailableCharacters_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "Add herostat";
            }
        }
        /// <summary>
        /// Define the drop event for dropped herostats.
        /// </summary>
        private async void AvailableCharacters_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var Items = await e.DataView.GetStorageItemsAsync();
                if (Items.Count > 0 && Items[0] is StorageFile Herostat)
                {
                    AddHerostat(Herostat);
                    PopulateAvailable();
                }
            }
        }
        /// <summary>
        /// Define the allowed drag items 
        /// </summary>
        private void DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
        {
            if (args.Items[0] is Available Selected && Selected.Character is Character SC)
            {
                args.Cancel = Selected.Children.Count > 0;
                Cfg.Dynamic.FloatingCharacter = SC.Path;
            }
        }
        /// <summary>
        /// Define the leaf nodes as characters, expand/collapse parent nodes.
        /// </summary>
        private void OnSelectionChanged(TreeView AC, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is Available Selected && Selected.Character is Character SC && SC.Path is string CharInfo)
            {
                Cfg.Dynamic.FloatingCharacter = CharInfo;
                if (Selected.Children.Count > 0)
                {
                    Selected.IsExpanded = !Selected.IsExpanded;
                    Cfg.Dynamic.FloatingCharacter = null;
                }
            }
        }

        private void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (Cfg.Dynamic.FloatingCharacter is not null)
            {
                AddToSelected(Cfg.Dynamic.FloatingCharacter);
            }
        }
        /// <summary>
        /// Page shortcuts. Only F3 for search for now, so this has an unique handler.
        /// </summary>
        private void TVsearch_Shortcut_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!args.Handled && TVsearch.FocusState != FocusState.Programmatic)
            {
                TVsearch.Focus(FocusState.Programmatic);
                args.Handled = true;
            }
        }
    }
}
