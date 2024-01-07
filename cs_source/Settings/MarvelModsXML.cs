using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;
using static OpenHeroSelectGUI.Settings.InternalSettings;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Handling MarvelMods specific XML actions
    /// </summary>
    internal class MarvelModsXML
    {
        private static readonly XmlElement Riser = XmlElementFromString("<item effect=\"menu/riser\" enabled=\"false\" name=\"new_stage_fx\" neverfocus=\"true\" type=\"MENU_ITEM_EFFECT\" />")!;

        /// <summary>
        /// Create an XML element from an XML string.
        /// </summary>
        /// <returns>The XmlElement or null if parsing was unsuccessful</returns>
        public static XmlElement? XmlElementFromString(string XmlString)
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
        /// Decompile a file to the temp folder by generating a filename for the decompiled file using the filename from the copiled file.
        /// </summary>
        /// <returns>The filename for the decompiled file (.xml)</returns>
        private static string? DecompileToTemp(string CompiledName)
        {
            string DecompiledName = Path.Combine(Directory.CreateDirectory(Path.Combine(cdPath, "Temp")).FullName, $"{Path.GetFileNameWithoutExtension(CompiledName)}.xml");
            if (Util.RunExeInCmd("json2xmlb", $"-d \"{CompiledName}\" \"{DecompiledName}\"")) { return DecompiledName; }
            return null;
        }
        /// <summary>
        /// Compile XML data to a defined file by writing to a temporary file first.
        /// </summary>
        /// <returns>True, if the file could be compiled, otherwise false.</returns>
        private static bool CompileToTarget(XmlDocument XmlData, string CompiledName)
        {
            string DecompiledName = Path.Combine(Directory.CreateDirectory(Path.Combine(cdPath, "Temp")).FullName, $"{Path.GetFileNameWithoutExtension(CompiledName)}.xml");
            XmlData.Save(DecompiledName);
            return Directory.Exists(Path.GetDirectoryName(CompiledName)) && Util.RunExeInCmd("json2xmlb", $"\"{DecompiledName}\" \"{CompiledName}\"");
        }
        /// <summary>
        /// Add data from settings to team_back.xmlb: Riser, Effects | WIP: Possibly make this a general function for other files as well (no others currently, charinfo etc are handled by OHS)
        /// </summary>
        /// <returns>The updated team_back.xmlb, or the input file if failed or no changes necessary - file as path (string)</returns>
        public static string UpdateLayout(string LayoutDataFile, bool HasRiser)
        {
            IEnumerable<SelectedCharacter> EL = CharacterLists.Instance.Selected.Where(c => !string.IsNullOrEmpty(c.Effect));
            if ((HasRiser || EL.Any()) && DecompileToTemp(LayoutDataFile) is string DLDF)
            {
                XmlDocument LayoutData = new();
                LayoutData.Load(DLDF);
                if (LayoutData.DocumentElement is XmlElement menu_items)
                {
                    if (HasRiser)
                        _ = menu_items.AppendChild(LayoutData.ImportNode(Riser, true));
                    for (int i = 0; i < (GUIsettings.Instance.HidableEffectsOnly && EL.Count() > 1 ? 2 : EL.Count()); i++)
                    {
                        SelectedCharacter SC = EL.ToArray()[i];
                        // WIP: Currently, the user must have effects on 24 and 03 to have hidden effects. We can freely use locations an hex edit though. The limit's still 2.
                        if (AddEffect(SC.Loc!, SC.Effect!, GUIsettings.Instance.HidableEffectsOnly ? i > 1 ? "24" : "03" : SC.Loc!) is XmlNode EffectLine)
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
        /// Add effect to the extracted as XML layout data file (team_back.xmlb.xml) by specifying the location and effect name
        /// </summary>
        private static XmlElement? AddEffect(string Loc, string EffectName, string FXslot)
        {
            // Note: Any config.xml can theoretically not contain the required entries at the correct location. If this is the case, the app will throw an unhandled exception. This can be avoided by verifying the XML with a scheme definition.
            XmlElement? EffectSetup = GUIXML.GetXmlElement(Path.Combine(cdPath, "stages", ".effects", "config.xml"));
            if (EffectSetup.SelectSingleNode($"descendant::Effect[Name=\"{EffectName}\"]") is XmlNode SelectedEffect && SelectedEffect["File"].InnerText is string EFN)
            {
                XmlDocument EffectFile = new();
                EffectFile.Load(Path.Combine(cdPath, "stages", ".effects", $"{EFN}.xml"));
                if (EffectFile.DocumentElement is XmlElement Effect)
                {
                    XmlNode Coordinates = DynamicSettings.Instance.Layout.SelectSingleNode($"//Location_Setup/Location[@Number='{Loc}']");
                    XmlNode EffectOffset = SelectedEffect["Offset"];
                    int X = int.Parse(Coordinates["X"].InnerText) + int.Parse(EffectOffset["X"].InnerText);
                    int Y = int.Parse(Coordinates["Y"].InnerText) + int.Parse(EffectOffset["Y"].InnerText);
                    int Z = int.Parse(Coordinates["Z"].InnerText) + int.Parse(EffectOffset["Z"].InnerText);
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
                    if (CompileToTarget(EffectFile, Path.Combine(Directory.CreateDirectory(Path.Combine(OHSsettings.Instance.GameInstallPath, "effects", "menu")).FullName, $"{EFN}.xmlb")))
                    {
                        // WIP: This seems to work, but is still in beta phase
                        return XmlElementFromString($"<item effect=\"menu/{EFN}\" enabled=\"false\" name=\"pad{FXslot}_fx\" neverfocus=\"true\" type=\"MENU_ITEM_EFFECT\" />");
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Get folders with a package folder from the game folder and MO2 mod folders, according to the settings.
        /// </summary>
        /// <returns>Array with matching folder paths as strings or empty array</returns>
        public static string[] GetFoldersWpkg()
        {
            string[] PkgSourceFolders = { OHSsettings.Instance.GameInstallPath, GUIsettings.Instance.GameInstallPath };
            if (Directory.Exists(OHSsettings.Instance.GameInstallPath)
                && Directory.GetParent(OHSsettings.Instance.GameInstallPath) is DirectoryInfo MO2
                && MO2.Name == "mods")
            {
                PkgSourceFolders = PkgSourceFolders.Concat(MO2.EnumerateDirectories().Select(d => d.FullName)).ToArray();
            }
            return PkgSourceFolders.Where(f => Directory.Exists(Path.Combine(f, "packages", "generated", "characters"))).Distinct().ToArray();
        }
        /// <summary>
        /// Look for Package and try to clone if not found but other skin package found.
        /// </summary>
        /// <param name="PkgSourceFolders">Array of folders, each with a package folder to look in</param>
        /// <param name="Skin">SkinDetail</param>
        /// <param name="IntName">Internal name</param>
        /// <returns>True if successfully cloned both packages or package exists, otherwise false.</returns>
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
        /// Clone packages for same character.
        /// </summary>
        /// <returns>True if both packages were cloned successfully, otherwise false.</returns>
        public static bool ClonePackage(string SourcePkg, string? IntName, string CharNum, string TargetNum) => ClonePackage(SourcePkg, IntName, CharNum, CharNum, TargetNum);
        /// <summary>
        /// Clone packages, providing character info and target number. SourcePkg must be the path to an existing package.
        /// </summary>
        /// <returns>True if both packages were cloned successfully, otherwise false.</returns>
        public static bool ClonePackage(string SourcePkg, string? IntName, string CharNum, string NewCharNum, string TargetNum)
        {
            string SourceNum = Path.GetFileNameWithoutExtension(SourcePkg)[^2..];
            string SourceNCpkg = SourcePkg[..(SourcePkg.LastIndexOf('.'))] + "_nc.pkgb";

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
                            if (l <= i) PD.AppendChild(packagedef.ChildNodes[i]);
                        }
                        _ = NcPkg.AppendChild(PD);
                    }
                    ReplaceInAttributes(packagedef, CharNum + SourceNum, NewCharNum + TargetNum);
                    ReplaceInAttributes(NcPkg.DocumentElement, CharNum + SourceNum, NewCharNum + TargetNum);
                    string PkgBasePath = Path.Combine(OHSsettings.Instance.GameInstallPath, "packages", "generated", "characters", $"{IntName}_{NewCharNum}{TargetNum}");
                    return CompileToTarget(Pkg, $"{PkgBasePath}.pkgb") && CompileToTarget(NcPkg, $"{PkgBasePath}_nc.pkgb");
                }
            }
            return false;
        }
        /// <summary>
        /// Use replace on all attributes of an XML element
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
        public static readonly string team_bonus = Path.Combine(cdPath, "team_bonus.engb.xml");
        /// <summary>
        /// Loads the team_bonus file as XML and deserializes it to a list for binding
        /// </summary>
        /// <returns>A Team list with the team bonus content</returns>
        public static void TeamBonusDeserializer(StandardUICommand DeleteCommand)
        {
            if (File.Exists(team_bonus) && GUIXML.GetXmlElement(team_bonus) is XmlElement Bonuses)
            {
                CharacterLists.Instance.Teams.Clear();
                for (int i = 0; i < Bonuses.ChildNodes.Count; i++)
                {
                    TeamBonus Team = new()
                    {
                        Members = new ObservableCollection<TeamMember>(),
                        Command = DeleteCommand
                    };
                    if (Bonuses.ChildNodes[i]!.Attributes is XmlAttributeCollection XAC && Bonuses.ChildNodes[i]!.ChildNodes is XmlNodeList XM)
                    {
                        Team.Name = (XAC.GetNamedItem("descname2") is XmlAttribute D2) ? $"{XAC.GetNamedItem("descname1")!.Value} {D2.Value}" : XAC.GetNamedItem("descname1")!.Value;
                        Team.Sound = XAC.GetNamedItem("sound")!.Value;
                        Team.Descbonus = XAC.GetNamedItem("descbonus")!.Value!.Replace("%%", "%");
                        for (int m = 0; m < XM.Count; m++)
                        {
                            if (XM[m]!.Attributes is XmlAttributeCollection XMA && XMA.GetNamedItem("name") is XmlAttribute M)
                                Team.Members.Add(new TeamMember { Name = M.Value, Skin = XMA.GetNamedItem("skin") is XmlAttribute S ? S.Value : "" });
                        }
                    }
                    CharacterLists.Instance.Teams.Add(Team);
                }
            }
        }
        /// <summary>
        /// Serializes the Team list to XML and saves it to the specified team_bonus file
        /// </summary>
        public static bool TeamBonusSerializer(string BonusFile)
        {
            if (CharacterLists.Instance.Teams.Count == 0) { return false; }
            XmlDocument Bonuses = new();
            Bonuses.LoadXml("<bonuses></bonuses>");
            for (int i = 0; i < CharacterLists.Instance.Teams.Count; i++)
            {
                if (Bonuses.DocumentElement!.AppendChild(Bonuses.CreateElement("bonus")) is XmlElement Bonus && CharacterLists.Instance.Teams[i].Descbonus is string DB && CharacterLists.Instance.Teams[i].Members is ObservableCollection<TeamMember> M)
                {
                    Bonus.SetAttribute("descbonus", DB.Replace("%", "%%"));
                    Bonus.SetAttribute("descname1", CharacterLists.Instance.Teams[i].Name);
                    if (CharacterLists.Instance.Teams[i].Name is string N && N.Length > 8 && N.Split(" ") is string[] Names && Names.Length > 1)
                    {
                        int Split = N.Length;
                        for (int s = 0, l = 0; s < Names.Length; s++)
                        {
                            l += Names[s].Length + 1;
                            Split = Math.Abs((N.Length / 2) - l) < Math.Abs((N.Length / 2) - Split) ? l : Split;
                        }
                        Bonus.SetAttribute("descname1", N[..(Split - 1)]);
                        Bonus.SetAttribute("descname2", N[Split..]);
                    }
                    Bonus.SetAttribute("powerup", TeamPowerups[DB]);
                    Bonus.SetAttribute("sound", CharacterLists.Instance.Teams[i].Sound);

                    for (int m = 0; m < M.Count; m++)
                    {
                        if (Bonus.AppendChild(Bonuses.CreateElement("hero")) is XmlElement Hero)
                        {
                            Hero.SetAttribute("name", M[m].Name);
                            if (!string.IsNullOrEmpty(M[m].Skin)) { Hero.SetAttribute("skin", M[m].Skin); }
                        }
                    }
                }
            }
            XmlWriterSettings xws = new() { OmitXmlDeclaration = true };
            using XmlWriter xw = XmlWriter.Create(BonusFile, xws);
            Bonuses.Save(xw);
            return true;
        }
        /// <summary>
        /// Serializes the Team list to XML and saves it to the compiled team_bonus file in the game folder. May fail.
        /// </summary>
        public static void TeamBonusCopy()
        {
            if (TeamBonusSerializer(team_bonus))
            {
                string CompiledName = Path.Combine(OHSsettings.Instance.GameInstallPath, "data", $"team_bonus{Path.GetExtension(OHSsettings.Instance.HerostatName)}");
                _ = Util.RunExeInCmd("json2xmlb", $"\"{team_bonus}\" \"{CompiledName}\"");
            }
        }
    }

    /// <summary>
    /// Handling GUI specific XML actions
    /// </summary>
    internal class GUIXML
    {
        /// <summary>
        /// Get the root XML elemet by providing the path to an XML file. File must not have an XML identifier.
        /// </summary>
        /// <returns>The root XML element containing the complete XML structure</returns>
        public static XmlElement? GetXmlElement(string Path)
        {
            XmlElement? XmlElement = null;
            if (File.Exists(Path))
            {
                XmlDocument XmlDocument = new();
                using XmlReader reader = XmlReader.Create(Path, new XmlReaderSettings() { IgnoreComments = true });
                XmlDocument.Load(reader);
                if (XmlDocument.FirstChild is XmlElement Root) XmlElement = Root;
            }
            return XmlElement;
        }
        /// <summary>
        /// Get the value of a root attribute from an XML string. Only to avoid use of System.XML.
        /// </summary>
        /// <returns>The attribute value or empty string</returns>
        public static string GetRootAttribute(string[] XmlData, string AttrName)
        {
            if (MarvelModsXML.XmlElementFromString(string.Join(Environment.NewLine, XmlData)) is XmlElement Root)
            {
                return Root.GetAttribute(AttrName);
            }
            return string.Empty;
        }
        /// <summary>
        /// Parse the XML stage details to Stage info with image and details
        /// </summary>
        /// <returns>Stage info</returns>
        public static StageModel? GetStageInfo(XmlElement? M, XmlElement? CM)
        {
            DirectoryInfo ModelFolder = new(Path.Combine(ModelPath, M["Path"].InnerText));
            if (ModelFolder.Exists && ModelFolder.EnumerateFiles("*.igb").Any())
            {
                IEnumerable<FileInfo> ModelImages = ModelFolder
                    .EnumerateFiles("*.png")
                    .Union(ModelFolder.EnumerateFiles("*.jpg"))
                    .Union(ModelFolder.EnumerateFiles("*.bmp"))
                    .Union(new DirectoryInfo(ModelPath).EnumerateFiles(".NoPreview.png"));
                StageModel StageItem = new()
                {
                    Name = M["Name"].InnerText,
                    Creator = M["Creator"].InnerText,
                    Path = ModelFolder,
                    Image = new BitmapImage(new Uri(ModelImages.First().FullName)),
                    Riser = CM.InnerText == "Official" && CM.GetAttribute("Riser") == "true"
                };
                return StageItem;
            }
            return null;
        }
        /// <summary>
        /// Get the effects list from stages/.effects/config.xml
        /// </summary>
        /// <returns>Returns a list of all effect names in the file</returns>
        public static List<string> GetAvailableEffects()
        {
            List<string> AvailableEffects = new();
            XmlElement? EffectSetup = GetXmlElement(Path.Combine(cdPath, "stages", ".effects", "config.xml"));
            for (int i = 0; i < EffectSetup.ChildNodes.Count; i++)
            {
                if (EffectSetup.ChildNodes[i]["Name"].InnerText is string EffectName)
                    AvailableEffects.Add(EffectName);
            }
            return AvailableEffects;
        }
    }
}
