﻿using OpenHeroSelectGUI.Functions;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Alchemy related static resources
    /// </summary>
    public partial class Alchemy
    {
        [GeneratedRegex(@"ig.*Matrix.*Select", RegexOptions.IgnoreCase)]
        private static partial Regex igSkinRX();

        public static readonly string? Root = Environment.GetEnvironmentVariable("IG_ROOT");
        public static readonly string INI = Path.Combine(Path.GetTempPath(), "OHSGUI_Alchemy.ini");
        public static readonly string? Optimizer = !string.IsNullOrWhiteSpace(Root)
                                                   && Path.Combine(Root, "bin", "sgOptimizer.exe") is string O
                                                   && File.Exists(O) ? O : null;
        /// <summary>
        /// Optimizes the <paramref name="SourceIGB"/> file to a temporary folder, using statistic optimizations, and reads the sgOptimizer output.
        /// </summary>
        /// <returns>A <see cref="string"/> with the stats or <see langword="null"/> if no stats were found or other errors occurred.</returns>
        public static string? GetSkinStats(string? SourceIGB)
        {
            if (!Opt.Write(Opt.GetSkinInfo, SourceIGB)) { return null; }
            string? Stats = Util.RunDosCommnand(Optimizer!, $"\"{SourceIGB}\" \"{Path.Combine(OHSpath.Temp, "temp.igb")}\" \"{INI}\"");
            return string.IsNullOrWhiteSpace(Stats) ? null : Stats;
        }
        /// <summary>
        /// Optimizes the <paramref name="SourceIGB"/> file to a temporary folder, using statistic optimizations, and gets igSkin from the sgOptimizer output.
        /// </summary>
        /// <returns>The igSkin name or <see langword="null"/> if not found or failed.</returns>
        public static string? GetIntName(string? SourceIGB)
        {
            return GetSkinStats(SourceIGB) is string Stats
                ? IntName(Stats.Split(Environment.NewLine))
                : null;
        }
        /// <summary>
        /// Searches for the igSkin name in Skin <paramref name="Stats"/>.
        /// </summary>
        /// <returns>The igSkin name or <see langword="null"/> if not found.</returns>
        public static string? IntName(string[] Stats)
        {
            string? igSkin = Stats.FirstOrDefault(s => igSkinRX().IsMatch(s)) is string igSkinLine && igSkinLine.Contains('|')
                ? igSkinLine.Split('|')[0].Trim()
                : null;
            return string.IsNullOrWhiteSpace(igSkin) ? null : igSkin;
        }
        /// <summary>
        /// If Alchemy works, optimize <paramref name="SourceIGB"/>:
        /// Change igSkin from <paramref name="IntName"/> to <paramref name="Name"/>, add GlobalColor according to <paramref name="AlchemyCompat"/> and convert to Geo 2 if <paramref name="ConvGeo"/>.
        /// Write to <paramref name="TargetIGB"/>. Both IGB paths must be checked for existence beforehand.
        /// </summary>
        /// <returns><see langword="True"/>, if optimized successfully, otherwise <see langword="false"/>.</returns>
        public static bool CopySkin(FileInfo SourceIGB, string TargetIGB, string Name, int AlchemyCompat, bool ConvGeo, string? igSkin = null, bool HexEdit = true)
        {
            return AlchemyCompat >= 8
                && Optimizer is not null
                && SourceIGB.Exists
                && Opt.Skins(Name, AlchemyCompat, ConvGeo, HexEdit, igSkin)
                && Util.RunExeInCmd(Optimizer, $"\"{SourceIGB}\" \"{TargetIGB}\" \"{INI}\"");
        }
    }
    /// <summary>
    /// Alchemy 5 optimizations for sgOptimizer.exe
    /// </summary>
    public class Opt
    {
        public static readonly string[] GetSkinInfo = [.. Head(3), .. StatTex(1), .. StatGeo(2), .. StatSkin(3)];
        public static string[] Head(int N) =>
            [
                "[OPTIMIZE]",
                $"optimizationCount = {N}",
                "hierarchyCheck = true"
            ];
        public static string[] StatTex(int N) =>
            [
                $"[OPTIMIZATION{N}]",
                "name = igStatisticsTexture",
                .. Stats("0x00000117"),
                "useFullPath = false"
            ];
        private static string[] StatGeo(int N) =>
            [
                $"[OPTIMIZATION{N}]",
                "name = igStatisticsGeometry",
                .. Stats("0x00500000")
            ];
        private static string[] StatSkin(int N) =>
            [
                $"[OPTIMIZATION{N}]",
                "name = igStatisticsSkin",
                .. Stats("0x00000006")
            ];
        private static string[] Stats(string Mask) =>
            [
                "separatorString = |",
                "columnMaxWidth = -1",
                $"showColumnsMask = {Mask}",
                "sortColumn = -1"
            ];
        public static string[] Rename(int N, string SourceName, string NewName) =>
            [
                $"[OPTIMIZATION{N}]",
                "name = igChangeObjectName",
                "objectTypeName = igNamedObject",
                $"targetName = ^{SourceName}$",
                $"newName = {NewName}"
            ];
        public static string[] GGC(int N) =>
            [
                $"[OPTIMIZATION{N}]",
                "name = igGenerateGlobalColor"
            ];
        public static string[] CGA(int N) =>
            [
                $"[OPTIMIZATION{N}]",
                "name = igConvertGeometryAttr",
                "accessMode = 3",
                "storeBoundingVolume = false"
            ];
        /// <summary>
        /// Create and write (if any) optimization set to change <paramref name="igSkin"/> to <paramref name="Name"/>, optimize according to <paramref name="AlchemyCompat"/>ibility and convert to Geo 2 if <paramref name="ConvGeo"/>.
        /// </summary>
        /// <returns><see langword="True" />, if any optimizations necessary or possible, otherwise <see langword="false"/>.</returns>
        public static bool Skins(string Name, int AlchemyCompat, bool ConvGeo, bool HexEdit, string? igSkin)
        {
            string[] Op = [];
            int i = 0;
            if (ConvGeo)  // convert to attr2
            {
                i++;
                Op = [.. Op, .. CGA(i)];
            }
            if (AlchemyCompat == 9)  // (there are issues sometimes if gc is already applied)
            {
                i++;
                Op = [.. Op, .. GGC(i)];
            }
            if (!(!HexEdit || string.IsNullOrWhiteSpace(igSkin) || igSkin == Name || igSkin.StartsWith("Bip01")))
            {
                i++;
                Op = [.. Op, .. Rename(i, igSkin, Name)];
            }
            if (i > 0)
            {
                File.WriteAllLines(Alchemy.INI, [.. Head(i), .. Op]);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Tries to write the <paramref name="Opts"/> to <see cref="Alchemy.INI"/>, if <paramref name="SourceIGB"/> exists.
        /// </summary>
        /// <returns><see langword="True"/>, if written successfully and <paramref name="SourceIGB"/> exists, otherwise <see langword="false"/>.</returns>
        public static bool Write(string[] Opts, string? SourceIGB)
        {
            if (Alchemy.Optimizer is null || !File.Exists(SourceIGB)) { return false; }
            try { File.WriteAllLines(Alchemy.INI, Opts); } catch { return false; }
            return true;
        }
    }
}
