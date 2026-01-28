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
using static OpenHeroSelectGUI.Settings.CharacterListCommands;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// The tree view with all files in the herostat folder
    /// </summary>
    public sealed partial class AvailableCharacters : Page
    {
        public Cfg Cfg { get; set; } = new();
        private bool ShowRosters { get; set; }
        private bool IsXvoiceSel { get; set; }
        private bool FilterVoice;
        private bool RenamedWithEnter;

        public AvailableCharacters()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            IsXvoiceSel = Cfg.GUI.Home.StartsWith("NavItem_XVoice");
            ShowRosters = !(IsXvoiceSel || Cfg.GUI.Home.StartsWith("NavItem_SkinEditor"));
            AvailChange(Cfg.GUI.AvailChars);
        }
        /// <summary>
        /// Load characters from the herostat folder, or rosters from the rosters folder, depending on selected tabs.
        /// </summary>
        private void PopulateAvailable(bool KeepFilter = false)
        {
            DirectoryInfo folder = new(OHSpath.HsFolder);
            DirectoryInfo rosters = new(Path.Combine(OHSpath.CD, OHSpath.Game, "rosters"));
            int BasePathLen = folder.FullName.Length + 1;
            bool LoadChars = Cfg.GUI.AvailChars || !ShowRosters;
            string[] NewAvailable = LoadChars && folder.Exists
                ? [.. folder.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Select(f => f.FullName[BasePathLen..^f.Extension.Length])
                            .Distinct(StringComparer.OrdinalIgnoreCase)]
                : !LoadChars && rosters.Exists
                ? [.. rosters.EnumerateFiles("*.cfg").Select(f => f.Name[..^f.Extension.Length])]
                : [];
            Cfg.Roster.Available = NewAvailable;
            if (NewAvailable.Length > 0)
            {
                if (!KeepFilter && TVsearch.Text.Length > 0) { TVsearch.Text = ""; }
                else { PopulateAvailable(Cfg.Roster.Available, TVsearch.Text, !KeepFilter || TVsearch.Text.Length == 0); }
            }
            else
            {
                Cfg.Roster.AvailableTemp.Clear();
                Cfg.Roster.AvailableTemp.Add(new AvailableCharacter([], "Drop herostats here to install"));
            }
        }
        /// <summary>
        /// Filter the available characters and populate the tree view
        /// </summary>
        private void PopulateAvailable(string[]? Available, string Filter, bool NoFilter)
        {
            if (Available is null) { return; } // WIP!
            PopulateAvailable(FilterVoice ? [..(NoFilter ? Available
                    : Available.Where(a => a.Contains(Filter, StringComparison.CurrentCultureIgnoreCase)))
                    .Where(n => !Zsnd.Lib.Lists.XVInternalNames.Contains(new Stats(n).InternalName, StringComparer.OrdinalIgnoreCase))]
                : NoFilter ? Available
                : [.. Available.Where(a => a.Contains(Filter, StringComparison.CurrentCultureIgnoreCase))]);
        }
        /// <summary>
        /// Populate the <see cref="TreeView"/> with the file structure from the <paramref name="AvailableList"/> (a array of paths)
        /// </summary>
        private void PopulateAvailable(string[] AvailableList)
        {
            Cfg.Roster.AvailableTemp.Clear();
            AvailableCharacter Root = new([], "") { Children = Cfg.Roster.AvailableTemp };
            for (int i = 0; i < AvailableList.Length; i++)
            {
                PopulateAvailable(Root, AvailableList[i], AvailableList[i]);
            }
        }
        /// <summary>
        /// Generate the <see cref="TreeView"/> structure from a file <paramref name="PathInfo"/>. Add <paramref name="RemainingPath"/> to <paramref name="Parent"/> recursively.
        /// </summary>
        private void PopulateAvailable(AvailableCharacter Parent, string RemainingPath, string PathInfo)
        {
            if (AvailRstrs.IsChecked ?? false)
            {
                Parent.Children.Add(new AvailableCharacter(Parent.Children, PathInfo));
                return;
            }
            string[] Nodes = RemainingPath.Split(Path.DirectorySeparatorChar, 2);
            bool IsDir = Nodes.Length > 1;
            AvailableCharacter? child = Parent.Children.SingleOrDefault(x => ((Character)x.Content).Name == Nodes[0]);
            if (child is null)
            {
                child = new AvailableCharacter(Parent.Children, new Character()
                {
                    Name = Nodes[0],
                    Path = IsDir ? PathInfo[..^Nodes[1].Length] : PathInfo
                });
                if (IsDir)
                {
                    int i = 0; for (; i < Parent.Children.Count; i++) { if (!Parent.Children[i].HasChildren) { break; } }
                    Parent.Children.Insert(i, child);
                }
                else
                {
                    Parent.Children.Add(child);
                }
            }
            if (IsDir) { PopulateAvailable(child, Nodes[1], PathInfo); }
        }
        /// <summary>
        /// Switch between available characters and rosters. <see langword="True"/> enables characters, <see langword="false"/> rosters.
        /// </summary>
        private void AvailChange(bool IsChar)
        {
            // WIP: Improve logic, possibly with binding
            IsChar = !ShowRosters || IsChar;
            FilterVoice = IsXvoiceSel && !FilterVoice;
            AvailChars.IsChecked = !(bool)(CharsNeedVoice.IsChecked = !(bool)(AvailRstrs.IsChecked = !IsChar) && FilterVoice) && IsChar;
            BrowseButton.Visibility = IsChar ? Visibility.Visible : Visibility.Collapsed;
            if (ShowRosters) { Cfg.GUI.AvailChars = IsChar; }
            PopulateAvailable(!ShowRosters);
        }
        /// <summary>
        /// Search while typing a string
        /// </summary>
        private void TVsearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            PopulateAvailable(Cfg.Roster.Available, sender.Text, sender.Text.Length == 0);
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
        /// Right click to select
        /// </summary>
        private void TreeViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (GUIHelpers.FindFirstParent<TreeViewItem>((DependencyObject)e.OriginalSource) is TreeViewItem RTTVN)
            {
                Cfg.Var.FloatingCharacter = RTTVN.DataContext is AvailableCharacter Selected
                    && !Selected.HasChildren
                    && RTTVN.Content is Character SC ? SC.Path : null;
                RTTVN.IsSelected = true;
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
                try
                {
                    foreach (string Source in OHSpath.GetModSource(ExtModPath))
                    {
                        if (OHSpath.GetFiles(Source).FirstOrDefault() is string Hs
                            && Herostat.Add(Hs, Path.GetExtension(Hs.AsSpan()).ToString(), new Stats(Hs).CharacterName))
                        {
                            i++;
                        }
                        if (OHSpath.GetModTarget(Source, Path.GetFileName(Source), Mod.Path) is string Target
                            && OHSpath.CopyFilesRecursively(Source, Target))
                        {
                            c++;
                        }
                    }
                }
                catch { }
                CopyInfo.IsOpen = c == 0;
                HSinfo.IsOpen = !(HSsuccess.IsOpen = i > 0);
            }
            else
            {
                try { HSsuccess.IsOpen = Herostat.Add(Mod.Path, Mod.FileType, new Stats(new FileInfo(Mod.Path)).CharacterName); }
                catch { HSsuccess.IsOpen = false; }
            }
        }
        /// <summary>
        /// Define the allowed drag items 
        /// </summary>
        private void DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
        {
            if (args.Items[0] is AvailableCharacter Selected && Selected.Content is Character SC)
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
            if (args.InvokedItem is AvailableCharacter Selected && Selected.Content is Character SC && SC.Path is not null)
            {
                if (Selected.HasChildren)
                {
                    //Selected.IsExpanded = !Selected.IsExpanded;
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
            if (ShowRosters)
            {
                if (Cfg.Var.FloatingCharacter is not null)
                {
                    AddToSelected(Cfg.Var.FloatingCharacter);
                    UpdateClashes();
                }
                else if (trvAvailableChars.SelectedItem is AvailableCharacter Node)
                {
                    if (!Cfg.GUI.AvailChars && Node.Content is string Roster)
                    {
                        Cfg.MUA.MenulocationsValue = Cfg.OHS.RosterValue = CfgCmd.FilterDefaultRV(Roster);
                        LoadRosterVal(Roster, Roster);
                    }
                    else
                    {
                        for (int i = 0; i < Node.Children.Count; i++)
                        {
                            if (!Node.Children[i].HasChildren && Node.Children[i].Content is Character SC)
                                AddToSelected(SC.Path);
                        }
                        UpdateClashes();
                    }
                }
            }
        }
        /// <summary>
        /// Delete the selected nodes recursively
        /// </summary>
        private void TreeViewItems_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            RemoveCharacters();
            args.Handled = true;
        }
        /// <summary>
        /// Remove the selected tree view node and all its child nodes, including the herostat files.
        /// </summary>
        private void RemoveCharacters()
        {
            // Currently, single selection mode is defined
            //foreach (AvailableCharacter Node in trvAvailableChars.SelectedItems.OfType<AvailableCharacter>())
            if (trvAvailableChars.SelectedItem is AvailableCharacter Node)
            {
                RemoveCharacters(Node);
                Node.ParentCollection.Remove(Node);
            }
            Cfg.Var.FloatingCharacter = null;
        }
        /// <summary>
        /// Remove the <paramref name="Node"/> and all its child nodes, including the corresponding herostat files according to the respective <see cref="Character.Path"/> property.
        /// </summary>
        private void RemoveCharacters(AvailableCharacter Node)
        {
            if (Node.HasChildren)
            {
                for (int i = Node.Children.Count - 1; i >= 0; i--)
                {
                    RemoveCharacters(Node.Children[i]);
                    Node.Children.RemoveAt(i);
                }
            }
            else
            {
                try
                {
                    if (Node.Content is Character Chr
                        && !string.IsNullOrEmpty(Chr.Path))
                    {
                        OHSpath.GetHsFile(Chr.Path).Delete();
                    }
                    else if (Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", $"{Node.Content}.cfg") is string r
                        && File.Exists(r))
                    {
                        File.Delete(r);
                    }
                }
                catch { DeleteFailed.IsOpen = true; }
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

        private void TreeViewItem_CopyClick(object sender, RoutedEventArgs e)
        {
            if (trvAvailableChars.SelectedItem is AvailableCharacter Node)
            {
                int i = Node.ParentCollection.IndexOf(Node);
                string file;
                if (Cfg.Var.FloatingCharacter is not null)
                {
                    try { file = OHSpath.GetHsFile(Cfg.Var.FloatingCharacter).FullName; } catch { return; }
                }
                else if (!Cfg.GUI.AvailChars && Node.Content is string Roster)
                {
                    file = Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", $"{Roster}.cfg");
                }
                else { return; }
                string Ext = Path.GetExtension(file);
                string newfile = OHSpath.GetVacant(file[..^Ext.Length], Ext, 1);
                try
                {
                    File.Copy(file, newfile);
                    string NewName = Path.GetFileName(newfile)[..^Ext.Length];
                    Node.ParentCollection.Insert(i + 1, new AvailableCharacter(
                        Node.ParentCollection,
                        OHSpath.GetParent(Cfg.Var.FloatingCharacter) is string Base
                            ? new Character { Name = NewName, Path = Path.Combine(Base, NewName) }
                            : NewName
                    ));
                }
                catch { } // no duplicate added, should be obvious to the user that it failed
            }
        }

        private void TreeViewItem_DeleteClick(object sender, RoutedEventArgs e) => RemoveCharacters();

        private async void TreeViewItem_RenameClick(object sender, RoutedEventArgs e)
        {
            if (trvAvailableChars.SelectedItem is AvailableCharacter Node)
            {
                bool Accepted = await EnterNewName.ShowAsync() == ContentDialogResult.Secondary || RenamedWithEnter;
                RenamedWithEnter = false;
                if (!Accepted || NewName.Text == "") { return; }
                int i = Node.ParentCollection.IndexOf(Node);
                AvailableCharacter NewNode = new(Node.ParentCollection, "");
                try
                {
                    string NewPath;
                    if (Node.Content is Character Chr)
                    {
                        if (Node.HasChildren) // dir
                        {
                            string SubDirs = Chr.Path![..^(Chr.Name!.Length + 1)];
                            NewPath = OHSpath.GetVacant(Path.Combine(OHSpath.HsFolder, SubDirs, NewName.Text));
                            Directory.Move(Path.Combine(OHSpath.HsFolder, Chr.Path!), NewPath);
                            Chr.Path = NewPath[OHSpath.HsFolder.Length..];
                            NewNode.Children = Node.Children;
                        }
                        else // leaf, file
                        {
                            FileInfo HS = OHSpath.GetHsFile(Chr.Path!);
                            string Ext = HS.Extension;
                            NewPath = OHSpath.GetVacant(Path.Combine(HS.DirectoryName!, NewName.Text), Ext);
                            Chr.Path = NewPath[(HS.FullName.Length - Chr.Path!.Length - Ext.Length)..^Ext.Length];
                            HS.MoveTo(NewPath); // Modifies HS.FullName
                        }
                        Chr.Name = Path.GetFileNameWithoutExtension(NewPath);
                        NewNode.Content = new Character { Name = Path.GetFileNameWithoutExtension(NewPath), Path = Chr.Path };
                    }
                    else if (Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", $"{Node.Content}.cfg") is string r
                        && File.Exists(r))
                    {
                        NewPath = OHSpath.GetVacant(Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", NewName.Text), ".cfg");
                        File.Move(r, NewPath);
                        NewNode.Content = Path.GetFileNameWithoutExtension(NewPath);
                    }
                    Node.ParentCollection.Insert(i + 1, NewNode);
                    Node.ParentCollection.RemoveAt(i);
                }
                catch { RenameFailed.IsOpen = true; }
            }
        }

        private async void TreeViewItem_RenameCharClick(object sender, RoutedEventArgs e)
        {
            if (trvAvailableChars.SelectedItem is AvailableCharacter Node && !Node.HasChildren && Node.Content is Character Chr && Chr.Path is not null)
            {
                try
                {
                    Stats S = new(Chr.Path);
                    string OldName = NewName.Text = S.CharacterName;
                    bool Accepted = await EnterNewName.ShowAsync() == ContentDialogResult.Secondary || RenamedWithEnter;
                    RenamedWithEnter = false;
                    if (!Accepted || NewName.Text == "" || NewName.Text.Equals(OldName, StringComparison.OrdinalIgnoreCase)) { return; }
                    S.ChangeAttributeValue("charactername", NewName.Text);
                }
                catch { RenameFailed.IsOpen = true; }
            }
        }
        /*
        private async void TreeViewItem_RenameInternClick(object sender, RoutedEventArgs e)
        {
            if (trvAvailableChars.SelectedItem is AvailableCharacter Node && !Node.HasChildren && Node.Content is Character Chr && Chr.Path is not null)
            {
                try
                {
                    Stats S = new(Chr.Path);
                    string OldName = NewName.Text = S.InternalName;
                    bool Accepted = await EnterNewName.ShowAsync() == ContentDialogResult.Secondary || RenamedWithEnter;
                    RenamedWithEnter = false;
                    if (!Accepted || NewName.Text == "" || NewName.Text.Equals(OldName, StringComparison.OrdinalIgnoreCase)) { return; }
                    for (int i = 0; i < NewName.Text.Length; i++) { if (!char.IsAsciiLetter(NewName.Text[i])) { RenIntFailed.IsOpen = true; return; } }
                    string Ext = Path.GetExtension(CfgSt.OHS.HerostatName);
                    string? OldTalents = await CfgCmd.LoadDialogue(Ext);
                    if (OldTalents is null) { return; }
                    if (Path.GetFileNameWithoutExtension(OldTalents).Equals(OldName, StringComparison.OrdinalIgnoreCase))
                    {
                        string TalentsDir = Path.GetDirectoryName(OldTalents)!;
                        string PkgsDir = OHSpath.Packages(TalentsDir[..^13]);
                        string NewInNm = NewName.Text.ToLower();
                        string CharNum = S.RootAttribute("skin")[..^2];
                        bool All = false;
                        foreach (string OldPkg in Directory.EnumerateFiles(PkgsDir, $"{OldName}*.pkgb"))
                        {
                            string SkinNum = OldPkg[^7..^5];
                            if (SkinNum is "nc" or "_c") { continue; } // nc are cloned alongside main, could theoretically be uppercase...
                            bool IsFightstyle = OldPkg[^16..^5].Equals("fightstyles", StringComparison.OrdinalIgnoreCase);
                            All = MarvelModsXML.ClonePackage(OldPkg,
                                Path.Combine(PkgsDir, IsFightstyle ? $"{NewInNm}_fightstyles" : $"{NewInNm}_{CharNum}{SkinNum}"),
                                CharNum, CharNum, IsFightstyle ? "01" : SkinNum, true) && All;
                        }
                        // Bare minimum, but won't work with char with OldName
                        File.Copy(OldTalents, Path.Combine(TalentsDir, $"{NewInNm}{Ext}"));
                        // Needs PS and renamed power references to be able to play alongside
                        if (MarvelModsXML.DecompileToTemp(OldTalents) is string DT
                            && MarvelModsXML.DecompileToTemp(Path.Combine(TalentsDir[..^8], "powerstyles", $"{S.RootAttribute("powerstyle")}{Ext}")) is string DPS)
                        {
                            System.Xml.XmlDocument XT = new(), XPS = new();
                            XT.Load(DT); XPS.Load(DPS);
                            if (XT.DocumentElement is System.Xml.XmlElement talents)
                            {
                                string OPNm = "_";
                                foreach (System.Xml.XmlElement t in talents.ChildNodes)
                                {
                                    OPNm = t.GetAttribute("name");
                                    if (OPNm.Length > 0 && OPNm[^8..^2] != "outfit" && OPNm.Contains('_')) { break; }
                                }
                                int ULI = OPNm.IndexOf('_');
                                OPNm = OPNm[..ULI];
                                string NPNm = NewInNm[..ULI];
                                if (OPNm == NPNm) { NPNm = $"{NewInNm[..^1]}{(Math.Clamp((byte)NPNm[^1], (byte)48, (byte)57) - 47) % 10}"; }
                                foreach (System.Xml.XmlAttribute a in XT.SelectNodes($"//attribute::*[starts-with(., '{OPNm}')]")!) { a.Value = $"{NPNm}{a.Value[OPNm.Length..]}"; }
                                foreach (System.Xml.XmlAttribute a in XPS.SelectNodes($"//attribute::*[starts-with(., '{OPNm}')]")!) { a.Value = $"{NPNm}{a.Value[OPNm.Length..]}"; }
                                _ = MarvelModsXML.CompileToTarget(XT, $"{NewInNm}{Ext}")
                                    && MarvelModsXML.CompileToTarget(XPS, Path.Combine(TalentsDir[..^8], "powerstyles", $"ps_{NewInNm}{Ext}"));
                                S.ChangeAttributeValue("powerstyle", $"ps_{NewInNm}");
                            }
                        }
                        RenPkgFailed.IsOpen = !All;
                    }
                    else { RenIntFailed.IsOpen = false; }
                    S.ChangeAttributeValue("name", NewName.Text);
                }
                catch { RenameFailed.IsOpen = true; }
            }
        }
        */
        private void NewName_Entered(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            RenamedWithEnter = true;
            EnterNewName.Hide();
        }
    }
}
