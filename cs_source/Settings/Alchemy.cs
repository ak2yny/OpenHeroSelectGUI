using System;
using System.IO;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Alchemy related static resources
    /// </summary>
    internal static class Alchemy
    {
        public static readonly string? Root = Environment.GetEnvironmentVariable("IG_ROOT");
        public static readonly string INI = Path.Combine(Path.GetTempPath(), "OHSGUI_Alchemy.ini");
        public static readonly string? Optimizer = !string.IsNullOrWhiteSpace(Root)
                                                   && Path.Combine(Root, "bin", "sgOptimizer.exe") is string O
                                                   && File.Exists(O) ? O : null;
    }
    /// <summary>
    /// Alchemy 5 optimizations for sgOptimizer.exe
    /// </summary>
    internal static class Opt
    {
        public static readonly string[] GetSkinInfo = [.. Head(3), .. StatTex(1), .. StatGeo(2), .. StatSkin(3)];
        public static string[] Head(int N) =>
            [
                "[OPTIMIZE]",
                $"optimizationCount = {N}",
                "hierarchyCheck = true"
            ];
        private static string[] StatTex(int N) =>
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
    }
}
