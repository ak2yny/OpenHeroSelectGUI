using OpenHeroSelectGUI.Settings;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenHeroSelectGUI.Functions
{
    // WIP: Performance is on the slower side. Optimizations: Less DirectoryInfo/FileInfo creations, less string manipulations.
    public static class ModOps
    {
        public readonly record struct Result(
            bool NoError,      // No message (level 0)   (level 1) if not 0 or 1 or -1
            bool Critical,     // "Herostat" file errors (level 2)
            string? NewNumber, // (level 0 or 1)
            string? Message    // Error or Warning (level 1 or 2)
        )
        {
            public static Result Cancel() =>
                new(true, true, null, null);
            public static Result Failure(string error) =>
                new(false, true, null, error);
            public static Result Success(string nnp, string? warning = null) =>
                new(warning is null, false, nnp, warning);
        }
        /// <summary>
        /// Renumber a <paramref name="Mod"/> to new number <paramref name="NN"/>. If mulitple mods (herostats) found, will currently do nothing and return empty message.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> containing error messages as returned from herostat renumber, files msg., or an empty list if no error.</returns>
        /*
        public static List<string?> Renumber(string Mod, string NN)
        {
            // Optionally, check if it's a mod or folder (continue with variable Original instead of Mod):
            //if (!string.IsNullOrEmpty(Mod)
            //    && (File.GetAttributes(Mod).HasFlag(FileAttributes.Directory) ? Mod : Util.Run7z(Mod, ON)) is string Original)
            List<string?> Messages = [];
            foreach (DirectoryInfo Source in OHSpath.GetModSource(Mod))
            {
                List<FileInfo> Chars = [.. Herostat.GetFiles(Source)];
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
                        [.. Skins.Select(s => Path.GetFileNameWithoutExtension(s.Name)).Where(s => s[..^2] == FS[..^2])])
                        ? null
                        : "Associated files not found.");
                }
                else { Messages.Add("Mod Renumbering failed."); }
            }
            // Number not detected, failed
            // WIP: Not ready to handle error messages.
            return Messages;
        }
        */
        /// <summary>
        /// Renumber a <paramref name="Mod"/>, based on herostat (<paramref name="HF"/>) information from <paramref name="ON"/> (can be "") to <paramref name="NN"/>. Installs to <paramref name="NewName"/>.
        /// </summary>
        /// <returns>An error message <see cref="string"/> as returned from herostat renumber, if any; otherwise <see langword="null"/>.</returns>
        public static Result Renumber(string Mod, string NewName, string ON, string NN, string HF)
        {
            Result Res = Renumber(Mod, ON, NN, HF);
            if (!Res.NoError) { return Res; }
            try
            {
                string Source = Res.NewNumber!;
                return OHSpath.GetModTarget(Source, NewName, Mod) is string Inst
                    && OHSpath.CopyFilesRecursively(Source, Inst)
                    ? Result.Success(NN)
                    : Result.Success(NN, "Mod installation failed.");
            }
            catch (System.Exception ex)
            {
                return Result.Success(NN, $"Mod installation failed.\n{ex}");
            }
        }
        /// <summary>
        /// Renumber a <paramref name="Mod"/>, based on herostat (<paramref name="HF"/>) information from <paramref name="ON"/> (can be "") to <paramref name="NN"/>.
        /// </summary>
        /// <returns>The mod's <see cref="DirectoryInfo"/> if it was found and no errors found, otherwise an error message <see cref="string"/>.</returns>
        private static Result Renumber(string Mod, string ON, string NN, string HF)
        {
            Stats HS = new(HF);

            string[] skins = new string[6];
            skins[0] = HS.RootAttribute("skin");
            for (int n = 2; n < 7; n++) { if (HS.RootAttribute($"skin_0{n}") is string SN && SN != string.Empty) { skins[n - 1] = ON + SN; } }
            string ca = HS.RootAttribute("characteranims");
            string HN = skins[0][..^2];
            if (HN != ca[..ca.IndexOf('_')]) { return Result.Failure("Herostat numbers don't match."); }
            if (ON == "") { ON = HN; } else if (ON != HN) { return Result.Failure($"Herostat numbers don't match '{ON}'."); }
            string name = HS.RootAttribute("name");
            string ps = HS.RootAttribute("powerstyle");

            HS.Clone(ON, NN);

            foreach (string Source in OHSpath.GetModSource(Mod))
            {
                if (Renumber(Source, ON, NN, name, ps, ca, skins))
                {
                    return Result.Success(Source);
                }
            }
            return Result.Success(NN, "Associated files not found.");
        }
        /// <summary>
        /// Renumber mod files in <paramref name="Source"/> from <paramref name="ON"/> to <paramref name="NN"/>, using <paramref name="name"/>, <paramref name="skins"/>, <paramref name="ca"/>, and <paramref name="ps"/> information.
        /// </summary>
        /// <returns><see langword="True"/> if skins[0] found and no exception thrown, otherwise <see langword="false"/>.</returns>
        private static bool Renumber(string Source, string ON, string NN, string name, string ps, string ca, string[] skins)
        {
            if (File.Exists($"{Source}/actors/{skins[0]}.igb"))
            {
                try
                {
                    string loading = Path.Combine(Source, "textures", "loading");
                    foreach (string f in
                        (string[])[.. Directory.Exists(loading) ? Directory.EnumerateFiles(loading, $"{ON}*.igb") : [],
                        Path.Combine(Source, "ui", "models", "mannequin", $"{ON}01.igb"),
                        Path.Combine(Source, "actors", $"{ca}.igb"),
                        Path.Combine(Source, "actors", $"{ca}_4_combat.igb")])
                    {
                        if (File.Exists(f)) { File.Move(f, Path.Combine(Path.GetDirectoryName(f)!, $"{NN}{Path.GetFileName(f)[ON.Length..]}"), true); }
                    }
                    for (int n = 0; n < 6; n++)
                    {
                        if (skins[n] is string F)
                        {
                            FileInfo S = new(Path.Combine(Source, "actors", $"{F}.igb"));
                            if (Alchemy.CopySkin(S, Path.Combine(S.DirectoryName!, $"{NN}{F[^2..]}.igb"), $"{NN}{F[^2..]}", 9, false, Alchemy.GetIntName(S.FullName)))
                            { S.Delete(); } // Not testing skin properties or platform. Just for PC 2006.
                            string HUD = Path.Combine(Source, "hud", $"hud_head_{F}.igb");
                            if (File.Exists(HUD)) { File.Move(HUD, Path.Combine(Path.GetDirectoryName(HUD)!, $"hud_head_{NN}{F[^2..]}.igb"), true); }
                            string PKG = OHSpath.Packages(Source, $"{name}_{F}.pkgb");
                            if (File.Exists(PKG)) { _ = MarvelModsXML.ClonePackage(PKG, Path.Combine(Path.GetDirectoryName(PKG)!, $"{name}_{NN}{F[^2..]}"), ON, NN, F[^2..]); }
                        }
                    }
                    _ = MarvelModsXML.ReplaceRef(Path.Combine(Source, "data", "powerstyles", $"{ps}{Path.GetExtension(CfgSt.OHS.HerostatName)}"), skins[0..5], NN);
                    return true;
                }
                catch { return false; }
            }
            return false;
        }
    }
}
