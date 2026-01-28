using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace OpenHeroSelectGUI.Functions
{
    public static partial class Herostat
    {
        [GeneratedRegex(@"(?<=^\s*""?menulocation(?:"":\s*""|\s*=\s*))\S.+?(?="",\s*$|\s*;?\s*$)", RegexOptions.IgnoreCase)]
        private static partial Regex MLRX();

        [GeneratedRegex(@"(?<=^\s*""?charactername(?:"":\s*""|\s*=\s*))\S.+?(?="",\s*$|\s*;?\s*$)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        public static partial Regex CharNameRX(); // Doesn't accept quotes in values of fake XML.

        [GeneratedRegex(@"(?<=^\s*""?name(?:"":\s*""|\s*=\s*))\S.+?(?="",\s*$|\s*;?\s*$)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        public static partial Regex NameRX();

        /// <summary>
        /// Gets the internal name from the currently selected character.
        /// </summary>
        /// <returns>The internal name from the herostat file, if <see cref="InternalObservables.FloatingCharacter"/> is not <see langword="null"/> and if no exceptions occurred; otherwise <see cref="string.Empty"/>.</returns>
        public static string GetInternalName()
        {
            try { return GetStats(CfgSt.Var.FloatingCharacter!).InternalName; } catch { return string.Empty; }
        }
        /// <summary>
        /// Gets the <see cref="Stats"/> for the relative <paramref name="HerostatPath"/> (Exceptions).
        /// </summary>
        internal static Stats GetStats(string HerostatPath) => new(OHSpath.GetHsFile(HerostatPath));
        /// <summary>
        /// Reads a herostat file from the provided <paramref name="FullName"/> path (file must exist) and copies the file to the available characters using <paramref name="Extension"/>, using the character<paramref name="Name"/> if found or the file name without extension.
        /// </summary>
        /// <returns><see langword="True"/>, if no exceptions occur; otherwise <see langword="false"/>.</returns>
        public static bool Add(string FullName, string Extension, string Name)
        {
            try
            {
                if (Name == "") { Name = Path.GetFileNameWithoutExtension(FullName); }
                string NewPath = OHSpath.GetVacant(Path.Combine(OHSpath.HsFolder, Name), Extension);
                File.Copy(FullName, NewPath, true);
                CfgSt.Roster.AddAvailable(NewPath[(OHSpath.HsFolder.Length + 1)..^Extension.Length]);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Splits <paramref name="Herostat"/> array, based on depth count by curly brackets. Saves them in <paramref name="HsFormat"/> to the <paramref name="OutputFolder"/>, if they have the charactername line.
        /// </summary>
        /// <remarks>Exceptions: System.IO (WriteAllLines); Various (unlikely)</remarks>
        public static void Split(string[] Herostat, bool IsJson, string OutputFolder)
        {
            int Depth = IsJson ? -1 : 0;
            List<string> SplitStat = [], MlL = [], CnL = [];
            string CN = "", ML = "";
            string Ext = IsJson ? ".json" : ".txt";
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
                    if (Line[0] == '{' || Line[^1] == '{') { Depth++; }
                    else if (Line[0] == '}' || Line[^(Line[^1] == ',' ? 2 : 1)] == '}') { Depth--; }
                    else if (CharNameRX().Match(Line) is Match M && M.Success) { CN = M.Value; }
                    else if (MLRX().Match(Line) is Match L && L.Success) { ML = L.Value; }
                }
                if (i > 5 && Depth == 1)
                {
                    if (CN is not "" and not "defaultman")
                    {
                        string OutputFile = Path.Combine(OutputFolder, CN + Ext);
                        if (File.Exists(OutputFile))
                        {
                            OutputFile = OHSpath.GetVacant(Path.Combine(OutputFolder,
                                $"{CN} ({NameRX().Match(string.Join(Environment.NewLine, SplitStat)).Value})"), Ext);
                        }
                        File.WriteAllLines(OutputFile, SplitStat);
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
        /// <remarks>Exceptions: System.IO (WriteAllLines)</remarks>
        public static void WriteCfgFiles(List<string> MlL, List<string> CnL)
        {
            string Name = $"SplitStats-{DateTime.Now:yyMMdd-HHmmss}.cfg";
            File.WriteAllLines(Path.Combine(OHSpath.CD, OHSpath.Game, "rosters", Name), CnL);
            if (CfgSt.GUI.IsXml2) { return; }
            File.WriteAllLines(Path.Combine(OHSpath.CD, OHSpath.Game, "menulocations", Name), MlL);
        }
    }
}
