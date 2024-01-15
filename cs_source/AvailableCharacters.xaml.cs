using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
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
        /// Load characters from the herostat folder
        /// </summary>
        private void PopulateAvailable()
        {
            DirectoryInfo folder = new(OHSpath.HsFolder);
            if (folder.Exists)
            {
                string[] NewAvailable = folder.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Select(f => Path.ChangeExtension(f.FullName, null)
                    .Replace(folder.FullName, string.Empty)
                    .Replace('\\', '/')
                    .TrimStart('/'))
                    .Distinct()
                    .ToImmutableSortedSet()
                    .ToArray();
                if (NewAvailable != Cfg.Roster.Available)
                {
                    Cfg.Roster.Available = NewAvailable;
                    PopulateAvailable(NewAvailable);
                }
            }
            else
            {
                TreeViewNode NA = new() { Content = "Drop herostats here to install" };
                trvAvailableChars.RootNodes.Clear();
                trvAvailableChars.RootNodes.Add(NA);
            }
        }
        /// <summary>
        /// Populate the <see cref="TreeView"/> with the file structure from the <paramref name="AvailableList"/> (a <see langword="string[]"/> of paths)
        /// </summary>
        private void PopulateAvailable(string[] AvailableList)
        {
            TreeViewNode Root = new();
            for (int i = 0; i < AvailableList.Length; i++)
            {
                PopulateAvailable(Root, AvailableList[i], AvailableList[i]);
            }
            trvAvailableChars.RootNodes.Clear();
            for (int i = 0; i < Root.Children.Count; i++)
            {
                trvAvailableChars.RootNodes.Add(Root.Children[i]);
            }
        }
        /// <summary>
        /// Generate the <see cref="TreeView"/> structure from a file <paramref name="PathInfo"/>. Add <paramref name="RemainingPath"/> to <paramref name="Parent"/> recursively.
        /// </summary>
        public static void PopulateAvailable(TreeViewNode Parent, string RemainingPath, string PathInfo)
        {
            string[] Node = RemainingPath.Split('/', 2);
            TreeViewNode? child = Parent.Children.SingleOrDefault(x => ((Character)x.Content).Name == Node[0]);
            if (child == null)
            {
                Character CharInfo = new()
                {
                    Name = Node[0],
                    Path = (Node.Length > 1) ? "" : PathInfo
                };
                child = new TreeViewNode() { Content = CharInfo };
                Parent.Children.Add(child);
            }
            if (Node.Length > 1) { PopulateAvailable(child, Node[1], PathInfo); }
        }
        /// <summary>
        /// Search typing
        /// </summary>
        private void TVsearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (Cfg.Roster.Available is not null)
            {
                PopulateAvailable(Cfg.Roster.Available.Where(a => a.Contains(sender.Text, StringComparison.InvariantCultureIgnoreCase)).ToArray());
            }
        }
        /// <summary>
        /// Open file picker to select a herostat file and add it to the herostat folder, then re-load the available characters from disk.
        /// </summary>
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // WIP: Should this accept multiple selected files? Let's see how others react.
            string? Herostat = await CfgCmd.LoadDialogue("*");
            if (Herostat != null)
            {
                AddHerostat(Herostat);
                PopulateAvailable();
            }
        }
        /// <summary>
        /// Re-load the available characters from disk
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
                e.DragUIOverride.Caption = "Add herostat(s)";
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
                int i;
                for (i = 0; i < Items.Count; i++)
                {
                    if (Items[i] is StorageFile Herostat) { AddHerostat(Herostat); }
                }
                if (i > 0) { PopulateAvailable(); }
            }
        }
        /// <summary>
        /// Define the allowed drag items 
        /// </summary>
        private void DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
        {
            if (args.Items[0] is TreeViewNode Selected && Selected.Content is Character SC)
            {
                args.Cancel = Selected.Children.Count > 0;
                Cfg.Var.FloatingCharacter = SC.Path;
            }
            else
            {
                args.Cancel = true;
            }
        }
        /// <summary>
        /// Define the leaf nodes as characters, expand/collapse parent nodes.
        /// </summary>
        private void OnSelectionChanged(TreeView AC, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is TreeViewNode Selected && Selected.Content is Character SC && SC.Path is string CharInfo)
            {
                Cfg.Var.FloatingCharacter = CharInfo;
                if (Selected.Children.Count > 0)
                {
                    Selected.IsExpanded = !Selected.IsExpanded;
                    Cfg.Var.FloatingCharacter = null;
                }
            }
        }
        /// <summary>
        /// Add the selected character to the selected list on double-tap, or, if a parent is double-tapped, add all direct leaf children
        /// </summary>
        private void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (Cfg.Var.FloatingCharacter is not null)
            {
                _ = AddToSelected(Cfg.Var.FloatingCharacter);
                UpdateClashes();
            }
            else if (trvAvailableChars.SelectedItem is TreeViewNode Node)
            {
                for (int i = 0; i < Node.Children.Count; i++)
                {
                    if (Node.Children[i].Children.Count == 0 && Node.Children[i].Content is Character SC)
                        _ = AddToSelected(SC.Path);
                }
                UpdateClashes();
            }
        }
        /// <summary>
        /// Delete the selected nodes recursively
        /// </summary>
        private void TreeViewItems_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            foreach (TreeViewNode Node in trvAvailableChars.SelectedItems.OfType<TreeViewNode>())
            {
                RemoveCharacters(Node);
            }
            args.Handled = true;
        }
        /// <summary>
        /// Remove the <paramref name="Node"/> and all its child nodes, including the herostat files.
        /// </summary>
        private void RemoveCharacters(TreeViewNode Node)
        {
            for (int i = 0; i < Node.Children.Count;)
            {
                RemoveCharacters(Node.Children[i]);
            }
            RemoveCharacter(Node);
        }
        /// <summary>
        /// Remove the <paramref name="Node"/> and the corresponding herostat file according to the node's path property.
        /// </summary>
        private void RemoveCharacter(TreeViewNode Node)
        {
            if (Node.Content is Character Chr
                && Node.Parent.Children.Remove(Node)
                && !string.IsNullOrEmpty(Chr.Path)
                && Herostat.GetFile(Chr.Path) is FileInfo HS)
            {
                HS.Delete();
            }
        }
        /// <summary>
        /// Window-wide page shortcut: F3 = search
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
