using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Zsnd_UI.lib;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The tree view with all files in the herostat folder
    /// </summary>
    public sealed partial class AvailableCharacters : Page
    {
        public Cfg Cfg { get; set; } = new();
        private bool HideRosters { get; set; }
        private bool FilterVoice { get; set; }

        public AvailableCharacters()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HideRosters = Cfg.GUI.Home.StartsWith("NavItem_SkinEditor") || Cfg.GUI.Home.StartsWith("NavItem_XVoice");
            if (HideRosters) { AvailRstrs.Visibility = Visibility.Collapsed; }
            if (!Cfg.GUI.Home.StartsWith("NavItem_XVoice")) { CharsNeedVoice.Visibility = Visibility.Collapsed; }
            AvailChange(Cfg.GUI.AvailChars);
        }
        /// <summary>
        /// Load characters from the herostat folder
        /// </summary>
        private void PopulateAvailable(bool KeepFilter = false)
        {
            DirectoryInfo folder = new(OHSpath.HsFolder);
            string rosters = Path.Combine(OHSpath.CD, OHSpath.Game, "rosters");
            bool LoadChars = Cfg.GUI.AvailChars || HideRosters;
            string[] NewAvailable = LoadChars && folder.Exists
                ? [.. folder.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Select(f => Path.GetRelativePath(folder.FullName, f.FullName)[..^f.Extension.Length]
                            .Replace(Path.DirectorySeparatorChar, '/'))
                            .Distinct(StringComparer.OrdinalIgnoreCase)]
                : !LoadChars && Directory.Exists(rosters)
                ? [.. Directory.EnumerateFiles(rosters, "*.cfg").Select(f => Path.GetFileNameWithoutExtension(f))]
                : [];
            if (NewAvailable != Cfg.Roster.Available)
            {
                Cfg.Roster.Available = NewAvailable;
                if (NewAvailable.Length > 0)
                {
                    if (!KeepFilter && TVsearch.Text.Length > 0)
                    {
                        TVsearch.Text = "";
                    }
                    else
                    {
                        PopulateAvailable(FilterVoice ? [..(!KeepFilter || TVsearch.Text.Length == 0 ? NewAvailable
                                : NewAvailable.Where(a => a.Contains(TVsearch.Text, StringComparison.CurrentCultureIgnoreCase)))
                                .Where(n => !ZsndLists.XVInternalNames.Contains(Herostat.GetInternalName(n), StringComparer.OrdinalIgnoreCase))]
                            : !KeepFilter || TVsearch.Text.Length == 0 ? NewAvailable
                            : [.. NewAvailable.Where(a => a.Contains(TVsearch.Text, StringComparison.CurrentCultureIgnoreCase))]);
                    }
                }
                else
                {
                    TreeViewNode NA = new() { Content = "Drop herostats here to install" };
                    trvAvailableChars.RootNodes.Clear();
                    trvAvailableChars.RootNodes.Add(NA);
                }
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
        private void PopulateAvailable(TreeViewNode Parent, string RemainingPath, string PathInfo)
        {
            if (AvailRstrs.IsChecked ?? false)
            {
                Parent.Children.Add(new TreeViewNode() { Content = PathInfo });
                return;
            }
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
                switch (Node.Length)
                {
                    case > 1:
                        Parent.Children.Insert(
                            Math.Max(Parent.Children.IndexOf(Parent.Children.FirstOrDefault(n => !n.HasChildren)), 0), child);
                        break;
                    default:
                        Parent.Children.Add(child);
                        break;
                }
            }
            if (Node.Length > 1) { PopulateAvailable(child, Node[1], PathInfo); }
        }
        /// <summary>
        /// Switch between available characters and rosters. <see langword="True"/> enables characters, <see langword="false"/> rosters.
        /// </summary>
        private void AvailChange(bool IsChar)
        {
            IsChar = HideRosters || IsChar;
            FilterVoice = Cfg.GUI.Home.StartsWith("NavItem_XVoice") && !FilterVoice;
            AvailChars.IsChecked = !(bool)(CharsNeedVoice.IsChecked = !(bool)(AvailRstrs.IsChecked = !IsChar) && FilterVoice) && IsChar;
            BrowseButton.Visibility = IsChar ? Visibility.Visible : Visibility.Collapsed;
            if (!HideRosters) { Cfg.GUI.AvailChars = IsChar; }
            PopulateAvailable(HideRosters);
        }
        /// <summary>
        /// Search typing
        /// </summary>
        private void TVsearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (Cfg.Roster.Available is not null)
            {
                PopulateAvailable(FilterVoice ? [..(sender.Text.Length == 0 ? Cfg.Roster.Available
                        : Cfg.Roster.Available.Where(a => a.Contains(sender.Text, StringComparison.CurrentCultureIgnoreCase)))
                        .Where(n => !ZsndLists.XVInternalNames.Contains(Herostat.GetInternalName(n), StringComparer.OrdinalIgnoreCase))]
                    : sender.Text.Length == 0 ? Cfg.Roster.Available
                    : [.. Cfg.Roster.Available.Where(a => a.Contains(sender.Text, StringComparison.CurrentCultureIgnoreCase))]);
            }
        }
        /// <summary>
        /// Open file picker to select a herostat file and add it to the herostat folder, then re-load the available characters from disk.
        /// </summary>
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new();
            filePicker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
            System.Collections.Generic.IReadOnlyList<StorageFile> Mods = await filePicker.PickMultipleFilesAsync();
            if (Mods != null)
            {
                for (int i = 0; i < Mods.Count; i++)
                {
                    Install(Mods[i]);
                }
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
                AvailableCharactersDropArea.Visibility = Visibility.Visible;
            }
        }
        /// <summary>
        /// Hide the drop visuals
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AvailableCharacters_DragLeave(object sender, DragEventArgs e)
        {
            AvailableCharactersDropArea.Visibility = Visibility.Collapsed;
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
                    if (Items[i] is StorageFile Mod)
                    {
                        Install(Mod);
                    }
                }
                if (i > 0) { PopulateAvailable(); }
            }
            AvailableCharactersDropArea.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// Install a <paramref name="Mod"/>, including herostat. <paramref name="Mod"/> can be a heroatat, which then is installed instead.
        /// </summary>
        private void Install(StorageFile Mod)
        {
            if (Util.Run7z(Mod.Path, Mod.DisplayName) is string ExtModPath)
            {
                CopyInfo.IsOpen = HSinfo.IsOpen = false;
                int i = 0, c = 0;
                foreach (DirectoryInfo Source in OHSpath.GetModSource(ExtModPath))
                {
                    if (Herostat.GetFiles(Source).FirstOrDefault() is FileInfo Hs
                        && Herostat.Add(Hs.FullName, Source.Name, Hs.Extension))
                    {
                        i++;
                    }
                    if (OHSpath.GetModTarget(Source, Source.Name, Mod.Path) is string Target
                        && OHSpath.CopyFilesRecursively(Source, Target))
                    {
                        c++;
                    }
                }
                CopyInfo.IsOpen = c == 0;
                HSinfo.IsOpen = !(HSsuccess.IsOpen = i > 0);
            }
            else
            {
                HSsuccess.IsOpen = Herostat.Add(Mod);
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
                args.Data.Properties.Add("Character", SC);
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
            Cfg.Var.FloatingCharacter = null;
            if (args.InvokedItem is TreeViewNode Selected && Selected.Content is Character SC && SC.Path is not null)
            {
                if (Selected.Children.Count > 0)
                {
                    Selected.IsExpanded = !Selected.IsExpanded;
                }
                else
                {
                    Cfg.Var.FloatingCharacter = SC.Path;
                }
            }
        }
        /// <summary>
        /// Add the selected character to the selected list on double-tap, or, if a parent is double-tapped, add all direct leaf children
        /// </summary>
        private void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (HideRosters) { return; }
            if (Cfg.Var.FloatingCharacter is not null)
            {
                _ = AddToSelected(Cfg.Var.FloatingCharacter);
                UpdateClashes();
            }
            else if (trvAvailableChars.SelectedItem is TreeViewNode Node)
            {
                if (!Cfg.GUI.AvailChars && Node.Content is string Roster)
                {
                    Cfg.MUA.MenulocationsValue = Cfg.OHS.RosterValue = CfgCmd.FilterDefaultRV(Roster);
                    LoadRosterVal(Roster);
                }
                else
                {
                    for (int i = 0; i < Node.Children.Count; i++)
                    {
                        if (Node.Children[i].Children.Count == 0 && Node.Children[i].Content is Character SC)
                            _ = AddToSelected(SC.Path);
                    }
                    UpdateClashes();
                }
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
        private static void RemoveCharacters(TreeViewNode Node)
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
        private static void RemoveCharacter(TreeViewNode Node)
        {
            try
            {
                if (Node.Content is Character Chr
                    && !string.IsNullOrEmpty(Chr.Path)
                    && Herostat.GetFile(Chr.Path) is FileInfo HS)
                {
                    HS.Delete();
                }
                else if (Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", $"{Node.Content}.cfg") is string r
                    && File.Exists(r))
                {
                    File.Delete(r);
                }
            }
            finally
            {
                _ = Node.Parent.Children.Remove(Node);
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

        private void Reload_Available(object sender, RoutedEventArgs e)
        {
            PopulateAvailable(true);
        }

        private void Avail_Click(object sender, RoutedEventArgs e) => AvailChange(!Cfg.GUI.AvailChars);
    }
}
