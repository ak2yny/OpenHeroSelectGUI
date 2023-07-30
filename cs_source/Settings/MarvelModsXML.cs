using System.Collections.Generic;
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
        private static XmlElement? XmlElementFromString(string XmlString)
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
        private static string DecompileToTemp(string CompiledName)
        {
            string DecompiledName = Path.Combine(Directory.CreateDirectory(Path.Combine(cdPath, "Temp")).FullName, $"{Path.GetFileNameWithoutExtension(CompiledName)}.xml");
            _ = Util.RunDosCommnand("json2xmlb", $"-d \"{CompiledName}\" \"{DecompiledName}\"");
            return DecompiledName;
        }
        /// <summary>
        /// Compile XML data to a defined file by writing to a temporary file first.
        /// </summary>
        private static void CompileToTarget(XmlDocument XmlData, string CompiledName)
        {
            string DecompiledName = Path.Combine(Directory.CreateDirectory(Path.Combine(cdPath, "Temp")).FullName, $"{Path.GetFileNameWithoutExtension(CompiledName)}.xml");
            XmlData.Save(DecompiledName);
            _ = Util.RunDosCommnand("json2xmlb", $"\"{DecompiledName}\" \"{CompiledName}\"");
        }
        /// <summary>
        /// Add data from settings to team_back.xmlb: Riser, Effects | WIP: Possibly make this a general function for other files as well (no others currently, charinfo etc are handled by OHS)
        /// </summary>
        /// <returns>The updated team_back.xmlb or the input file if failed or no changes necessary - file as path (string)</returns>
        public static string UpdateLayout(string LayoutDataFile)
        {
            IEnumerable<SelectedCharacter> EL = CharacterLists.Instance.Selected.Where(c => !string.IsNullOrEmpty(c.Effect));
            if (DynamicSettings.Instance.Riser || EL.Any())
            {
                string DLDF = DecompileToTemp(LayoutDataFile);

                XmlDocument LayoutData = new();
                LayoutData.Load(DLDF);
                if (LayoutData.DocumentElement is XmlElement menu_items)
                {
                    if (DynamicSettings.Instance.Riser)
                        _ = menu_items.AppendChild(LayoutData.ImportNode(Riser, true));
                    for (int i = 0; i < (GUIsettings.Instance.HidableEffectsOnly && EL.Count() > 1 ? 2 : EL.Count()); i++)
                    {
                        SelectedCharacter SC = EL.ToArray()[i];
                        // WIP: Currently, the user must have effects on 24 an 03 to have hidden effects. We can freely use locations an hex edit though. The limit's still 2 though.
                        XmlNode? EffectLine = AddEffect(SC.Loc!, SC.Effect!, GUIsettings.Instance.HidableEffectsOnly ? i > 1 ? "24" : "03" : SC.Loc!);
                        if (EffectLine != null)
                            _ = menu_items.AppendChild(LayoutData.ImportNode(EffectLine, true));
                    }

                    CompileToTarget(LayoutData, $"{DLDF}b");
                    return $"{DLDF}b";
                }
            }
            return LayoutDataFile;
        }
        /// <summary>
        /// Add effect to the extracted as XML layout data file (team_back.xmlb.xml) by specifying the location and effect name
        /// </summary>
        private static XmlElement? AddEffect(string Loc, string EffectName, string FXslot)
        {
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

                    CompileToTarget(EffectFile, Path.Combine(Directory.CreateDirectory(Path.Combine(OHSsettings.Instance.GameInstallPath, "effects", "menu")).FullName, $"{EFN}.xmlb"));
                    XmlElement Test = XmlElementFromString($"<item effect=\"menu/{EFN}\" enabled=\"false\" name=\"pad{FXslot}_fx\" neverfocus=\"true\" type=\"MENU_ITEM_EFFECT\" />");
                    return Test;
                }
            }
            return null;
        }
        /// <summary>
        /// WIP: (possibly remove) Add an XML element to an xml file (possibly change to xmlb)
        /// </summary>
        private static void AddXmlElement(string DataFile, XmlElement Add)
        {
            XmlDocument XData = new();
            XData.Load(DataFile);
            if (XData.DocumentElement is XmlElement Root)
            {
                _ = Root.AppendChild(XData.ImportNode(Add, true));
            }
        }
        /// <summary>
        /// Clone packages for same character.
        /// </summary>
        public static void ClonePackage(string SourcePkg, string? IntName, string CharNum, string TargetNum) => ClonePackage(SourcePkg, IntName, CharNum, CharNum, TargetNum);
        /// <summary>
        /// Clone packages, providing character info and target number. SourcePkg must be the path to an existing package.
        /// </summary>
        public static void ClonePackage(string SourcePkg, string? IntName, string CharNum, string NewCharNum, string TargetNum)
        {
            string SourceNum = Path.GetFileNameWithoutExtension(SourcePkg)[^2..];
            string SourceNCpkg = SourcePkg[..(SourcePkg.LastIndexOf('.'))] + "_nc.pkgb";

            XmlDocument Pkg = new(), NcPkg = new();
            Pkg.Load(DecompileToTemp(SourcePkg));
            if (Pkg.DocumentElement is XmlElement packagedef)
            {
                if (SourceNum == "nc")
                {
                    SourceNum = Path.GetFileNameWithoutExtension(SourcePkg)[^5..^3];
                }
                else if (File.Exists(SourceNCpkg))
                {
                    using XmlReader NCrdr = XmlReader.Create(DecompileToTemp(SourceNCpkg));
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
                CompileToTarget(Pkg, $"{PkgBasePath}.pkgb");
                CompileToTarget(NcPkg, $"{PkgBasePath}_nc.pkgb");
            }
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
