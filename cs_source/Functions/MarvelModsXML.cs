using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;
using static OpenHeroSelectGUI.Settings.InternalSettings;

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
            string DecompiledName = Path.Combine(Directory.CreateDirectory(Path.Combine(OHSpath.CD, "Temp")).FullName, $"{Path.GetFileNameWithoutExtension(CompiledName)}.xml");
            return Util.RunExeInCmd("json2xmlb", $"-d \"{CompiledName}\" \"{DecompiledName}\"") ? DecompiledName : null;
        }
        /// <summary>
        /// Compile <paramref name="XmlData"/> to <paramref name="CompiledName"/> file (full path) by writing to a temporary file first.
        /// </summary>
        /// <returns><see langword="True" />, if the file could be compiled, otherwise <see langword="false" />.</returns>
        private static bool CompileToTarget(XmlDocument XmlData, string CompiledName)
        {
            string DecompiledName = Path.Combine(Directory.CreateDirectory(Path.Combine(OHSpath.CD, "Temp")).FullName, $"{Path.GetFileNameWithoutExtension(CompiledName)}.xml");
            XmlData.Save(DecompiledName);
            return Directory.Exists(Path.GetDirectoryName(CompiledName)) && Util.RunExeInCmd("json2xmlb", $"\"{DecompiledName}\" \"{CompiledName}\"");
        }
        /// <summary>
        /// Add data from settings to team_back.xmlb (<paramref name="LayoutDataFile"/>): Riser (<paramref name="HasRiser"/>), Effects | WIP: Possibly make this a general function for other files as well (no others currently, charinfo etc are handled by OHS)
        /// </summary>
        /// <returns>The updated team_back.xmlb file name <see cref="string"/>, or the input file name (<paramref name="LayoutDataFile"/>) if failed or no changes necessary</returns>
        public static string UpdateLayout(string LayoutDataFile, bool HasRiser)
        {
            SelectedCharacter[] EL = CfgSt.Roster.Selected.Where(c => !string.IsNullOrEmpty(c.Effect)).ToArray();
            if ((HasRiser || EL.Length > 0) && DecompileToTemp(LayoutDataFile) is string DLDF)
            {
                XmlDocument LayoutData = new();
                LayoutData.Load(DLDF);
                if (LayoutData.DocumentElement is XmlElement menu_items)
                {
                    if (HasRiser)
                        _ = menu_items.AppendChild(LayoutData.ImportNode(Riser, true));
                    for (int i = 0; i < (CfgSt.GUI.HidableEffectsOnly && EL.Length > 1 ? 2 : EL.Length); i++)
                    {
                        SelectedCharacter SC = EL[i];
                        // WIP: Currently, the user must have effects on 24 and 03 to have hidden effects. We can freely use locations an hex edit though. The limit's still 2.
                        if (AddEffect(SC.Loc, SC.Effect!, CfgSt.GUI.HidableEffectsOnly ? i > 1 ? "24" : "03" : SC.Loc!) is XmlNode EffectLine)
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
            return null;
        }
        /// <summary>
        /// Look for package in <paramref name="PkgSourceFolders" /> and, if none found that match <paramref name="Skin" /> and <paramref name="IntName" />, try to clone another package if one exists.
        /// </summary>
        /// <param name="PkgSourceFolders">Array of folders, each with a package folder to look in</param>
        /// <param name="Skin">SkinDetail</param>
        /// <param name="IntName">Internal name</param>
        /// <returns><see langword="True" />, if both packages were cloned successfully or the package exists, otherwise <see langword="false" />.</returns>
        public static bool ClonePackage(string[] PkgSourceFolders, SkinDetails Skin, string? IntName)
        {
            for (int p = 0; p < PkgSourceFolders.Length; p++)
            {
                DirectoryInfo PkgSrcF = new(Path.Combine(PkgSourceFolders[p], "packages", "generated", "characters"));
                if (File.Exists(Path.Combine(PkgSrcF.FullName, $"{IntName}_{Skin.CharNum}{Skin.Number}.pkgb"))) { return true; }
                if (PkgSrcF.EnumerateFiles($"{IntName}_{Skin.CharNum}??.pkgb")
                           .FirstOrDefault() is FileInfo PkgSrc)
                { return ClonePackage(PkgSrc.FullName, IntName, Skin.CharNum, Skin.Number); }
            }
            return false;
        }
        /// <summary>
        /// Clone <paramref name="SourcePkg" /> for <paramref name="IntName" />_<paramref name="CharNum" /> to new <paramref name="TargetNum" />.
        /// </summary>
        /// <returns><see langword="True" /> if both packages were cloned successfully, otherwise <see langword="false" />.</returns>
        public static bool ClonePackage(string SourcePkg, string? IntName, string CharNum, string TargetNum) => ClonePackage(SourcePkg, IntName, CharNum, CharNum, TargetNum);
        /// <summary>
        /// Clone <paramref name="SourcePkg" /> to new <paramref name="IntName" />_<paramref name="NewCharNum" /><paramref name="TargetNum" />, specifying source <paramref name="CharNum" /> for replace. <paramref name="SourcePkg" /> must be the path to an existing package.
        /// </summary>
        /// <returns><see langword="True" /> if both packages were cloned successfully, otherwise <see langword="false" />.</returns>
        public static bool ClonePackage(string SourcePkg, string? IntName, string CharNum, string NewCharNum, string TargetNum)
        {
            string SourceNum = Path.GetFileNameWithoutExtension(SourcePkg)[^2..];
            string SourceNCpkg = SourcePkg[..SourcePkg.LastIndexOf('.')] + "_nc.pkgb";

            XmlDocument Pkg = new(), NcPkg = new();
            if (DecompileToTemp(SourcePkg) is string DP)
            {
                Pkg.Load(DP);
                if (Pkg.DocumentElement is XmlElement packagedef)
                {
                    if (SourceNum == "nc")
                    {
                        SourceNum = Path.GetFileNameWithoutExtension(SourcePkg)[^5..^3];
                    }
                    else if (File.Exists(SourceNCpkg) && DecompileToTemp(SourceNCpkg) is string DN)
                    {
                        using XmlReader NCrdr = XmlReader.Create(DN);
                        NcPkg.Load(NCrdr);
                    }
                    else
                    {
                        XmlElement PD = NcPkg.CreateElement("packagedef");
                        for (int i = 0, l = 0; i < packagedef.ChildNodes.Count; i++)
                        {
                            if (packagedef.ChildNodes[i] is XmlElement PkgElmt)
                            {
                                string filename = PkgElmt.GetAttribute("filename");
                                if (filename == $"hud_head_{CharNum}{SourceNum}") l = i;
                                PkgElmt.SetAttribute("filename", filename.Replace(CharNum + SourceNum, NewCharNum + TargetNum));
                            }
                            if (l <= i) { _ = PD.AppendChild(packagedef.ChildNodes[i]!); }
                        }
                        _ = NcPkg.AppendChild(PD);
                    }
                    ReplaceInAttributes(packagedef, CharNum + SourceNum, NewCharNum + TargetNum);
                    ReplaceInAttributes(NcPkg.DocumentElement!, CharNum + SourceNum, NewCharNum + TargetNum);
                    string PkgBasePath = Path.Combine(CfgSt.OHS.GameInstallPath, "packages", "generated", "characters", $"{IntName}_{NewCharNum}{TargetNum}");
                    return CompileToTarget(Pkg, $"{PkgBasePath}.pkgb") && CompileToTarget(NcPkg, $"{PkgBasePath}_nc.pkgb");
                }
            }
            return false;
        }
        /// <summary>
        /// Replace <paramref name="SearchString"/> with <paramref name="ReplaceString"/> on all attributes of <see cref="XmlElement"/> <paramref name="XE"/>
        /// </summary>
        private static void ReplaceInAttributes(XmlElement XE, string SearchString, string ReplaceString)
        {
            XmlNodeList XNL = XE.SelectNodes($"//attribute::*[contains(., '{SearchString}')]/..")!;
            for (int i = 0; i < XNL.Count; i++)
            {
                XmlAttributeCollection XAC = XNL[i]!.Attributes!;
                for (int a = 0; a < XAC.Count; a++)
                {
                    XAC[a].Value = XAC[a].Value.Replace(SearchString, ReplaceString);
                }
            }
        }
        /// <summary>
        /// Update the Teams character list link to the currently active game
        /// </summary>
        private static void UpdateTeams()
        {
            CfgSt.Roster.Teams = CfgSt.GUI.Game == "xml2" ? CfgSt.Roster.TeamsXML2 : CfgSt.Roster.TeamsMUA;
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
                        Team.Skinset = CfgSt.GUI.Game == "xml2" && XAC.GetNamedItem("skinset") is XmlAttribute SS ? SS.Value : "";
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
        public static bool TeamBonusSerializer(string BonusFile)
        {
            UpdateTeams();
            if (CfgSt.Roster.Teams.Count > 0)
            {
                XmlDocument Bonuses = new();
                Bonuses.LoadXml("<bonuses></bonuses>");
                Dictionary<string, string> PU = CfgSt.GUI.Game == "xml2" ? TeamPowerupsXML2 : TeamPowerups;
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
                                    if (!string.IsNullOrEmpty(M[m].Skin)) { Hero.SetAttribute("skin", M[m].Skin); }
                                }
                            }
                        }
                        Bonus.SetAttribute("sound", TB.Sound);
                    }
                }
                XmlWriterSettings xws = new() { OmitXmlDeclaration = true, Indent = true };
                using XmlWriter xw = XmlWriter.Create(BonusFile, xws);
                Bonuses.Save(xw);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Serializes the Team <see cref="ObservableCollection{T}"/> to XML and saves it to the compiled team_bonus file in the game folder. May fail.
        /// </summary>
        public static void TeamBonusCopy()
        {
            if (TeamBonusSerializer(OHSpath.Team_bonus))
            {
                string CompiledName = Path.Combine(CfgSt.OHS.GameInstallPath, "data", $"{CfgSt.GUI.TeamBonusName}{Path.GetExtension(CfgSt.OHS.HerostatName)}");
                _ = Util.RunExeInCmd("json2xmlb", $"\"{OHSpath.Team_bonus}\" \"{CompiledName}\"");
            }
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
        /// <returns>The <see cref="XmlElement"/> or <see langword="null" /> if parsing was unsuccessful</returns>
        public static XmlElement? ElementFromString(string XmlString)
        {
            XmlDocument doc = new();
            doc.LoadXml(XmlString);
            if (doc.DocumentElement is XmlElement Root)
            {
                return Root;
            }
            return null;
        }
        /// <summary>
        /// Get the root <see cref="XmlElement"/> by providing the <paramref name="Path" /> to an XML file.
        /// </summary>
        /// <returns>The root <see cref="XmlElement"/> containing the complete XML structure</returns>
        public static XmlElement? GetXmlElement(string Path)
        {
            XmlElement? XmlElement = null;
            if (File.Exists(Path))
            {
                XmlDocument XmlDocument = new();
                using XmlReader reader = XmlReader.Create(Path, new XmlReaderSettings() { IgnoreComments = true });
                XmlDocument.Load(reader);
                if (XmlDocument.DocumentElement is XmlElement Root) XmlElement = Root;
            }
            return XmlElement;
        }
        /// <summary>
        /// Get the value of a root <paramref name="Attribute" /> from an <paramref name="XmlData" /> <see langword="string[]" />. Instead of using System.XML.
        /// </summary>
        /// <returns>The attribute value or <see cref="string.Empty" /></returns>
        public static string GetRootAttribute(string[] XmlData, string Attribute)
        {
            return ElementFromString(string.Join(Environment.NewLine, XmlData)) is XmlElement Root ? Root.GetAttribute(Attribute) : string.Empty;
        }
        /// <summary>
        /// Parse the XML stage details (<paramref name="M"/>) to stage info with image and details (add riser <see cref="bool"/> from <paramref name="CM"/>).
        /// </summary>
        /// <returns>Stage info</returns>
        public static StageModel? GetStageInfo(XmlElement M, XmlElement CM)
        {
            if (Path.Combine(OHSpath.Model, M["Path"]!.InnerText) is string MP && Directory.Exists(MP))
            {
                DirectoryInfo ModelFolder = new(MP);
                if (ModelFolder.Exists && ModelFolder.EnumerateFiles("*.igb").Any())
                {
                    IEnumerable<FileInfo> ModelImages = ModelFolder
                        .EnumerateFiles("*.png")
                        .Union(ModelFolder.EnumerateFiles("*.jpg"))
                        .Union(ModelFolder.EnumerateFiles("*.bmp"))
                        .Union(new DirectoryInfo(OHSpath.Model).EnumerateFiles(".NoPreview.png"));
                    StageModel StageItem = new()
                    {
                        Name = M["Name"]!.InnerText,
                        Creator = M["Creator"]!.InnerText,
                        Path = ModelFolder,
                        Image = new BitmapImage(new Uri(ModelImages.First().FullName)),
                        Riser = CM.InnerText == "Official" && CM.GetAttribute("Riser") == "true"
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
        /// <returns><see langword="True" />, if the file could be parsed as XML, otherwise <see langword="false" /></returns>
        public static bool SplitXMLStats(string XMLFilePath, string OutputFolder)
        {
            if (GetXmlElement(XMLFilePath) is XmlElement Characters)
            {
                List<string> MlL = [], CnL = [];
                DirectoryInfo HSFolder = Directory.CreateDirectory(OHSpath.GetOHSFolder(OutputFolder));
                for (int i = 0; i < Characters.ChildNodes.Count; i++)
                {
                    if (Characters.ChildNodes[i] is XmlElement XE
                        && XE.GetAttribute("charactername") is string CN
                        && CN is not "" and not "defaultman")
                    {
                        XmlDocument Xdoc = new();
                        Xdoc.LoadXml(XE.OuterXml);
                        Xdoc.Save(Path.Combine(HSFolder.FullName, $"{CN}.xml"));
                        CnL.Add(CN); MlL.Add(XE.GetAttribute("menulocation"));
                    }
                }
                Herostat.WriteCfgFiles(MlL, CnL);
                return true;
            }
            return false;
        }
    }
}
