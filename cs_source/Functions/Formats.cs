using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Storage;

namespace OpenHeroSelectGUI.Functions
{
    internal static class Formats
    {
        /// <summary>
        /// Search a fake XML or JSON file as <paramref name="array"/> for an attribute <paramref name="name"/>. Ignores parent/child relations. Treats nodes as attributes but returns empty.
        /// </summary>
        /// <param name="array">A <see langword="string[]" /> of lines read from a fake XML or JSON file</param>
        /// <param name="name">The name of the attribute</param>
        /// <returns>The value of the first matching attribute or <see cref="string.Empty"/> if not found</returns>
        public static string GetAttr(string[] array, string name)
        {
            string[] Lines = Array.FindAll(array, c => new Regex($"^\"?{name}(\": | =)").IsMatch(c.Trim().ToLower()));
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
        [GeneratedRegex(@"^""?menulocation(\"": | = )", RegexOptions.IgnoreCase)]
        private static partial Regex MLRX();

        [GeneratedRegex(@"(?<=charactername[=:""\s]*)[^=:""\s][^;=""\n]+[^;=""\s]", RegexOptions.IgnoreCase)]
        public static partial Regex CharNameRX();

        private static readonly string[] HSexts = [".txt", ".xml", ".json"];

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
        /// Get the full path to the herostat file, providing the <paramref name="HsPath"/> relative in the herostat folder.
        /// </summary>
        /// <returns><see cref="FileInfo"/> of the first herostat found or <see langword="null"/> if not found</returns>
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
        /// Find herostats in a <paramref name="ModFolder"/>.
        /// </summary>
        /// <returns><see cref="IEnumerable{FileInfo}"/> of all <see cref="FileInfo"/> that match a herostat name or content.</returns>
        public static IEnumerable<FileInfo> GetFile(DirectoryInfo ModFolder)
        {
            return ModFolder.EnumerateFiles("*.*")
                .Where(h => HSexts.Contains(h.Extension.ToLower())
                    && (h.Name.Contains("herostat", StringComparison.OrdinalIgnoreCase)
                    || File.ReadAllText(h.FullName).Contains("stats", StringComparison.OrdinalIgnoreCase)));
        }
        /// <summary>
        /// Find herostats in a <paramref name="ModFolder"/>.
        /// </summary>
        /// <returns><see cref="IEnumerable{FileInfo}"/> of all <see cref="FileInfo"/> that match a herostat name or content.</returns>
        public static IEnumerable<FileInfo> GetFiles(DirectoryInfo ModFolder)
        {
            return GetFile(ModFolder) is IEnumerable<FileInfo> Hs && Hs.Any()
                ? Hs
                : ModFolder.EnumerateDirectories("data").FirstOrDefault() is DirectoryInfo D
                && GetFile(D) is IEnumerable<FileInfo> HsD && HsD.Any()
                ? HsD
                : ModFolder.Parent is DirectoryInfo P
                ? GetFile(P)
                : [];
        }
        /// <summary>
        /// Gets the internal name from the currently selected character
        /// </summary>
        /// <returns>The internal name or <see cref="string.Empty"/></returns>
        public static string GetInternalName() => GetInternalName(CfgSt.Var.FloatingCharacter);
        /// <summary>
        /// Gets the internal name from an available character (path as in roster file)
        /// </summary>
        /// <returns>The internal name of <paramref name="AvailableChar"/> or <see cref="string.Empty"/></returns>
        public static string GetInternalName(string? AvailableChar)
        {
            return Load(AvailableChar) is string[] HS ? RootAttribute(HS, "name") : string.Empty;
        }
        /// <summary>
        /// Get a herostat root attribute by providing a herostat <see langword="string[]"/>, regardless of the format.
        /// </summary>
        /// <returns>The value of the attribute or <see cref="string.Empty"/> if not found</returns>
        public static string RootAttribute(string[] Herostat, string AttrName)
        {
            return Herostat[0].Trim()[0] == '<' ? GUIXML.GetRootAttribute(Herostat, AttrName) : Formats.GetAttr(Herostat, AttrName);
        }
        /// <summary>
        /// Adds a <paramref name="Herostat"/> from an existing <see cref="StorageFile"/> to the available characters.
        /// </summary>
        /// <returns><see langword="True"/>, if no exceptions occur, otherwise <see langword="False"/>.</returns>
        public static bool Add(StorageFile Herostat) => Add(Herostat.Path, Herostat.Name, Herostat.FileType);
        /// <summary>
        /// Reads a herostat file from the provided <paramref name="HSpath"/> (file must exist) and copies the file to the available characters with <paramref name="HSext"/> extension, using the charactername found or <paramref name="HSname"/>.
        /// </summary>
        /// <returns><see langword="True"/>, if no exceptions occur, otherwise <see langword="False"/>.</returns>
        public static bool Add(string HSpath, string HSname, string HSext)
        {
            try
            {
                string? Name = File.ReadAllLines(HSpath)
                    .FirstOrDefault(l => l.Contains("charactername", StringComparison.OrdinalIgnoreCase)) is string CharLine
                    && CharNameRX().Match(CharLine) is Match M && M.Success
                    ? M.Value
                    : Path.GetFileNameWithoutExtension(HSname);
                File.Copy(HSpath, OHSpath.GetVacant(Path.Combine(OHSpath.HsFolder, Name), HSext), true);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Clone a herostat (<paramref name="HF"/>), using its content <paramref name="HS"/>, and changing the number from <paramref name="ON"/> to <paramref name="NN"/>.
        /// </summary>
        public static void Clone(string HS, FileInfo HF, string ON, string NN)
        {
            string New = OHSpath.GetVacant($"{HF.FullName[..^HF.Extension.Length]} {NN}", HF.Extension);
            File.WriteAllText(New, Regex.Replace(HS, $@"((characteranims|skin)[=:""\s]+){ON}(_|\d\d)", "${1}" + NN + "${3}"));
            CfgSt.Var.FloatingCharacter = Path.GetRelativePath(OHSpath.HsFolder, New)[..^HF.Extension.Length].Replace("\\", "/");
        }
        /// <summary>
        /// Splits <paramref name="Herostat"/> <see langword="string[]"/>, based on depth count by curly brackets. Saves them in <paramref name="HsFormat"/> to the <paramref name="OutputFolder"/>, if they have the charactername line. Crashes on errors.
        /// </summary>
        public static void Split(string[] Herostat, char HsFormat, string OutputFolder)
        {
            int Depth = HsFormat == '{' ? -1 : 0;
            List<string> SplitStat = [], MlL = [], CnL = [];
            string CN = "", ML = "";
            string Ext = HsFormat == '{' ? ".json" : ".txt";
            for (int i = 0; i < Herostat.Length; i++)
            {
                string Line = Herostat[i].Trim();
                if (Depth < 1
                    && Line.Length > 0
                    && Line[^1] == '{'
                    && Line.TrimStart('"').StartsWith("stats", StringComparison.OrdinalIgnoreCase))
                { Depth++; }
                if (Depth > 1 || (Depth == 1 && Line.Length > 0 && Line[^1] is '{' or '}'))
                {
                    SplitStat.Add(Herostat[i]);
                }
                if (Line.Length > 0)
                {
                    if (Line[0] == '{' || Line[^1] == '{') Depth++;
                    if (Line[0] == '}' || Line.TrimEnd(',')[^1] == '}') Depth--;
                }
                if (CharNameRX().Match(Line) is Match M && M.Success) { CN = M.Value; }
                else if (MLRX().IsMatch(Line)) { ML = Line.Split(HsFormat == '{' ? ':' : '=', 2)[1].TrimEnd(';').TrimEnd(',').Trim().Trim('"'); }
                if (i > 5 && Depth == 1)
                {
                    if (CN is not "" and not "defaultman")
                    {
                        if (File.Exists(Path.Combine(OutputFolder, CN + Ext)))
                        {
                            CN = Path.GetFileNameWithoutExtension(OHSpath.GetVacant(Path.Combine(OutputFolder, $"{CN} ({Formats.GetAttr([.. SplitStat], "name")})"), Ext));
                        }
                        File.WriteAllLines(Path.Combine(OutputFolder, CN + Ext), SplitStat);
                        CnL.Add(CN); MlL.Add(ML);
                    }
                    SplitStat.Clear();
                    CN = "";
                }
            }
            WriteCfgFiles(MlL, CnL);
        }
        /// <summary>
        /// Write roster <see cref="List{string}"/> (<paramref name="CnL"/>) and menulocations <see cref="List{string}"/> (<paramref name="MlL"/>, MUA only) to OHS folders with a generated filename. Throws WriteAllLines exceptions.
        /// </summary>
        public static void WriteCfgFiles(List<string> MlL, List<string> CnL)
        {
            string Name = $"SplitStats-{DateTime.Now:yyMMdd-HHmmss}.cfg";
            File.WriteAllLines(Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", Name), CnL);
            if (CfgSt.GUI.Game == "XML2") { return; }
            File.WriteAllLines(Path.Combine(OHSpath.CD, OHSpath.Game, "menulocations", Name), MlL);
        }
    }
}
