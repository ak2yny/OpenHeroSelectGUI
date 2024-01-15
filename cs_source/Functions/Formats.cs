using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenHeroSelectGUI.Functions
{
    internal static class Formats
    {
        /// <summary>
        /// Search a fake XML or JSON file as <paramref name="array" /> for an attribute <paramref name="name" />. Ignores parent/child relations. Treats nodes as attributes but returns empty.
        /// </summary>
        /// <param name="array">A <see cref="string[]" /> of lines read from a fake XML or JSON file</param>
        /// <param name="name">The name of the attribute</param>
        /// <returns>The value of the first matching attribute or <see cref="string.Empty" /> if not found</returns>
        public static string GetAttr(string[] array, string name)
        {
            Regex RXstartsWith = new($"^\"?{name}(\": | =)");
            string[] Lines = Array.FindAll(array, c => RXstartsWith.IsMatch(c.Trim().ToLower()));
            if (Lines.Length != 0)
            {
                CfgSt.Var.HsFormat = Lines[0].Trim()[0] == '"' ? ':' : '=';
                return Lines[0].Split([CfgSt.Var.HsFormat], 2)[1].TrimEnd(';').TrimEnd(',').Trim().Trim('"');
            }
            return string.Empty;
        }
    }
    internal static partial class Herostat
    {
        [GeneratedRegex(@"^""?menulocation(\"": | = )")]
        private static partial Regex MLRX();

        /// <summary>
        /// Load herostat for the character defined in the argument as relative path.
        /// </summary>
        /// <returns>First found herostat as <see langword="string[]"/> array or <see langword="null" /> if not found</returns>
        public static string[]? Load(string? FC)
        {
            if (!string.IsNullOrEmpty(FC)
                && GetFile(FC) is FileInfo HS)
            {
                string[] Herostat = [.. File.ReadLines(HS.FullName).Where(l => l.Trim() != "")];
                CfgSt.Var.HsPath = HS;
                CfgSt.Var.HsFormat = Herostat[0].Trim()[0];
                return Herostat;
            }
            return null;
        }
        /// <summary>
        /// Get the full path to the herostat file, providing the path relative in the herostat folder.
        /// </summary>
        /// <returns><see cref="FileInfo"/> of the first herostat found or <see langword="null" /> if not found</returns>
        public static FileInfo? GetFile(string HsPath)
        {
            return Path.Combine(OHSpath.HsFolder, HsPath) is string FolderString
                && FolderString.Replace('\\', '/').LastIndexOf('/') is int S
                && new DirectoryInfo(FolderString[..S].TrimEnd('/')) is DirectoryInfo folder
                && folder.Exists
                && folder.EnumerateFiles($"{FolderString[(S + 1)..]}.??????????").FirstOrDefault() is FileInfo HS
                ? HS
                : null;
        }
        /// <summary>
        /// Gets the internal name from the currently selected character
        /// </summary>
        /// <returns>The internal name or <see cref="string.Empty" /></returns>
        public static string GetInternalName()
        {
            return Load(CfgSt.Var.FloatingCharacter) is string[] HS ? RootAttribute(HS, "name") : string.Empty;
        }
        /// <summary>
        /// Get a herostat root attribute by providing a herostat <see langword="string[]"/>, regardless of the format.
        /// </summary>
        /// <returns>The value of the attribute or <see cref="string.Empty" /> if not found</returns>
        public static string RootAttribute(string[] Herostat, string AttrName)
        {
            return Herostat[0].Trim()[0] == '<' ? GUIXML.GetRootAttribute(Herostat, AttrName) : Formats.GetAttr(Herostat, AttrName);
        }
        /// <summary>
        /// Splits <paramref name="Herostat"/> <see langword="string[]"/>, based on depth count by curly brackets. Saves them in <paramref name="HsFormat"/> to the <paramref name="OutputFolder"/>, if they have the charactername line.
        /// </summary>
        public static void Split(string[] Herostat, char HsFormat, string OutputFolder)
        {
            int Depth = HsFormat == '{' ? -1 : 0;
            List<string> SplitStat = [], MlL = [], CnL = [];
            string CN = "", ML = "";
            string Ext = HsFormat == '{' ? ".json" : ".txt";
            DirectoryInfo HSFolder = Directory.CreateDirectory(OHSpath.GetOHSFolder(OutputFolder));
            for (int i = 0; i < Herostat.Length; i++)
            {
                string Line = Herostat[i].Trim();
                if (Depth > 1 || (Depth == 1 && Line != ""))
                {
                    SplitStat.Add(Herostat[i]);
                }
                if (!string.IsNullOrEmpty(Line))
                {
                    if (Line[0] == '{' || Line[^1] == '{') Depth++;
                    if (Line[0] == '}' || Line.TrimEnd(',')[^1] == '}') Depth--;
                }
                if (CharacterListCommands.CharNameRX().Match(Line) is Match M && M.Success)
                    CN = M.Value;
                if (MLRX().IsMatch(Line))
                    ML = Line.Split(HsFormat == '{' ? ':' : '=', 2)[1].TrimEnd(';').TrimEnd(',').Trim().Trim('"');
                if (i > 5 && Depth == 1)
                {
                    if (CN is not "" and not "defaultman")
                    {
                        File.WriteAllLines(Path.Combine(HSFolder.FullName, CN + Ext), SplitStat);
                        CnL.Add(CN); MlL.Add(ML);
                    }
                    SplitStat.Clear();
                    CN = "";
                }
            }
            WriteCfgFiles(MlL, CnL);
        }
        /// <summary>
        /// Write roster <see cref="List{string}"/> (<paramref name="CnL"/>) and menulocations <see cref="List{string}"/> (<paramref name="MlL"/>, MUA only) to OHS folders with a generated filename.
        /// </summary>
        public static void WriteCfgFiles(List<string> MlL, List<string> CnL)
        {
            File.WriteAllLines(Path.Combine(OHSpath.CD, CfgSt.GUI.Game, "rosters", $"Roster-{DateTime.Now:yyMMdd-HHmmss}.cfg"), CnL);
            if (CfgSt.GUI.Game == "xml2") { return; }
            File.WriteAllLines(Path.Combine(OHSpath.CD, CfgSt.GUI.Game, "menulocations", $"Menulocations-{DateTime.Now:yyMMdd-HHmmss}.cfg"), MlL);
        }
    }
}
