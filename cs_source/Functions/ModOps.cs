using OpenHeroSelectGUI.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenHeroSelectGUI.Functions
{
    internal class ModOps
    {
        /// <summary>
        /// Renumber a <paramref name="Mod"/> to new number <paramref name="NN"/>. If mulitple mods (herostats) found, will currently do nothing and return empty message.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> containing error messages as returned from herostat renumber, files msg., or <see langword="null"/> if no error.</returns>
        public static List<string?> Renumber(string Mod, string NN)
        {
            // Optionally, check if it's a mod or folder (continue with variable Original instead of Mod):
            //if (!string.IsNullOrEmpty(Mod)
            //    && (File.GetAttributes(Mod).HasFlag(FileAttributes.Directory) ? Mod : Util.Run7z(Mod, ON)) is string Original)
            List<string?> Messages = [];
            foreach (DirectoryInfo Source in OHSpath.GetModSource(Mod))
            {
                List<FileInfo> Chars = Herostat.GetFiles(Source).ToList();
                //if (Chars.Count > 1) { show selection; }
                //else
                if (Chars.Count == 1) { Messages.Add(Renumber(Source.FullName, Source.Name, "", NN, Chars[0])); } // Installs mods, but should prly not
                else if (Chars.Count == 0
                    && Source.GetDirectories("actors") is DirectoryInfo[] actors
                    && actors.Length > 0
                    && actors[0].EnumerateFiles("*.igb").Where(n => Path.GetFileNameWithoutExtension(n.Name).All(c => c is >= '0' and <= '9')) is IEnumerable<FileInfo> Skins
                    && Skins.FirstOrDefault() is FileInfo Skin) // Taking the first skin found, which might not be using the actual mod number
                {
                    string FS = Path.GetFileNameWithoutExtension(Skin.Name);
                    // This method currently doesn't install the renumbered mod.
                    Messages.Add(Renumber(Source, FS[..^2], NN,
                        Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(OHSpath.Packages(Source.FullName), $"*{FS}.pkgb").FirstOrDefault()) is string PN ? PN[..PN.LastIndexOf('_')] : "",
                        Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(Path.Combine(Source.FullName, "data", "powerstyles"), $"*.*").FirstOrDefault("")),
                        Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(Path.Combine(Source.FullName, "actors"), $"{FS[..^2]}_*").FirstOrDefault("")).Replace("_4_combat", ""),
                        Skins.Select(s => Path.GetFileNameWithoutExtension(s.Name)).Where(s => s[..^2] == FS[..^2]).ToArray())
                        ? null
                        : "Associated files not found.");
                }
                else { Messages.Add("Mod Renumbering failed."); }
            }
            // Number not detected, failed
            // WIP: Not ready to handle error messages.
            return Messages;
        }
        /// <summary>
        /// Renumber a <paramref name="Mod"/>, based on herostat (<paramref name="HF"/>) information from <paramref name="ON"/> (can be "") to <paramref name="NN"/>. Installs to <paramref name="NewName"/>.
        /// </summary>
        /// <returns>An error message <see cref="string"/> as returned from herostat renumber, or <see langword="null"/> if no error.</returns>
        public static string? Renumber(string Mod, string NewName, string ON, string NN, FileInfo HF)
        {
            object Res = Renumber(Mod, ON, NN, HF);
            try
            {
                DirectoryInfo Clone = (DirectoryInfo)Res;
                return OHSpath.GetModTarget(Clone, NewName, Mod) is string Inst
                    && OHSpath.CopyFilesRecursively(Clone, Inst)
                    ? null
                    : "Mod installation failed.";
            }
            catch
            {
                return (string)Res;
            }
        }
        /// <summary>
        /// Renumber a <paramref name="Mod"/>, based on herostat (<paramref name="HF"/>) information from <paramref name="ON"/> (can be "") to <paramref name="NN"/>.
        /// </summary>
        /// <returns>The mod's <see cref="DirectoryInfo"/> if it was found and no errors found, otherwise an error message <see cref="string"/>.</returns>
        public static object Renumber(string Mod, string ON, string NN, FileInfo HF)
        {
            string HS = File.ReadAllText(HF.FullName);
            string[] HSA = [.. HS.Split(Environment.NewLine)];

            string[] skins = new string[6];
            skins[0] = Herostat.RootAttribute(HSA, "skin");
            for (int n = 2; n < 7; n++) { if (Herostat.RootAttribute(HSA, $"skin_0{n}") is string SN && SN != string.Empty) { skins[n - 1] = ON + SN; } }
            string ca = Herostat.RootAttribute(HSA, "characteranims");
            string HN = skins[0][..^2];
            if (HN != ca[..ca.IndexOf('_')]) { return "Herostat numbers don't match."; }
            if (ON == "") { ON = HN; } else if (ON != HN) { return $"Herostat numbers don't match '{ON}'."; }

            Herostat.Clone(HS, HF, ON, NN);

            foreach (DirectoryInfo Source in OHSpath.GetModSource(Mod))
            {
                if (Renumber(Source, ON, NN, Herostat.RootAttribute(HSA, "name"), Herostat.RootAttribute(HSA, "powerstyle"), ca, skins))
                {
                    return Source;
                }
            }
            return "Associated files not found.";
        }
        /// <summary>
        /// Renumber mod files in <paramref name="Source"/> from <paramref name="ON"/> to <paramref name="NN"/>, using <paramref name="name"/>, <paramref name="skins"/>, <paramref name="ca"/>, and <paramref name="ps"/> information.
        /// </summary>
        /// <returns><see langword="True"/> if skins[0] found and no exception thrown, otherwise <see langword="false"/>.</returns>
        private static bool Renumber(DirectoryInfo Source, string ON, string NN, string name, string ps, string ca, string[] skins)
        {
            if (File.Exists(Path.Combine(Source.FullName, "actors", $"{skins[0]}.igb")))
            {
                try
                {
                    string loading = Path.Combine(Source.FullName, "textures", "loading");
                    List<string> FL = Directory.Exists(loading)
                        ? [.. Directory.EnumerateFiles(loading, $"{ON}*.igb")]
                        : [];
                    FL.Add(Path.Combine(Source.FullName, "ui", "models", "mannequin", $"{ON}01.igb"));
                    FL.Add(Path.Combine(Source.FullName, "actors", $"{ca}.igb"));
                    FL.Add(Path.Combine(Source.FullName, "actors", $"{ca}_4_combat.igb"));
                    for (int f = 0; f < FL.Count; f++)
                    {
                        FileInfo IGB = new(FL[f]);
                        if (IGB.Exists) { IGB.MoveTo(Path.Combine(IGB.DirectoryName!, $"{NN}{IGB.Name[ON.Length..]}"), true); }
                    }
                    for (int n = 0; n < 6; n++)
                    {
                        if (skins[n] is string F)
                        {
                            FileInfo S = new(Path.Combine(Source.FullName, "actors", $"{F}.igb"));
                            if (Alchemy.CopySkin(S, Path.Combine(S.DirectoryName!, $"{NN}{F[^2..]}.igb"), $"{NN}{F[^2..]}", 9, false, Alchemy.GetIntName(S.FullName)))
                            { S.Delete(); } // Not testing skin properties or platform. Just for PC 2006.
                            FileInfo HUD = new(Path.Combine(Source.FullName, "hud", $"hud_head_{F}.igb"));
                            if (HUD.Exists) { HUD.MoveTo(Path.Combine(HUD.DirectoryName!, $"hud_head_{NN}{F[^2..]}.igb"), true); }
                            FileInfo PKG = new(OHSpath.Packages(Source.FullName, $"{name}_{F}.pkgb"));
                            if (PKG.Exists) { _ = MarvelModsXML.ClonePackage(PKG.FullName, Path.Combine(PKG.DirectoryName!, $"{name}_{NN}{F[^2..]}"), ON, NN, F[^2..]); }
                        }
                    }
                    _ = MarvelModsXML.ReplaceRef(Path.Combine(Source.FullName, "data", "powerstyles", $"{ps}{Path.GetExtension(CfgSt.OHS.HerostatName)}"), skins[0..5], NN);
                    return true;
                }
                catch { return false; }
            }
            return false;
        }
    }
}
