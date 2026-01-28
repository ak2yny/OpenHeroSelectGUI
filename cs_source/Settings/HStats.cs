using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Lightweight, immutable structure, defining <see cref="Delimiter"/> and <see cref="Ending"/> <see cref="char"/>acters used for parsing non-XML herostat attributes.
    /// </summary>
    internal readonly struct FormatCharacters(char delimiter, char ending)
    {
        internal readonly char Delimiter = delimiter;
        internal readonly char Ending = ending;
        internal readonly bool IsJSON = delimiter == ':';
    }
    /// <summary>
    /// Herostat class with pre-defined attributes for binding. Includes source file <see cref="HsPath"/>.
    /// </summary>
    internal partial class HStats : ObservableObject
    {
        internal string? HsPath;

        internal bool IsXML;

        internal string? InternalName { get; private set; }

        [ObservableProperty]
        internal partial string? CharacterName { get; private set; }

        [ObservableProperty]
        internal partial string CharNum { get; private set; } = ""; // Or "00" fallback?

        internal ObservableCollection<SkinDetails> SkinsList { get; init; } = [];

        [ObservableProperty]
        internal partial bool CanAddSkins { get; private set; } = false;

        [ObservableProperty]
        internal partial bool AnySkins { get; private set; } = false;

        [ObservableProperty]
        internal partial string MannequinNumber { get; private set; } = "01";
        /// <summary>
        /// Calculate the mannequin number, based on the first <see cref="SkinDetails"/> in <see cref="SkinsList"/>.
        /// </summary>
        /// <returns>Number <see cref="string"/> of the first skin if XML2 and bigger than 10; otherwise "01".</returns>
        private string MqNum => CfgSt.GUI.IsXml2 && AnySkins && SkinsList[0].Num > 10 ? SkinsList[0].Number : "01";

        private readonly string[] SkinIdentifiers;

        private readonly StandardUICommand DeleteCommand = new(StandardUICommandKind.Delete);

        /// <summary>
        /// Initializes a new instance from the source herostat <see cref="HsPath"/>, which is constructed from the floating character (<paramref name="FC"/>).
        /// </summary>
        internal HStats()
        {
            SkinIdentifiers = InternalSettings.GetSkinAttributes;
            DeleteCommand.ExecuteRequested += RemoveSkin;
        }
        /// <summary>
        /// Loads attributes from the source herostat <see cref="HsPath"/>, which is constructed from the floating character (<paramref name="FC"/>).
        /// </summary>
        /// <remarks>
        /// The herostat should be either XML or a simple key-value format. The constructor determines the format automatically.
        /// The properties are incomplete, not all attributes are read at this time.
        /// Exceptions: System.IO exceptions. Incl. file not found and path invalid.
        /// </remarks>
        internal void Load(string FC, bool LoadSkins = true)
        {
            CharacterName = CharNum = "";
            SkinsList.Clear();
            CanAddSkins = AnySkins = false;
            if (!string.IsNullOrEmpty(FC))
            {
                HsPath = OHSpath.GetHsFile(FC);
                XmlElement? XML = null;
                Dictionary<string, string> Attributes = new(StringComparer.OrdinalIgnoreCase);

                using FileStream fs = File.OpenRead(HsPath);
                IsXML = fs.IsXml();
                if (IsXML)
                {
                    XmlDocument xd = new();
                    xd.Load(fs);
                    XML = xd.DocumentElement;
                    // For future use of additional attributes:
                    // internal XmlElement? XML = xd.DocumentElement;
                    // internal bool TryGet(string name, out string? value)
                    // {
                    //     return !string.IsNullOrEmpty(name) && (IsXML
                    //         ? !string.IsNullOrEmpty(value = XML.GetAttribute(name))
                    //         : Attributes.TryGetValue(name, out value) && !string.IsNullOrEmpty(value));
                    // }
                    // Or alternatively:
                    // if (xd.DocumentElement is XmlElement el)
                    // {
                    //     Attributes = el.Attributes.Cast<XmlAttribute>()
                    //         .ToDictionary(a => a.Name, a => a.Value, StringComparer.OrdinalIgnoreCase);
                    // }
                    // Attributes.TryGetValue(name, out value)
                }
                else
                {
                    using StreamReader sr = new(fs);
                    // Theoretically, we already have Format, but this is safer in case of leading comments
                    FormatCharacters? format = null;

                    while (!sr.EndOfStream)
                    {
                        string? line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) { continue; }
                        ReadOnlySpan<char> span = line.AsSpan().Trim();

                        if (format is FormatCharacters f) {
                            if (span.IndexOf(f.Delimiter) is int pos && pos > 0)
                            {
                                ReadOnlySpan<char> val = span[(pos + 1)..].Trim().TrimEnd(f.Ending).TrimEnd();
                                if (f.IsJSON) { val = val.Trim('"'); }
                                _ = Attributes.TryAdd(span[..pos].Trim().Trim('"').ToString(), val.ToString());
                            }
                        }
                        else
                        {
                            format = (span.IndexOf(':') is int cn_i && cn_i > 2 && span[0] == '"')
                                   ? new FormatCharacters(':', ',')
                                   : (span.IndexOf('=') is int eq_i && eq_i > 0) // && eq_i < cn_i
                                   ? new FormatCharacters('=', ';')
                                   : null;
                        }
                    }
                    // Or alternatively:
                    // [GeneratedRegex(@"^\s*""?(?<key>[^""\s:=]+)""?\s*(?:\:|=)\s*""?(?<val>[^""]*?)""?\s*[;,]?\s*$", RegexOptions.Multiline)]
                    // private static partial Regex ToAttributes();
                    // MatchCollection Attributes = ToAttributes().Matches(sr.ReadToEnd());
                    // for (int i = 0; i < Attributes.Count; i++)
                    // {
                    //     GroupCollection KeyVal = Attributes[i].Groups;
                    //     if (KeyVal["key"].Value is string key && key.Length > 0)
                    //     {
                    //         _ = AttributesDictionary.TryAdd(key, KeyVal["val"].Value);
                    //     }
                    // }
                }
                IsXML = XML != null;
                if (!IsXML && Attributes.Count == 0) { return; }
                Func<string, string> GetAttribute = IsXML
                    ? (name => XML!.GetAttribute(name))
                    : (name => Attributes.TryGetValue(name, out string? value) ? value : "");
                if (GetAttribute("skin") is string Skin && Skin.Length > 3)
                {
                    CharNum = Skin[..^2];
                    InternalName = GetAttribute("name");
                    CharacterName = GetAttribute("charactername");
                    if (LoadSkins)
                    {
                        for (int i = 0; i < SkinIdentifiers.Length; i++)
                        {
                            string SkinNumber = GetAttribute(SkinIdentifiers[i]);
                            int N = -1; if (CfgSt.GUI.IsXml2 || SkinNumber.Length > 1 && int.TryParse(SkinNumber[^2..], out N))
                            {
                                SkinsList.Add(GetSkin(N,
                                    CfgSt.GUI.IsXml2 ? InternalSettings.XML2SkinNames[i] : GetAttribute(InternalSettings.MUASkinNameAttributes[i])
                                ));
                            }
                        }
                        CanAddSkins = SkinsList.Count < SkinIdentifiers.Length;
                        AnySkins = SkinsList.Count > 0;
                        MannequinNumber = MqNum;
                    }
                }
            }
        }
        /// <summary>
        /// Save edited <see cref="SkinsList"/>, by replacing the skin attributes in the source <see cref="HsPath"/>. (Currently skins only)
        /// </summary>
        /// <remarks>Exceptions: System.IO exceptions (additionally). List empty or XML2 size wrong.</remarks>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="XmlException"/>
        internal void Save()
        {
            if (SkinsList.Count == 0)
            {
                throw new InvalidOperationException("Skins list is empty.");
            }
            bool isMUA = CfgSt.GUI.IsMua;
            string[] SkinIds = SkinIdentifiers;
            int SkinsCount = SkinsList.Count;
            int MaxCount = SkinIds.Length;
            ObservableCollection<SkinDetails> UsedSkins = isMUA ? SkinsList : [];
            if (!isMUA)
            {
                if (SkinsCount != MaxCount) { throw new Exception("Internal error, XML2 skins list size was modified."); }
                SkinsCount = SkinsList.Count(s => s.Number != "");
                foreach (SkinDetails SD in SkinsList.OrderBy(s => s.Number == "")) { UsedSkins.Add(SD); }
                SkinIds = [SkinIds[0], .. UsedSkins.Skip(1).Select(s => s.Name)];
            }
            // IsXML = StreamIsXml(fs); should already be known from Load
            if (IsXML)
            {
                // Expected to throw an exception if HsPath is inaccessible or null
                using FileStream fs = File.Open(HsPath!, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                XmlDocument XmlStat = new();
                XmlStat.Load(fs);
                if (XmlStat.DocumentElement is XmlElement Stats
                    && Stats.GetAttributeNode("skin") is XmlAttribute Skin)
                {
                    void SetAttribute(string Name, string Value)
                    {
                        XmlAttribute? Attr = Stats.GetAttributeNode(Name);
                        if (Attr is null)
                        {
                            Attr = XmlStat.CreateAttribute(Name);
                            _ = Stats.SetAttributeNode(Attr);
                        }
                        Attr.Value = Value;
                    }
                    Skin.Value = $"{CharNum}{SkinsList[0].Number}";
                    if (isMUA) { SetAttribute("skin_01_name", SkinsList[0].Name); }

                    for (int i = 1; i < SkinsCount; i++)
                    {
                        SetAttribute(SkinIds[i], UsedSkins[i].Number);
                        if (isMUA) { SetAttribute(InternalSettings.MUASkinNameAttributes[i], UsedSkins[i].Name); }
                    }
                    for (int i = SkinsCount; i < MaxCount; i++)
                    {
                        Stats.RemoveAttribute(SkinIds[i]);
                        if (isMUA) { Stats.RemoveAttribute(InternalSettings.MUASkinNameAttributes[i]); }
                    }
                    XmlStat.Save(fs);
                }
                else
                {
                    throw new XmlException("Skin attribute not found in XML herostat.");
                }
            }
            else
            {
                List<string> LoadedHerostat = [.. File.ReadLines(HsPath!)];
                int Indx = LoadedHerostat.IndexOf(LoadedHerostat.First(l => l.Trim().Trim('"').StartsWith("skin", StringComparison.OrdinalIgnoreCase)));
                if (Indx > -1)
                {
                    // A skin should always be present, but in case it isn't and a child element with a skin attribut exists,
                    // the result will be corrupted, but the herostat is already corrupted in that case
                    string SkinLine = LoadedHerostat[Indx];
                    int st_i = 0; while (char.IsWhiteSpace(SkinLine[st_i])) { st_i++; }
                    bool IsJSON = SkinLine.IndexOf(':') is int cn_i && cn_i > st_i + 2 && SkinLine[st_i] == '"';
                    if (IsJSON || (SkinLine.IndexOf('=') is int eq_i && eq_i > st_i)) // && eq_i < cn_i or cn_i == -1
                    {
                        string Attribute = SkinLine[..st_i] + (IsJSON ? "\"{0}\": \"{1}\","
                                                                      : "{0} = {1} ;");
                        void SetAttribute(int Ix, string Name, string Value)
                        {
                            if (LoadedHerostat[Ix].Trim().Trim('"').StartsWith("skin", StringComparison.OrdinalIgnoreCase))
                            {
                                LoadedHerostat[Ix] = string.Format(Attribute, Name, Value);
                            }
                            else
                            {
                                LoadedHerostat.Insert(Ix, string.Format(Attribute, Name, Value));
                            }
                        }
                        int mult = isMUA ? 2 : 1;
                        for (int i = 0; i < SkinsCount; i++)
                        {
                            int r_i = Indx + (i * mult);
                            SetAttribute(r_i, SkinIds[i], i == 0
                                ? $"{CharNum}{UsedSkins[i].Number}"
                                : UsedSkins[i].Number);
                            if (isMUA)
                            {
                                SetAttribute(r_i + 1,
                                    InternalSettings.MUASkinNameAttributes[i],
                                    UsedSkins[i].Name);
                            }
                        }
                        for (int i = Indx + (SkinsCount * mult); LoadedHerostat[i].Trim().Trim('"')
                            .StartsWith("skin", StringComparison.OrdinalIgnoreCase);)
                        {
                            LoadedHerostat.RemoveAt(i);
                        }
                    }
                }
                File.WriteAllLines(HsPath!, LoadedHerostat);
            }
        }
        /// <summary>
        /// Gets <see cref="SkinDetails"/> from <paramref name="SkinNumber"/> and <paramref name="SkinName"/>, using <see cref="CharNum"/> and <see cref="DeleteCommand"/>.
        /// </summary>
        private SkinDetails GetSkin(int SkinNumber, string SkinName) => new(CharNum, SkinNumber, SkinName, DeleteCommand);
        /// <summary>
        /// Adds the <paramref name="Skin"/> to the <see cref="SkinsList"/>, replacing <see cref="SkinDetails.CharNum"/> with <see cref="CharNum"/>.
        /// </summary>
        internal void AddSkin(int SkinNumber, string SkinName)
        {
            SkinsList.Add(GetSkin(SkinNumber, SkinName));
            CanAddSkins = SkinsList.Count < SkinIdentifiers.Length;
            AnySkins = true;
        }
        /// <summary>
        /// Remove first skin from <see cref="SkinsList"/> that matches the number of the command.
        /// </summary>
        private void RemoveSkin(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is SkinDetails SD && (CfgSt.GUI.IsMua || SD.Number != ""))
            {
                for (int i = 0; i < SkinsList.Count; i++)
                {
                    if (SkinsList[i] == SD)
                    {
                        RemoveSkinAt(i);
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Remove skin from <see cref="SkinsList"/> at <paramref name="index"/>. (Must be a positive number)
        /// </summary>
        internal void RemoveSkinAt(int index)
        {
            if (SkinsList.Count > index) // && CharNum != ""
            {
                if (CfgSt.GUI.IsMua)
                {
                    SkinsList.RemoveAt(index);
                    CanAddSkins = true;
                    AnySkins = SkinsList.Count > 0;
                }
                else if (index > 0)
                {
                    // Just avoiding using ObservableProperty on SkinDetails.Number
                    SkinsList[index].Number = "";
                    SkinsList[index] = SkinsList[index];
                }
            }
        }
        /// <summary>
        /// Fix the XML2 <see cref="SkinsList"/>'s skin names to be in the correct order and, if <paramref name="Fill"/>, fill missing skins with empty entries.
        /// </summary>
        internal void FixXML2names(bool Fill = false)
        {
            // SkinsList.Count and InternalSettings.XML2SkinNames.Length should be 9 by design
            // Index out of range exception is thrown (as expected) if less (unhandled if more)
            int Count = Fill ? SkinsList.Count : 9;
            for (int i = 0; i < Count; i++)
            {
                string Name = InternalSettings.XML2SkinNames[i];
                if (SkinsList[i].Name != Name)
                {
                    SkinsList[i].Name = Name;
                    SkinsList[i] = SkinsList[i];
                }
            }
            if (Fill && Count < 9) // Should never happen; Just a safeguard
            {
                for (; Count < 9; Count++)
                {
                    SkinsList.Add(GetSkin(-1, InternalSettings.XML2SkinNames[Count]));
                }
                CanAddSkins = !(AnySkins = true);
            }
            MannequinNumber = MqNum;
        }
        /// <summary>
        /// Updates the mannequin number to match the current first skin number if XML2 mode is enabled.
        /// </summary>
        internal void UpdateMannequinNumber(int Num) => MannequinNumber = AnySkins && Num > 10 ? $"{Num}" : "01";
    }
    /// <summary>
    /// Light Herostat class that represents a decompiled file (no comments), for easy, cached attribute access. Includes source file <see cref="HsPath"/>.
    /// </summary>
    internal class Stats
    {
        internal string? HsPath;

        internal bool IsXML;
        // The attribute value cached for future calls.
        //private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _attributeCache = new(StringComparer.OrdinalIgnoreCase);

        private readonly string? _text;

        private readonly XmlElement? _XML;
        /// <summary>
        /// Gets the internal name of the currently loaded herostat of this instance. Returns <see cref="string.Empty"/> if not found.
        /// </summary>
        internal string InternalName => IsXML ? _XML!.GetAttribute("name") :
            Herostat.NameRX().Match(_text!) is Match M && M.Success ? M.Value : string.Empty;
        /// <summary>
        /// Gets the character name of the currently loaded herostat of this instance. Returns <see cref="string.Empty"/> if not found.
        /// </summary>
        internal string CharacterName => IsXML ? _XML!.GetAttribute("charactername") :
            Herostat.CharNameRX().Match(_text!) is Match M && M.Success ? M.Value : string.Empty;
        /// <summary>
        /// Initializes a new instance from the full source herostat <paramref name="Path"/> and caches the contents, depending on <see cref="IsXML"/>.
        /// </summary>
        /// <remarks>Exceptions: System.IO exceptions. Incl. file not found and path invalid.</remarks>
        internal Stats(string Path)
        {
            HsPath = Path;
            using FileStream fs = File.OpenRead(Path);
            IsXML = fs.IsXml();
            fs.Close();
            if (IsXML && GUIXML.GetXmlElement(Path) is XmlElement Root)
            {
                _XML = Root;
            }
            else
            {
                IsXML = false; // If XML parsing failed, treat as other format
                _text = File.ReadAllText(Path);
            }
        }
        /// <summary>
        /// Retrieves the value of the specified <paramref name="AttrName"/> from <see cref="_XML"/> (if <see cref="IsXML"/>) or <see cref="_text"/>.
        /// </summary>
        /// <param name="AttrName">The name of the attribute to retrieve. Is expected to be lowercase on XML <see cref="Format"/>s, is case-insensitive on others.</param>
        /// <returns>The value of the specified <paramref name="AttrName"/> if it's found; otherwise, <see cref="string.Empty"/>.</returns>
        internal string RootAttribute(string AttrName) => IsXML ? _XML!.GetAttribute(AttrName) : GetAttribute(AttrName);
        /// <summary>
        /// Retrieves the value of the specified <paramref name="AttrName"/> from <see cref="_text"/>.
        /// </summary>
        private string GetAttribute(string AttrName)
        {
            Match match = new Regex($@"(?<=^\s*""?{AttrName}(?:"":\s*""|\s*=\s*))\S.+?(?="",\s*$|\s*;?\s*$)",
                          RegexOptions.IgnoreCase | RegexOptions.Multiline).Match(_text!);
            return match.Success ? match.Value : string.Empty;
        }
        /// <summary>
        /// Write <see cref="_XML"/> (if <see cref="IsXML"/>) or <see cref="_text"/> to a new file next to <see cref="HsPath"/>, replacing <paramref name="OldNum"/>ber with <paramref name="NewNum"/>ber.
        /// </summary>
        internal void Clone(string OldNum, string NewNum)
        {
            string Ext = Path.GetExtension(HsPath!);
            string New = OHSpath.GetVacant($"{HsPath![..^Ext.Length]} {NewNum}", Ext);
            if (IsXML)
            {
                // SelectNodes and OwnerDocument are not null because _XML is XmlDocument.DocumentElement
                foreach (XmlAttribute SkinAttr in _XML!.SelectNodes("*/@skin")!) { SkinAttr.Value = $"{NewNum}{SkinAttr.Value[^2..]}"; }
                if (_XML!.GetAttributeNode("characteranims") is XmlAttribute ca) { ca.Value = $"{NewNum}{ca.Value[OldNum.Length..]}"; }
                using XmlWriter xw = XmlWriter.Create(New, GUIXML.xws);
                _XML!.OwnerDocument!.Save(xw);
            }
            else
            {
                File.WriteAllText(New, Regex.Replace(_text!, $@"((?:characteranims|skin)(?:"":\s*""|\s*=\s*)){OldNum}(_|\d\d)", "${1}" + NewNum + "${2}"));
            }
            CfgSt.Var.FloatingCharacter = Path.GetRelativePath(OHSpath.HsFolder, New)[..^Ext.Length];
        }
        /// <summary>
        /// Change the specified <paramref name="AttributeName"/> to <paramref name="NewValue"/> in the herostat associated with <see cref="HsPath"/> (saves the modified file).
        /// </summary>
        /// <remarks>Exceptions: System.IO exceptions (additionally). Attribute not found (only non-XML).</remarks>
        /// <exception cref="ArgumentException"/>
        public void ChangeAttributeValue(string AttributeName, string NewValue)
        {
            if (IsXML)
            {
                XmlDocument XmlStat = _XML!.OwnerDocument;
                XmlAttribute? Attr = _XML.GetAttributeNode(AttributeName);
                if (Attr is null)
                {
                    Attr = XmlStat.CreateAttribute(AttributeName);
                    _ = _XML.SetAttributeNode(Attr);
                }
                Attr.Value = NewValue;
                using FileStream fs = File.OpenWrite(HsPath!);
                XmlStat.Save(fs);
            }
            else
            {
                string[] LoadedHerostat = _text!.Split(Environment.NewLine);
                for (int i = 0; i < LoadedHerostat.Length; i++)
                {
                    string Line = LoadedHerostat[i];
                    if (!Line.Trim().Trim('"').StartsWith(AttributeName, StringComparison.OrdinalIgnoreCase)) { continue; }
                    int st_i = 0; while (char.IsWhiteSpace(Line[st_i])) { st_i++; }
                    bool IsJSON = Line.IndexOf(':', st_i) is int cn_i && cn_i > st_i + 2 && Line[st_i] == '"';
                    if (IsJSON || (Line.IndexOf('=', st_i) is int eq_i && eq_i > st_i)) // && eq_i < cn_i or cn_i == -1
                    {
                        string Attribute = Line[..st_i] + (IsJSON ? "\"{0}\": \"{1}\","
                                                                      : "{0} = {1} ;");
                        LoadedHerostat[i] = string.Format(Attribute, AttributeName, NewValue);
                    }
                    File.WriteAllLines(HsPath!, LoadedHerostat);
                    return;
                }
                throw new ArgumentException($"Attribute '{AttributeName}' not found in '{HsPath}'.");
            }
        }
    }
}
