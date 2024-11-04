using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;

namespace OpenHeroSelectGUI.Functions
{
    /// <summary>
    /// Handling MarvelMods specific XML actions
    /// </summary>
    internal class MarvelModsXML
    {
        private static readonly XmlElement Riser = GUIXML.ElementFromString("<item effect=\"menu/riser\" enabled=\"false\" name=\"new_stage_fx\" neverfocus=\"true\" type=\"MENU_ITEM_EFFECT\" />")!;

        /// <summary>
        /// Decompile a file to the temp folder by generating a filename for the decompiled file using the <paramref name="CompiledName"/>.
        /// </summary>
        /// <returns>The filename for the decompiled file (.xml)</returns>
        private static string? DecompileToTemp(string CompiledName)
        {
            string DecompiledName = Path.Combine(OHSpath.Temp, $"{Path.GetFileNameWithoutExtension(CompiledName)}.xml");
            return Util.RunExeInCmd("json2xmlb", $"-d \"{CompiledName}\" \"{DecompiledName}\"") ? DecompiledName : null;
        }
        /// <summary>
        /// Compile <paramref name="XmlData"/> to <paramref name="CompiledName"/> file (full path) by writing to a temporary file first.
        /// </summary>
        /// <returns><see langword="True" />, if the file could be compiled, otherwise <see langword="false" />.</returns>
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
            List<SelectedCharacter> EL = [.. CfgSt.Roster.Selected.Where(c => !string.IsNullOrEmpty(c.Effect))];
            SelectedCharacter[] GRHT = [.. EL.Where(c => c.Loc is "03" or "24")];
            foreach (SelectedCharacter C in GRHT)
            {
                EL.Remove(C);
                EL.Insert(0, C);
            }
            if ((HasRiser || EL.Count > 0) && DecompileToTemp(LayoutDataFile) is string DLDF)
            {
                XmlDocument LayoutData = new();
                try { LayoutData.Load(DLDF); } catch { return LayoutDataFile; }
                if (LayoutData.DocumentElement is XmlElement menu_items)
                {
                    if (HasRiser) { _ = menu_items.AppendChild(LayoutData.ImportNode(Riser, true)); }
                    for (int i = 0; i < (CfgSt.GUI.HidableEffectsOnly && EL.Count > 1 ? 2 : EL.Count); i++)
                    {
                        // Alternatively, we could hex-edit to hide effects on different slots: Util.HexEdit(0x3cc67b, SC.Loc!, CfgSt.GUI.ActualGamePath + "/Game.exe")
                        if (AddEffect(EL[i].Loc, EL[i].Effect!, EL[i].Loc == "24" || i > 1 ? EL[i].Loc! : i > 0 ? "24" : "03") is XmlNode EffectLine)
                        {
                            _ = menu_items.AppendChild(LayoutData.ImportNode(EffectLine, true));
                        }
                    }
                    if (CompileToTarget(LayoutData, $"{DLDF}b")) { return $"{DLDF}b"; }
                }
            }
            return LayoutDataFile;
        }
        /// <summary>
        /// Modify the referenced effect file of <paramref name="EffectName"/> with the <paramref name="Loc"/> coordinates and copy it to the game files with the _<paramref name="FXslot"/> suffix.
        /// </summary>
        /// <returns>An <see cref="XmlElement"/> with the same values for team_back.xmlb</returns>
        private static XmlElement? AddEffect(string? Loc, string EffectName, string FXslot)
        {
            // Note: Any config.xml can theoretically not contain the required entries at the correct location. If this is the case, the app will throw an unhandled exception. This can be avoided by verifying the XML with a scheme definition.
            if (Loc is not null
                && GUIXML.GetXmlElement(Path.Combine(OHSpath.CD, "stages", ".effects", "config.xml")) is XmlElement EffectSetup
                && EffectSetup.SelectSingleNode($"descendant::Effect[Name=\"{EffectName}\"]") is XmlNode SelectedEffect
                && SelectedEffect["File"]!.InnerText is string EFN
                && Path.Combine(OHSpath.CD, "stages", ".effects", $"{EFN}.xml") is string EFP
                && File.Exists(EFP))
            {
                try
                {
                    XmlDocument EffectFile = new();
                    EffectFile.Load(EFP);
                    if (EffectFile.DocumentElement is XmlElement Effect
                        && CfgSt.Var.Layout is XmlElement CL
                        && CL.SelectSingleNode($"//Location_Setup/Location[@Number='{Loc}']") is XmlNode Coordinates
                        && SelectedEffect["Offset"] is XmlNode EffectOffset)
                    {
                        int X = int.Parse(Coordinates["X"]!.InnerText) + int.Parse(EffectOffset["X"]!.InnerText);
                        int Y = int.Parse(Coordinates["Y"]!.InnerText) + int.Parse(EffectOffset["Y"]!.InnerText);
                        int Z = int.Parse(Coordinates["Z"]!.InnerText) + int.Parse(EffectOffset["Z"]!.InnerText);
                        foreach (XmlElement EffectType in Effect.ChildNodes)
                        {
                            XmlAttribute O1 = EffectFile.CreateAttribute("origin");
                            XmlAttribute O2 = EffectFile.CreateAttribute("origin2");
                            O1.Value = O2.Value = $"{X} {Y} {Z} {X} {Y} {Z}";
                            if (EffectType.HasAttribute("origin"))
                                _ = EffectType.SetAttributeNode(O1);
                            if (EffectType.HasAttribute("origin2"))
                                _ = EffectType.SetAttributeNode(O2);
                        }
                        EFN = $"{EFN}_{FXslot}";
                        if (CompileToTarget(EffectFile, Path.Combine(Directory.CreateDirectory(Path.Combine(CfgSt.OHS.GameInstallPath, "effects", "menu")).FullName, $"{EFN}.xmlb")))
                        {
                            // WIP: This seems to work, but is still in beta phase
                            return GUIXML.ElementFromString($"<item effect=\"menu/{EFN}\" enabled=\"false\" name=\"pad{FXslot}_fx\" neverfocus=\"true\" type=\"MENU_ITEM_EFFECT\" />");
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
        /// <param name="PkgSourceFolders">Array of folders, each with a package folder to look in</param>
        /// <param name="Skin">SkinDetail</param>
        /// <param name="IntName">Internal name</param>
        /// <param name="TargetPath">Target folder to clone package to</param>
        /// <returns><see langword="True" />, if both packages were cloned successfully or the package exists, otherwise <see langword="false" />.</returns>
        public static bool ClonePackage(string[] PkgSourceFolders, SkinDetails Skin, string? IntName, string TargetPath)
        {
            try
            {
                for (int p = 0; p < PkgSourceFolders.Length; p++)
                {
                    DirectoryInfo PkgSrcF = new(OHSpath.Packages(PkgSourceFolders[p]));
                    if (File.Exists(Path.Combine(PkgSrcF.FullName, $"{IntName}_{Skin.CharNum}{Skin.Number}.pkgb"))) { return true; }
                    if (PkgSrcF.EnumerateFiles($"{IntName}_{Skin.CharNum}??.pkgb")
                               .FirstOrDefault() is FileInfo PkgSrc)
                    {
                        return ClonePackage(PkgSrc.FullName,
                        OHSpath.Packages(TargetPath, $"{IntName}_{Skin.CharNum}{Skin.Number}"),
                        Skin.CharNum, Skin.CharNum, Skin.Number);
                    }
                }
            }
            catch { return false; }
            return false;
        }
        /// <summary>
        /// Clone <paramref name="SourcePkg" /> to new <paramref name="PkgBasePath" /> (plus _nc if available), replacing <paramref name="CharNum"/> (plus num from source pkg) references with <paramref name="NewCharNum"/><paramref name="TargetNum"/>. <paramref name="SourcePkg" /> must be the path to an existing package.
        /// </summary>
        /// <returns><see langword="True"/> if both packages were cloned successfully, otherwise <see langword="false"/>.</returns>
        public static bool ClonePackage(string SourcePkg, string? PkgBasePath, string CharNum, string NewCharNum, string TargetNum)
        {
            string SourceNum = Path.GetFileNameWithoutExtension(SourcePkg)[^2..];
            string SourceNCpkg = SourcePkg[..SourcePkg.LastIndexOf('.')] + "_nc.pkgb";

            if (DecompileToTemp(SourcePkg) is string DP)
            {
                XmlDocument Pkg = new(), NcPkg = new();
                try { Pkg.Load(DP); } catch { return false; }
                if (Pkg.DocumentElement is XmlElement packagedef)
                {
                    if (SourceNum == "nc")
                    {
                        PkgReplInAttr(packagedef, CharNum, Path.GetFileNameWithoutExtension(SourcePkg)[^5..^3], NewCharNum, TargetNum);
                        return CompileToTarget(Pkg, $"{PkgBasePath}.pkgb");
                    }
                    else if (File.Exists(SourceNCpkg) && DecompileToTemp(SourceNCpkg) is string DN)
                    {
                        using XmlReader NCrdr = XmlReader.Create(DN);
                        try { NcPkg.Load(NCrdr); } catch { return false; }
                    }
                    else
                    {
                        XmlElement PD = NcPkg.CreateElement("packagedef");
                        for (int i = 0; i < packagedef.ChildNodes.Count; i++)
                        {
                            if (packagedef.ChildNodes[i] is XmlElement PkgElmt)
                            {
                                string filename = PkgElmt.GetAttribute("filename");
                                if (filename == $"hud_head_{CharNum}{SourceNum}") { i = packagedef.ChildNodes.Count; }
                                PkgElmt.SetAttribute("filename", PkgReplace(filename, CharNum, SourceNum, NewCharNum, TargetNum));
                                _ = PD.AppendChild(PkgElmt);
                            }
                        }
                        _ = NcPkg.AppendChild(PD);
                    }
                    PkgReplInAttr(packagedef, CharNum, SourceNum, NewCharNum, TargetNum);
                    PkgReplInAttr(NcPkg.DocumentElement!, CharNum, SourceNum, NewCharNum, TargetNum);
                    return CompileToTarget(Pkg, $"{PkgBasePath}.pkgb") && CompileToTarget(NcPkg, $"{PkgBasePath}_nc.pkgb");
                }
            }
            return false;
        }
        /// <summary>
        /// Extended replace method for packages: Call <see cref="PkgReplace"/> on all attributes of <see cref="XmlElement"/> <paramref name="XE"/>
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
        /// In <paramref name="filename"/> replace '<paramref name="CharNum"/><paramref name="SourceNum"/>'/'^<paramref name="CharNum"/>_' with '<paramref name="NewCharNum"/><paramref name="TargetNum"/>'/'^<paramref name="NewCharNum"/>_'.
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
                        string SD = Directory.GetParent(SourceFile)!.Parent!.FullName;
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
        /// Update the Teams character list link to the currently active game
        /// </summary>
        private static void UpdateTeams()
        {
            CfgSt.Roster.Teams = CfgSt.GUI.Game == "XML2" ? CfgSt.Roster.TeamsXML2 : CfgSt.Roster.TeamsMUA;
        }
        /// <summary>
        /// Load the team_bonus file as XML and deserialize it to the Teams <see cref="ObservableCollection{T}"/> for binding (adds <paramref name="DeleteCommand"/> to entries)
        /// </summary>
        public static void TeamBonusDeserializer(StandardUICommand DeleteCommand)
        {
            UpdateTeams();
            if (CfgSt.Roster.Teams.Count == 0
                && File.Exists(OHSpath.Team_bonus)
                && GUIXML.GetXmlElement(OHSpath.Team_bonus) is XmlElement Bonuses)
            {
                for (int i = 0; i < Bonuses.ChildNodes.Count; i++)
                {
                    TeamBonus Team = new()
                    {
                        Members = [],
                        Command = DeleteCommand
                    };
                    if (Bonuses.ChildNodes[i]!.Attributes is XmlAttributeCollection XAC
                        && Bonuses.ChildNodes[i]!.ChildNodes is XmlNodeList XM
                        && (XM.Count > 0 || XAC.GetNamedItem("skinset") is not null))
                    {
                        Team.Name = XAC.GetNamedItem("descname2") is XmlAttribute D2
                            ? $"{XAC.GetNamedItem("descname1")!.Value} {D2.Value}"
                            : XAC.GetNamedItem("descname1")!.Value;
                        Team.Sound = XAC.GetNamedItem("sound")!.Value;
                        Team.Descbonus = XAC.GetNamedItem("descbonus")!.Value!.Replace("%%", "%");
                        Team.Skinset = CfgSt.GUI.Game == "XML2" && XAC.GetNamedItem("skinset") is XmlAttribute SS ? SS.Value : "";
                        for (int m = 0; m < XM.Count; m++)
                        {
                            if (XM[m]!.Attributes is XmlAttributeCollection XMA && XMA.GetNamedItem("name") is XmlAttribute M)
                                Team.Members.Add(new TeamMember { Name = M.Value, Skin = XMA.GetNamedItem("skin") is XmlAttribute S ? S.Value : "" });
                        }
                        CfgSt.Roster.Teams.Add(Team);
                    }
                }
            }
        }
        /// <summary>
        /// Serializes the Team <see cref="ObservableCollection{T}"/> to XML and saves it to the <paramref name="BonusFile"/> (team_bonus)
        /// </summary>
        /// <returns><see langword="True"/> if a new file was created, otherwise <see langword="false"/>.</returns>
        public static bool TeamBonusSerializer(string BonusFile)
        {
            UpdateTeams();
            if (CfgSt.Roster.Teams.Count > 0)
            {
                XmlDocument Bonuses = new();
                Bonuses.LoadXml("<bonuses></bonuses>");
                Dictionary<string, string> PU = CfgSt.GUI.Game == "XML2" ? InternalSettings.TeamPowerupsXML2 : InternalSettings.TeamPowerups;
                for (int i = 0; i < CfgSt.Roster.Teams.Count; i++)
                {
                    if (CfgSt.Roster.Teams[i] is TeamBonus TB
                        && TB.Descbonus is string DB
                        && TB.Members is ObservableCollection<TeamMember> M
                        && TB.Name is string N
                        && Bonuses.DocumentElement!.AppendChild(Bonuses.CreateElement("bonus")) is XmlElement Bonus)
                    {
                        Bonus.SetAttribute("descbonus", DB.Replace("%", "%%"));
                        Bonus.SetAttribute("descname1", N);
                        if (N.Length > 8 && N.Split(" ") is string[] Names && Names.Length > 1)
                        {
                            int Split = N.Length;
                            for (int s = 0, l = 0; s < Names.Length; s++)
                            {
                                l += Names[s].Length + 1;
                                Split = Math.Abs(N.Length / 2 - l) < Math.Abs(N.Length / 2 - Split) ? l : Split;
                            }
                            Bonus.SetAttribute("descname1", N[..(Split - 1)]);
                            Bonus.SetAttribute("descname2", N[Split..]);
                        }
                        Bonus.SetAttribute("powerup", PU[DB]);
                        if (!string.IsNullOrEmpty(TB.Skinset))
                        {
                            Bonus.SetAttribute("skinset", TB.Skinset);
                        }
                        else
                        {
                            for (int m = 0; m < M.Count; m++)
                            {
                                if (Bonus.AppendChild(Bonuses.CreateElement("hero")) is XmlElement Hero)
                                {
                                    Hero.SetAttribute("name", M[m].Name);
                                    if (!string.IsNullOrWhiteSpace(M[m].Skin)) { Hero.SetAttribute("skin", M[m].Skin); }
                                }
                            }
                        }
                        Bonus.SetAttribute("sound", TB.Sound);
                    }
                }
                try
                {
                    XmlWriterSettings xws = new() { OmitXmlDeclaration = true, Indent = true };
                    using XmlWriter xw = XmlWriter.Create(BonusFile, xws);
                    Bonuses.Save(xw);
                    return true;
                }
                catch { return false; }
            }
            return false;
        }
        /// <summary>
        /// Serializes the Team <see cref="ObservableCollection{T}"/> to XML (if it has any team bonuses) and saves it to the compiled team_bonus file in the game folder. (May fail?)
        /// </summary>
        /// <returns><see langword="True" />, if a new team bonus was created and json2xmlb could convert it successfully, otherwise <see langword="false" />.</returns>
        public static bool TeamBonusCopy()
        {
            if (TeamBonusSerializer(OHSpath.Team_bonus))
            {
                string team_bonus = CfgSt.GUI.ModPack ? CfgSt.GUI.TeamBonusName : "team_bonus";
                string CompiledName = Path.Combine(CfgSt.OHS.GameInstallPath, "data", $"{team_bonus}{Path.GetExtension(CfgSt.OHS.HerostatName)}");
                return Util.RunExeInCmd("json2xmlb", $"\"{OHSpath.Team_bonus}\" \"{CompiledName}\"");
            }
            return true;
        }
    }

    /// <summary>
    /// Handling GUI specific XML actions
    /// </summary>
    internal class GUIXML
    {
        /// <summary>
        /// Create an <see cref="XmlElement"/> from an <paramref name="XmlString"/>.
        /// </summary>
        /// <returns>The <see cref="XmlElement"/> or <see langword="null" /> if parsing was unsuccessful.</returns>
        public static XmlElement? ElementFromString(string XmlString)
        {
            XmlDocument doc = new();
            try { doc.LoadXml(XmlString); } catch { return null; }
            return doc.DocumentElement;
        }
        /// <summary>
        /// Get the root <see cref="XmlElement"/> by providing the <paramref name="Path"/> to an XML file.
        /// </summary>
        /// <returns>The root <see cref="XmlElement"/> containing the complete XML structure. Returns <see langword="null"/> if loading file <paramref name="Path"/> failed or no <see cref="XmlElement"/> could be retreived.</returns>
        public static XmlElement? GetXmlElement(string Path)
        {
            try
            {
                XmlDocument doc = new();
                using XmlReader reader = XmlReader.Create(Path, new XmlReaderSettings() { IgnoreComments = true });
                doc.Load(reader);
                return doc.DocumentElement;
            }
            catch { return null; }
        }
        /// <summary>
        /// Get the value of a root <paramref name="Attribute" /> from an <paramref name="XmlData" /> <see langword="string[]" />. Instead of using System.XML.
        /// </summary>
        /// <returns>The attribute value or <see cref="string.Empty"/></returns>
        public static string GetRootAttribute(string[] XmlData, string Attribute)
        {
            return ElementFromString(string.Join(Environment.NewLine, XmlData)) is XmlElement Root ? Root.GetAttribute(Attribute) : string.Empty;
        }
        /// <summary>
        /// Parse the XML stage details (<paramref name="M"/>) to stage info with image and details (add riser <see cref="bool"/> from <paramref name="CM"/>).
        /// </summary>
        /// <returns><see cref="StageModel"/> info or <see langword="null"/> if M info doesn't point to a valid folder or folder access failed.</returns>
        public static StageModel? GetStageInfo(XmlElement M, XmlElement CM)
        {
            if (Path.Combine(OHSpath.Model, M["Path"]!.InnerText) is string MP)
            {
                DirectoryInfo ModelFolder = new(MP);
                int count;
                try { count = ModelFolder.GetFiles("*.igb").Length; } catch { count = 0; }
                if (count > 0)
                {
                    IEnumerable<FileInfo> ModelImages = ModelFolder
                        .EnumerateFiles("*.png")
                        .Union(ModelFolder.EnumerateFiles("*.jpg"))
                        .Union(ModelFolder.EnumerateFiles("*.bmp"))
                        .Union(new DirectoryInfo(OHSpath.Model).EnumerateFiles(".NoPreview.png"));
                    // Note: Performs a crash, if stage files integrity is broken
                    StageModel StageItem = new()
                    {
                        Name = M["Name"]!.InnerText,
                        Creator = M["Creator"]!.InnerText,
                        Path = ModelFolder,
                        Image = new BitmapImage(new Uri(ModelImages.First().FullName)),
                        Riser = CM.InnerText == "Official" && CM.GetAttribute("Riser") == "true",
                        Favourite = CfgSt.GUI.StageFavourites.Contains(M["Name"]!.InnerText)
                    };
                    return StageItem;
                }
            }
            return null;
        }
        /// <summary>
        /// Get the effects list from stages/.effects/config.xml
        /// </summary>
        /// <returns>Returns a <see cref="List{string}"/> of all effect names in the file</returns>
        public static List<string> GetAvailableEffects()
        {
            List<string> AvailableEffects = [];
            if (GetXmlElement(Path.Combine(OHSpath.CD, "stages", ".effects", "config.xml")) is XmlElement EffectSetup)
            {
                for (int i = 0; i < EffectSetup.ChildNodes.Count; i++)
                {
                    if (EffectSetup.ChildNodes[i]!["Name"]!.InnerText is string EffectName)
                        AvailableEffects.Add(EffectName);
                }
            }
            return AvailableEffects;
        }
        /// <summary>
        /// Split an XML file (<paramref name="XMLFilePath"/>) to the root's child elements and saves them to <paramref name="OutputFolder"/> as .xml files if they have the charactername attribute.
        /// </summary>
        /// <returns><see langword="True" />, if the file could be parsed as XML and writing files succeeds, otherwise <see langword="false" /></returns>
        public static bool SplitXMLStats(string XMLFilePath, string OutputFolder)
        {
            if (GetXmlElement(XMLFilePath) is XmlElement Characters)
            {
                try
                {
                    List<string> MlL = [], CnL = [];
                    for (int i = 0; i < Characters.ChildNodes.Count; i++)
                    {
                        if (Characters.ChildNodes[i] is XmlElement XE
                            && XE.GetAttribute("charactername") is string CN
                            && CN is not "" and not "defaultman")
                        {
                            if (File.Exists(Path.Combine(OutputFolder, $"{CN}.xml")))
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
                catch { return false; }
            }
            return false;
        }
    }
}
