using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using OpenHeroSelectGUI.Settings;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using static OpenHeroSelectGUI.Settings.CharacterListCommands;

namespace OpenHeroSelectGUI.Controls
{
    public sealed partial class AvailableCharacters : UserControl
    {
        internal Cfg Cfg { get; set; } = new();

        private readonly bool DisableDoubleTap;

        public AvailableCharacters(bool IsXvoiceTab, bool Needy = false)
        {
            DisableDoubleTap = IsXvoiceTab;
            Cfg.Roster.NeedyCharsOnly = Needy;
            InitializeComponent();
        }
        /// <summary>
        /// Install a <paramref name="Mod"/>, including herostat. <paramref name="Mod"/> can be a heroatat, which then is installed instead.
        /// </summary>
        /// <remarks>Shows success and error messages in the control's main grid.</remarks>
        public void Install(StorageFile Mod)
        {
            if (Util.Run7z(Mod.Path, Mod.DisplayName) is string ExtModPath)
            {
                CopyInfo.IsOpen = HSinfo.IsOpen = false;
                int i = 0, c = 0;
                try
                {
                    foreach (string Source in OHSpath.GetModSource(ExtModPath))
                    {
                        if (OHSpath.GetHsFiles(Source).FirstOrDefault() is string Hs
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
                try { HSsuccess.IsOpen = Herostat.Add(Mod.Path, Mod.FileType, new Stats(Mod.Path).CharacterName); }
                catch { HSsuccess.IsOpen = false; }
            }
        }
        /// <summary>
        /// Remove the selected tree view node and all its child nodes recursively, including the herostat files.
        /// </summary>
        private void RemoveCharacters()
        {
            DeleteFailed.IsOpen = false;
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
        /// Remove the <paramref name="Node"/> and all its child nodes recursively, including the corresponding herostat files according to the respective <see cref="Character.Path"/> property.
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
                    if (!string.IsNullOrEmpty(Node.Content.Path)) { File.Delete(OHSpath.GetHsFile(Node.Content.Path)); }
                }
                catch { DeleteFailed.IsOpen = true; }
            }
        }
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
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i] is StorageFile Mod) { Install(Mod); }
                }
            }
            AvailableCharactersDropArea.Visibility = Visibility.Collapsed;
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
            else { args.Cancel = true; }
        }
        /// <summary>
        /// Add the selected character to the selected list on double-tap, or, if a parent is double-tapped, add all direct leaf children
        /// </summary>
        private void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (DisableDoubleTap) { return; }
            if (Cfg.Var.FloatingCharacter is string FC) { AddToSelected(FC); }
            else if (trvAvailableChars.SelectedItem is AvailableCharacter Node)
            {
                int[] AvailLocs = [.. CfgSt.CSS.Locs.Where(static l => !CfgSt.Roster.SelectedLocs.Get(l)).Take(Node.Children.Count)];
                for (int i = 0, a = 0; i < Node.Children.Count && a < AvailLocs.Length; i++)
                {
                    if (!Node.Children[i].HasChildren && Node.Children[i].Content is Character C && C.Path is not null)
                    {
                        AddToSelectedBulk(AvailLocs[a], C.Path);
                    }
                }
                if (CfgSt.GUI.IsMua) { CfgSt.CSS.UpdateLocBoxes(); }
                UpdateClashes();
            }
        }
        /// <summary>
        /// Define the leaf nodes as characters, expand/collapse parent nodes.
        /// </summary>
        private void OnSelectionChanged(TreeView AC, TreeViewItemInvokedEventArgs args)
        {
            Cfg.Var.FloatingCharacter = null;
            if (args.InvokedItem is AvailableCharacter Selected && Selected.Content.Path is not null)
            {
                if (!Selected.HasChildren) { Cfg.Var.FloatingCharacter = Selected.Content.Path; }
                // else { Selected.IsExpanded = !Selected.IsExpanded; }
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
                    && !Selected.HasChildren ? Selected.Content.Path : null;
                RTTVN.IsSelected = true;
            }
        }
        // Context menu commands:
        private void TreeViewItem_CopyClick(object sender, RoutedEventArgs e)
        {
            if (trvAvailableChars.SelectedItem is AvailableCharacter Node)
            {
                int i = Node.ParentCollection.IndexOf(Node);
                if (string.IsNullOrEmpty(Cfg.Var.FloatingCharacter)) { return; }
                try
                {
                    string HS = OHSpath.GetHsFile(Cfg.Var.FloatingCharacter);
                    string Ext = Path.GetExtension(HS);
                    string NewFile = OHSpath.GetVacant(HS[..^Ext.Length], Ext, 1);
                    File.Copy(HS, NewFile);
                    string NewName = NewFile[OHSpath.IndexOfFileName(NewFile)..^Ext.Length];
                    Node.ParentCollection.Insert(i + 1, new AvailableCharacter(
                        Node.ParentCollection,
                        new Character { Name = NewName, Path = Path.Combine(OHSpath.GetParent(Cfg.Var.FloatingCharacter)!, NewName) }
                    ));
                }
                catch { } // no duplicate added, should be obvious to the user that it failed
            }
        }

        private void TreeViewItem_DeleteClick(object sender, RoutedEventArgs e) => RemoveCharacters();

        private async void TreeViewItem_RenameClick(object sender, RoutedEventArgs e)
        {
            if (trvAvailableChars.SelectedItem is AvailableCharacter Node && Node.Content is Character Chr)
            {
                string OldName = Chr.Name ?? "";
                ContentDialogResult result = await EnterNewName.ShowAsync(OldName);
                string NewName = EnterNewName.InputText;
                if (result != ContentDialogResult.Secondary || NewName == "" || NewName.Equals(OldName)) { return; }
                int i = Node.ParentCollection.IndexOf(Node);
                try
                {
                    string NewPath;
                    if (Node.HasChildren) // dir
                    {
                        string SubDirs = Chr.Path![..^(Chr.Name!.Length + 1)];
                        NewPath = OHSpath.GetVacant(Path.Combine(OHSpath.HsFolder, SubDirs, NewName));
                        Directory.Move(Path.Combine(OHSpath.HsFolder, Chr.Path!), NewPath);
                        Chr.Path = NewPath[OHSpath.HsFolder.Length..];
                    }
                    else // leaf, file
                    {
                        string HS = OHSpath.GetHsFile(Chr.Path!), Ext = Path.GetExtension(HS);
                        NewPath = OHSpath.GetVacant(Path.Combine(Path.GetDirectoryName(HS)!, NewName), Ext);
                        File.Move(HS, NewPath);
                        Chr.Path = NewPath[(HS.Length - Chr.Path!.Length - Ext.Length)..^Ext.Length];
                    }
                    Chr.Name = Path.GetFileNameWithoutExtension(NewPath);
                    Node.ParentCollection[i] = Node;
                }
                catch { RenameFailed.IsOpen = true; }
            }
        }

        private async void TreeViewItem_RenameCharClick(object sender, RoutedEventArgs e)
        {
            if (trvAvailableChars.SelectedItem is AvailableCharacter Node
                && !Node.HasChildren && Node.Content.Path is not null)
            {
                try
                {
                    Stats S = Herostat.GetStats(Node.Content.Path);
                    string OldName = S.CharacterName;
                    ContentDialogResult result = await EnterNewName.ShowAsync(OldName);
                    string NewName = EnterNewName.InputText;
                    if (result != ContentDialogResult.Secondary || NewName == "" || NewName.Equals(OldName)) { return; }
                    S.ChangeAttributeValue("charactername", NewName);
                    RenameFailed.IsOpen = false;
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
        /// <summary>
        /// Delete the selected nodes recursively
        /// </summary>
        private void TreeViewItems_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            RemoveCharacters();
            args.Handled = true;
        }
    }
}
