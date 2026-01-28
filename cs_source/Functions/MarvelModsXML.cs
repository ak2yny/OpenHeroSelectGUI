using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace OpenHeroSelectGUI.Functions
{
    /// <summary>
    /// Handling MarvelMods specific XML actions
    /// </summary>
    internal static class MarvelModsXML
    {
        private static readonly string Riser = "<item effect=\"menu/riser\" enabled=\"false\" name=\"new_stage_fx\" neverfocus=\"true\" type=\"MENU_ITEM_EFFECT\" />";

        /// <summary>
        /// Decompile a file to the temp folder by generating a filename for the decompiled file using the <paramref name="CompiledName"/>.
        /// </summary>
        /// <returns>The filename for the decompiled file (.xml), if the file could be decompiled; otherwise <see langword="null" />.</returns>
        private static string? DecompileToTemp(string CompiledName)
        {
            string DecompiledName = Path.Combine(OHSpath.Temp, $"{Path.GetFileNameWithoutExtension(CompiledName)}.xml");
            return Util.RunExeInCmd("json2xmlb", $"-d \"{CompiledName}\" \"{DecompiledName}\"") ? DecompiledName : null;
        }
        /// <summary>
        /// Compile <paramref name="XmlData"/> to <paramref name="CompiledName"/> file (full path) by writing to a temporary file first.
        /// </summary>
        /// <returns><see langword="True" />, if the file could be compiled; otherwise <see langword="false" />.</returns>
        private static bool CompileToTarget(XmlDocument XmlData, string CompiledName)
        {
            string DecompiledName = Path.Combine(OHSpath.Temp, $"{Path.GetFileNameWithoutExtension(CompiledName)}.xml");
            try { XmlData.Save(DecompiledName); } catch { return false; }
            return Directory.Exists(Path.GetDirectoryName(CompiledName)) && Util.RunExeInCmd("json2xmlb", $"\"{DecompiledName}\" \"{CompiledName}\"");
        }
        /// <summary>
        /// Add data from settings to <paramref name="LayoutDataFile"/> (team_back.xmlb): Riser (if <paramref name="HasRiser"/>), Effects
        /// </summary>
        /// <returns>The updated team_back.xmlb file name <see cref="string"/>, or the input <paramref name="LayoutDataFile"/> name if failed or no changes necessary</returns>
        public static string UpdateLayout(string LayoutDataFile, bool HasRiser)
        {
            SelectedCharacter[] EL = [.. CfgSt.Roster.Selected.Where(static c => c.Effect is not null)]; // No results for XML2
            if ((HasRiser || EL.Length > 0) && DecompileToTemp(LayoutDataFile) is string DLDF)
            {
                XmlDocument LayoutData = new();
                try { LayoutData.Load(DLDF); } catch { return LayoutDataFile; }
                if (LayoutData.DocumentElement is XmlElement menu_items)
                {
                    _ = HasRiser && GUIXML.ImportXmlString(LayoutData, Riser);
                    for (int i = 0, c = 0; i < EL.Length && c < 2; i++)
                    {
                        if (EL[i].LocNum == 24) { (EL[i], EL[0]) = (EL[0], EL[i]); c++; }
                        else if (EL[i].LocNum == 3) { (EL[i], EL[1]) = (EL[1], EL[i]); c++; }
                    }
                    if (CfgSt.GUI.HidableEffectsOnly && Util.GameExe(CfgSt.GUI.ActualGameExe != "" ? CfgSt.GUI.ActualGameExe : OHSpath.GetStartExe) is FileStream fs)
                    {   // Hiding only works with hex-editing (effect only disappears on selection if fx matches menuloaction)
                        Util.HexEdit(0x3cc67b, EL[0].Loc, fs);
                        if (EL.Length > 1) { Util.HexEdit(0x3cc687, EL[1].Loc, fs); }
                        fs.Close();
                    }
                    int total = CfgSt.GUI.HidableEffectsOnly ? Math.Min(EL.Length, 2) : EL.Length;
                    for (int i = 0; i < total; i++)
                    {
                        _ = AddEffect(EL[i].LocNum, EL[i].Effect!, EL[0].Loc) is string EffectLine
                            && GUIXML.ImportXmlString(LayoutData, EffectLine);
                    }
                    if (CompileToTarget(LayoutData, $"{DLDF}b")) { return $"{DLDF}b"; }
                }
            }
            return LayoutDataFile;
        }
        /// <summary>
        /// Modify the referenced <paramref name="effect"/> file with the <paramref name="Loc"/> coordinates and copy it to the game files with the _<paramref name="FXslot"/> suffix.
        /// </summary>
        /// <returns>An XML <see cref="string"/> with the same values for team_back.xmlb if succeeded; otherwise <see langword="null"/>.</returns>
        private static string? AddEffect(int Loc, CSSEffect effect, string FXslot)
        {
            if (effect.File is string EFN
                && Path.Combine(OHSpath.StagesDir, ".effects", $"{EFN}.xml") is string EFP
                && File.Exists(EFP))
            {
                try
                {
                    XmlDocument EffectFile = new();
                    EffectFile.Load(EFP);
                    if (EffectFile.DocumentElement is XmlElement Effect
                        && CfgSt.CSS.Locs.Length > 0 && CfgSt.CSS.Locs.IndexOf(Loc) is int i && i != -1)
                    {
                        Location Coordinates = CfgSt.CSS.LocationSetup.Locations[i];
                        int X = Coordinates.X + effect.Offset.X;
                        int Y = Coordinates.Y + effect.Offset.Y;
                        int Z = Coordinates.Z + effect.Offset.Z;
                        string Value = $"{X} {Y} {Z} {X} {Y} {Z}";
                        foreach (XmlElement EffectType in Effect.ChildNodes)
                        {
                            XmlAttribute O1 = EffectFile.CreateAttribute("origin");
                            XmlAttribute O2 = EffectFile.CreateAttribute("origin2");
                            O1.Value = O2.Value = Value;
                            if (EffectType.HasAttribute("origin"))
                                _ = EffectType.SetAttributeNode(O1);
                            if (EffectType.HasAttribute("origin2"))
                                _ = EffectType.SetAttributeNode(O2);
                        }
                        if (CompileToTarget(EffectFile, Path.Combine(Directory.CreateDirectory($"{CfgSt.OHS.GameInstallPath}/effects/menu").FullName, $"{EFN}_{Loc}.xmlb")))
                        {
                            // WIP: This seems to work, but is still in post-beta phase
                            return $"<item effect=\"menu/{EFN}_{Loc}\" enabled=\"false\" name=\"pad{FXslot}_fx\" neverfocus=\"true\" type=\"MENU_ITEM_EFFECT\" />";
                        }
                    }
                }
                catch { return null; }
            }
            return null;
        }
        /// <summary>
        /// Look for package in <paramref name="PkgSourceFolders" /> and, if none found that match <paramref name="Skin" /> and <paramref name="IntName" />, try to clone another package to <paramref name="TargetPath"/> if one exists.
        /// </summary>
        /// <param name="PkgSourceFolders">Enumerable of directories, each with a package folder to look in</param>
        /// <param name="Skin">SkinDetail</param>
        /// <param name="IntName">Internal name</param>
        /// <param name="TargetPath">Target folder to clone package to</param>
        /// <returns><see langword="True" />, if both packages were cloned successfully or the package exists; otherwise <see langword="false" />.</returns>
        public static bool ClonePackage(IEnumerable<string> PkgSourceFolders, SkinDetails Skin, string? IntName, string TargetPath)
        {
            foreach (string PkgSrcF in PkgSourceFolders)
            {
                if (File.Exists($"{PkgSrcF}/{IntName}_{Skin.CharNum}{Skin.Number}.pkgb")) { return true; }
            }
            try
            {
                foreach (string PkgSrcF in PkgSourceFolders)
                {
                    if (Directory.EnumerateFiles(PkgSrcF, $"{IntName}_{Skin.CharNum}??.pkgb")
                               .FirstOrDefault() is string PkgSrc)
                    {
                        return ClonePackage(PkgSrc,
                            OHSpath.Packages(TargetPath, $"{IntName}_{Skin.CharNum}{Skin.Number}"),
                            Skin.CharNum, Skin.CharNum, Skin.Number);
                    }
                }
            }
            catch { }
            return false;
        }
        /// <summary>
        /// Clone <paramref name="SourcePkg" /> to new <paramref name="PkgBasePath" />.pkgb (plus _nc if available),
        /// replacing <paramref name="CharNum"/> (plus num from source pkg) references with <paramref name="NewCharNum"/><paramref name="TargetNum"/>,
        /// as well as the talent file names, taken from the internal name parts from the package parameters, if <paramref name="InclTalents"/>.
        /// <paramref name="SourcePkg" /> must be the path to an existing package (only .pkgb).
        /// </summary>
        /// <returns><see langword="True"/> if both packages were cloned successfully; otherwise <see langword="false"/>.</returns>
        public static bool ClonePackage(string SourcePkg, string PkgBasePath, string CharNum, string NewCharNum, string TargetNum) // , bool InclTalents = false
        {
            if (DecompileToTemp(SourcePkg) is string DP)
            {
                XmlDocument Pkg = new(), NcPkg = new();
                try { Pkg.Load(DP); } catch { return false; }
                if (Pkg.DocumentElement is XmlElement packagedef)
                {
                    string SourceName = Path.GetFileNameWithoutExtension(SourcePkg);
                    string SourceNum = SourceName[^2..];
                    string SourceNCpkg = $"{SourcePkg[..^5]}_nc.pkgb";
                    bool IsFightstyle = PkgBasePath[^11..] == "fightstyles";

                    if (SourceNum is "nc" or "_c")
                    {
                        PkgReplInAttr(packagedef, CharNum, SourceNum == "nc" ? SourceName[^5..^3] : SourceName[^4..^2], NewCharNum, TargetNum);
                        return CompileToTarget(Pkg, $"{PkgBasePath}.pkgb");
                    }
                    else if (File.Exists(SourceNCpkg) && DecompileToTemp(SourceNCpkg) is string DN)
                    {
                        try { NcPkg.Load(DN); } catch { return false; }
                    }
                    else if (!IsFightstyle)
                    {
                        XmlElement PD = NcPkg.CreateElement("packagedef");
                        for (int i = 0; i < packagedef.ChildNodes.Count; i++)
                        {
                            if (packagedef.ChildNodes[i] is XmlElement PkgElmt)
                            {
                                string filename = PkgElmt.GetAttribute("filename");
                                PkgElmt.SetAttribute("filename", PkgReplace(filename, CharNum, SourceNum, NewCharNum, TargetNum));
                                _ = PD.AppendChild(PkgElmt);
                                if (filename == $"hud_head_{CharNum}{SourceNum}") { break; }
                            }
                        }
                        _ = NcPkg.AppendChild(PD);
                    }
                    //if (InclTalents)
                    //{
                    //    int SourceULPos = SourceName.LastIndexOf('_');
                    //    string OldIntName = SourceName[..SourceULPos];
                    //    string NewIntName = Path.GetFileName(PkgBasePath)![..^(SourceName.Length - SourceULPos)];
                    //    //if (OldIntName != NewIntName)
                    //    PkgReplInAttr(packagedef, $"data/talents/{OldIntName}", $"data/talents/{NewIntName}");
                    //    if (!IsFightstyle)
                    //        PkgReplInAttr(NcPkg.DocumentElement!, $"data/talents/{OldIntName}", $"data/talents/{NewIntName}");
                    //}
                    PkgReplInAttr(packagedef, CharNum, SourceNum, NewCharNum, TargetNum);
                    if (!IsFightstyle) { PkgReplInAttr(NcPkg.DocumentElement!, CharNum, SourceNum, NewCharNum, TargetNum); }
                    return CompileToTarget(Pkg, $"{PkgBasePath}.pkgb") && (IsFightstyle || CompileToTarget(NcPkg, $"{PkgBasePath}_nc.pkgb"));
                }
            }
            return false;
        }
        /// <summary>
        /// Extended replace method for packages: Replace all values of attributes that match <paramref name="OldValue"/> in <paramref name="XE"/> with <paramref name="NewValue"/>.
        /// </summary>
        private static void PkgReplInAttr(XmlElement XE, string OldValue, string NewValue)
        {
            if (XE.SelectNodes($"//attribute::*[contains(., '{OldValue}')]/..") is XmlNodeList XNL)
            {
                // All nodes have a filename attribute (some also plat) and filename is the only important one
                foreach (XmlElement F in XNL) { F.GetAttributeNode("filename")!.Value = NewValue; }
            }
        }
        /// <summary>
        /// Extended replace method for packages: Call <see cref="PkgReplace"/> on all attributes of <paramref name="XE"/>
        /// </summary>
        private static void PkgReplInAttr(XmlElement XE, string CharNum, string SourceNum, string NewCharNum, string TargetNum)
        {
            XmlNodeList XNL = XE.SelectNodes($"//attribute::*[contains(., '{CharNum}')]/..")!;
            for (int i = 0; i < XNL.Count; i++)
            {
                XmlAttributeCollection XAC = XNL[i]!.Attributes!;
                for (int a = 0; a < XAC.Count; a++)
                {
                    XAC[a].Value = PkgReplace(XAC[a].Value, CharNum, SourceNum, NewCharNum, TargetNum);
                }
            }
        }
        /// <summary>
        /// In <paramref name="filename"/> replace '<paramref name="CharNum"/><paramref name="SourceNum"/>'/'<paramref name="CharNum"/>_' with '<paramref name="NewCharNum"/><paramref name="TargetNum"/>'/'<paramref name="NewCharNum"/>_'.
        /// </summary>
        /// <returns>The replaced string or <paramref name="filename"/> if nothing to replace.</returns>
        private static string PkgReplace(string filename, string CharNum, string SourceNum, string NewCharNum, string TargetNum)
        {
            return filename.Length > CharNum.Length && filename[..(CharNum.Length + 1)] == $"{CharNum}_"
                ? $"{NewCharNum}{filename[CharNum.Length..]}"
                : filename.Replace($"{CharNum}{SourceNum}", $"{NewCharNum}{TargetNum}");
        }
        /// <summary>
        /// Replace all skin_filter references that match any <paramref name="OldSkinNums"/> with <paramref name="NewCharNum"/> (plus [^2..] of old number) in <paramref name="SourceFile"/>. <paramref name="SourceFile"/> must exist and will be replaced. Currently calls itself also on all ents_ (entities) references.
        /// </summary>
        /// <returns><see langword="True"/> if <paramref name="SourceFile"/> is an XML file with a root and could be accessed successfully, otherwise <see langword="false"/>.</returns>
        public static bool ReplaceRef(string SourceFile, string[] OldSkinNums, string NewCharNum)
        {
            if (DecompileToTemp(SourceFile) is string DP)
            {
                XmlDocument XML = new();
                try { XML.Load(DP); } catch { return false; }
                if (XML.DocumentElement is XmlElement RootE)
                {
                    int c = 0;
                    // Warning: attributes case sensitive! I don't know if XMLB attributes are case sensitive.
                    for (int s = 0; s < OldSkinNums.Length; s++)
                    {
                        if (OldSkinNums[s] is string ON
                            && RootE.SelectNodes($"//*[@skin_filter=\"{ON}\"]") is XmlNodeList XNL)
                        {
                            for (int i = 0; i < XNL.Count; i++)
                            {
                                XNL[i]!.Attributes!["skin_filter"]!.Value = NewCharNum + ON[^2..];
                                c++;
                            }
                        }
                    }
                    if (!Path.GetFileName(SourceFile).StartsWith("ents_")
                        && RootE.SelectNodes($"//*[contains(@filename, 'ents_')]") is XmlNodeList Ents)
                    {
                        string SD = Path.GetDirectoryName(OHSpath.GetParent(SourceFile))!;
                        for (int i = 0; i < Ents.Count; i++)
                        {
                            _ = ReplaceRef(Path.Combine(SD, "entities", $"{Ents[i]!.Attributes!["filename"]!.Value}.xmlb"), OldSkinNums, NewCharNum);
                        }
                    }
                    return c > 0 && CompileToTarget(XML, SourceFile);
                }
            }
            return false;
        }
        /// <summary>
        /// Serializes the Team <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/> to XML (if it has any team bonuses) and saves it to the compiled team_bonus file in the game folder.
        /// </summary>
        /// <returns><see langword="True" />, if a new team bonus was created and json2xmlb could convert it successfully; otherwise <see langword="false" />.</returns>
        public static bool TeamBonusCopy()
        {
            if (BonusSerializer.Serialize(OHSpath.Team_bonus))
            {
                string team_bonus = CfgSt.GUI.ModPack ? CfgSt.GUI.TeamBonusName : "team_bonus";
                string CompiledName = Path.Combine(CfgSt.OHS.GameInstallPath, "data", $"{team_bonus}{Path.GetExtension(CfgSt.OHS.HerostatName)}");
                return Util.RunExeInCmd("json2xmlb", $"\"{OHSpath.Team_bonus}\" \"{CompiledName}\"");
            }
            return true;
        }
        /// <summary>
        /// Split an XML file (<paramref name="XMLFilePath"/>) to the root's child elements and saves them to <paramref name="OutputFolder"/> as .xml files if they have the charactername attribute.
        /// </summary>
        /// <remarks>Exceptions: System.IO (WriteAllLines, XML: unknown)</remarks>
        /// <returns><see langword="True" />, if the file could be parsed as XML; otherwise <see langword="false" />.</returns>
        public static bool SplitXMLStats(string XMLFilePath, string OutputFolder)
        {
            if (GUIXML.GetXmlElement(XMLFilePath) is XmlElement Characters)
            {
                List<string> MlL = [], CnL = [];
                for (int i = 0; i < Characters.ChildNodes.Count; i++)
                {
                    if (Characters.ChildNodes[i] is XmlElement XE
                        && XE.GetAttribute("charactername") is string CN
                        && CN is not "" and not "defaultman")
                    {
                        if (File.Exists($"{OutputFolder}/{CN}.xml"))
                        {
                            CN = Path.GetFileNameWithoutExtension(OHSpath.GetVacant(Path.Combine(OutputFolder, $"{CN} ({XE.GetAttribute("name")})"), ".xml"));
                        }
                        XmlDocument Xdoc = new();
                        Xdoc.LoadXml(XE.OuterXml);
                        Xdoc.Save(Path.Combine(OutputFolder, $"{CN}.xml"));
                        CnL.Add(CN); MlL.Add(XE.GetAttribute("menulocation"));
                    }
                }
                Herostat.WriteCfgFiles(MlL, CnL);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Handling GUI specific XML actions
    /// </summary>
    internal static class GUIXML
    {
        internal static readonly XmlSerializerNamespaces ns = new([new XmlQualifiedName()]);

        internal static readonly XmlWriterSettings xws = new() { OmitXmlDeclaration = true, Indent = true };

        internal static readonly XmlReaderSettings xrs = new() { IgnoreComments = true };

        /// <summary>
        /// Import/append an <paramref name="XmlString"/> to the XML <paramref name="Doc"/>ument (root node). <paramref name="Doc"/> must have a root node.
        /// </summary>
        /// <returns><see langword="True"/>, if imported successfully; otherwise <see langword="false"/>.</returns>
        public static bool ImportXmlString(XmlDocument Doc, string XmlString)
        {
            XmlDocumentFragment Fragment = Doc.CreateDocumentFragment();
            Fragment.InnerXml = XmlString;
            return Doc.DocumentElement!.AppendChild(Fragment) is not null;
        }
        /// <summary>
        /// Deserialize the XML file at <paramref name="path"/> to the specified <paramref name="type"/>.
        /// </summary>
        /// <returns>The <see cref="object"/> of the specified <paramref name="type"/>, or <see langword="null"/> if deserialization failed.</returns>
        public static object? Deserialize(string path, Type type)
        {
            if (!File.Exists(path)) { return null; }
            try
            {
                XmlSerializer XS = new(type);
                using FileStream fs = File.OpenRead(path);
                return XS.Deserialize(fs);
            }
            catch { return null; }
        }
        /// <summary>
        /// Get the root <see cref="XmlElement"/> by providing the <paramref name="Path"/> to an XML file.
        /// </summary>
        /// <returns>The root <see cref="XmlElement"/> containing the complete XML structure. Returns <see langword="null"/> if loading file <paramref name="Path"/> failed or no <see cref="XmlElement"/> could be retreived.</returns>
        /// <exception cref="XmlException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="UriFormatException"/>
        public static XmlElement? GetXmlElement(string Path)
        {
            try
            {
                XmlDocument doc = new();
                using XmlReader reader = XmlReader.Create(Path, xrs);
                doc.Load(reader);
                return doc.DocumentElement;
            }
            catch { return null; }
        }
    }
}
