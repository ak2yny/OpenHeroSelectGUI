using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using static OpenHeroSelectGUI.Settings.InternalSettings;

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
            DirectoryInfo folder = new(Path.Combine(cdPath, Cfg.GUI.Game, Cfg.OHS.HerostatFolder));
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
                Available Root = new();
                foreach (string PathInfo in Cfg.Roster.Available)
                {
                    PopulateAvailable(Root, PathInfo, PathInfo);
                }
                Cfg.Roster.AvailableCharacterList = Root.Children;
            }
            else
            {
                Available NA = new() { Character = new Character() { Name = "Drop herostats here to install", Path = "" } };
                Cfg.Roster.AvailableCharacterList.Clear();
                Cfg.Roster.AvailableCharacterList.Add(NA);
            }
        }
        /// <summary>
        /// Generate Tree View Data from files list
        /// </summary>
        private void PopulateAvailable(Available Parent, string RemainingPath, string PathInfo)
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
            if (Node.Length > 1)  PopulateAvailable(child, Node[1], PathInfo);
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
        /// <summary>
        /// Tree View: Old code reading from disk and adding directly to the TV without binding.
        /// </summary>
        private void PopulateAvailableOld()
        {
            trvAvailableChars.RootNodes.Clear();
            DirectoryInfo folder = new(Path.Combine("Same"));
            if (folder.Exists)
            {
                TreeViewNode Root = new()
                {
                    Content = "Characters"
                };
                PopulateAvailable(folder, Root);
                foreach (TreeViewNode root in Root.Children)
                {
                    trvAvailableChars.RootNodes.Add(root);
                }
            }
            else
            {
                TreeViewNode node = new()
                {
                    Content = "Drop herostats here to install"
                };
                trvAvailableChars.RootNodes.Add(node);
            }
        }
        private void PopulateAvailable(DirectoryInfo folder, TreeViewNode nodes)
        {
            //It performs well on my PC, even using several enumeratefiles.
            if (folder.EnumerateFiles("*", SearchOption.AllDirectories).Any())
            {
                foreach (DirectoryInfo subfolder in folder.EnumerateDirectories())
                {
                    TreeViewNode node = new()
                    {
                        Content = subfolder.Name
                    };
                    nodes.Children.Add(node);

                    PopulateAvailable(subfolder, node);
                }
                foreach (var file in folder.EnumerateFiles().Select(f => Path.GetFileNameWithoutExtension(f.Name)).Distinct().ToImmutableSortedSet())
                {
                    Character Char = new()
                    {
                        Name = file,
                        Path = Path.GetDirectoryName(file)
                    };
                    TreeViewNode node = new()
                    {
                        Content = Char
                    };
                    nodes.Children.Add(node);
                }
            }
        }
        /// <summary>
        /// Tree View: Old code for getting the path when creating the TV from disk.
        /// </summary>
        private void TreeViewNodeGetPath(TreeViewNode node)
        {
            Cfg.Dynamic.FloatingCharacter = $"/{node.Content}" + Cfg.Dynamic.FloatingCharacter;
            if (node.Parent.Depth > 0)
            {
                TreeViewNodeGetPath(node.Parent);
            }
            else
            {
                Cfg.Dynamic.FloatingCharacter = $"{node.Parent.Content}" + Cfg.Dynamic.FloatingCharacter;
            }
        }
    }
}
