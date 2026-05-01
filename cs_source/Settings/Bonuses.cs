using Microsoft.UI.Xaml.Input;
using OpenHeroSelectGUI.Functions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Serialization;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Team Bonus details. Also contains a list (<see cref="ObservableCollection{T}"/>) of <see cref="Hero"/>es.
    /// </summary>
    public class Bonus
    {

        [XmlIgnore]
        public string Descbonus { get; set; } = "+5% Damage";

        [XmlAttribute("descbonus")]
        public string DescbonusEscaped
        {
            get => Descbonus.Replace("%", "%%");
            set => Descbonus = value.Replace("%%", "%");
        }

        [XmlAttribute("descname1")]
        public string Descname1 { get; set; } = "";

        [XmlAttribute("descname2")]
        public string? Descname2 { get; set; }

        [XmlIgnore]
        public string? Name { get; set; }

        [XmlAttribute("powerup")]
        public string? Powerup { get; set; }

        [XmlAttribute("skinset")]
        public string? Skinset { get; set; }

        [XmlAttribute("sound")]
        public string Sound { get; set; } = "common/team_bonus_";

        [XmlElement("hero")]
        public ObservableCollection<Hero>? Members { get; set; }

        [XmlIgnore]
        public StandardUICommand? Command;

        /// <summary>
        /// Ignore the <see cref="Members"/> collection if it's null or empty, or if the <see cref="Skinset"/> is specified.
        /// </summary>
        public bool ShouldSerializeMembers()
        {
            return Members != null && Members.Count > 0 && string.IsNullOrWhiteSpace(Skinset);
        }
        /// <summary>
        /// Called after! deserialization to set the <see cref="Name"/> UI property from the XML attributes, add the <paramref name="DeleteCommand"/> and initialize <see cref="Members"/> if the XML didn't specify any.
        /// </summary>
        public void OnDeserialized(StandardUICommand? DeleteCommand)
        {
            Name = Descname2 is null ? Descname1 : $"{Descname1} {Descname2}";
            Members ??= [];
            Command = DeleteCommand;
        }
        /// <summary>
        /// Called before! serialization to split the <see cref="Name"/> to the XML attributes and update <see cref="Powerup"/> based on <paramref name="PU"/>.
        /// </summary>
        public void OnSerializing(Dictionary<string, string> PU)
        {
            // Age of Apocalypse is split differently than original
            if (Name is not null && Name.Length > 8
                && Name.Split(' ', StringSplitOptions.RemoveEmptyEntries) is string[] Words
                && Words.Length > 1)
            {
                string NormalizedName = string.Join(' ', Words);
                int ni = NormalizedName.Length;
                for (int il = NormalizedName.Length / 2, cl = Words[0].Length + 1, wi = 1;
                    wi < Words.Length && Math.Abs(il - cl) < Math.Abs(il - ni);
                    cl += Words[wi].Length + 1, wi++)
                {
                    ni = cl;
                }
                Descname1 = NormalizedName[..(ni - 1)];
                Descname2 = NormalizedName[ni..];
            }
            else
            {
                Descname1 = Name ?? "";
                Descname2 = null;
            }
            Powerup = PU[Descbonus];
        }
    }

    public class Hero
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlAttribute("skin")]
        public string? Skin { get; set; }
    }

    [XmlRoot("bonuses")]
    public class Bonuses
    {
        [XmlElement("bonus")]
        public List<Bonus> Teams { get; set; } = [];
    }

    internal static class BonusSerializer
    {
        /// <summary>
        /// Team bonus powerups with description for use in MUA team_bonus files
        /// </summary>
        internal static readonly Dictionary<string, string> Powerups = new()
        {
            ["5% Damage Inflicted as Health Gain"] = "shared_team_damage_to_health",
            ["20 Energy Per Knockout"] = "shared_team_energy_per_kill",
            ["+60% S.H.I.E.L.D. Credit Drops"] = "shared_team_extra_money",
            ["20 Health Per Knockout"] = "shared_team_health_per_kill",
            ["+5 Health Regeneration"] = "shared_team_health_regen",
            ["+5% Criticals"] = "shared_team_increase_criticals",
            ["+5% Damage"] = "shared_team_increase_damage",
            ["+5 All Resistances"] = "shared_team_increase_resistances",
            ["+15 Strike"] = "shared_team_increase_striking",
            ["+6 Body, Strike, Focus"] = "shared_team_increase_traits",
            ["+5% Experience Points"] = "shared_team_increase_xp",
            ["+15% Maximum Energy"] = "shared_team_max_energy",
            ["+15% Maximum Health"] = "shared_team_max_health",
            ["10% Reduced Energy Cost"] = "shared_team_reduce_energy_cost",
            ["15% Reduced Energy Cost"] = "shared_team_reduce_energy_cost_dlc"
        };
        /// <summary>
        /// Team bonus powerups with description for use in XML2 team_bonus files
        /// </summary>
        internal static readonly Dictionary<string, string> PowerupsXML2 = new()
        {
            ["+20 Energy per Knockout"] = "shared_team_bruiser_bash",
            ["5% Dmg inflicted as Health Gain"] = "shared_team_femme_fatale",
            ["+5% Experience"] = "shared_team_brotherhood_of_evil",
            ["+10 All Resistances"] = "shared_team_elemental_fusion",
            ["+100% Attack Rating"] = "shared_team_age_of_apoc",
            ["+5% Damage"] = "shared_team_special_ops",
            ["+10 All Traits"] = "shared_team_heavy_metal",
            ["+5 Health Regeneration"] = "shared_team_family_affair",
            ["+15% Max Energy"] = "shared_team_old_school",
            ["20 Health per KO"] = "shared_team_double_date",
            ["+15% Max Health"] = "shared_team_new_xmen",
            ["+60% Techbit drops"] = "shared_team_raven_knights"
        };
        // Note: One could add get/set modifications to the properties, but I hope this way, the UI is more responsive.
        /// <summary>
        /// If the <see cref="CharacterLists.TeamsMUA"/>/<see cref="CharacterLists.TeamsXML2"/> count isn't 0, uses these saved collections;
        /// otherwise deserializes the team_bonus XML file as <see cref="Bonuses"/> to <see cref="CharacterLists.Teams"/>
        /// (using <see cref="Bonuses.Teams"/>, and adding <paramref name="DeleteCommand"/> to entries).
        /// </summary>
        internal static void Deserialize(StandardUICommand? DeleteCommand = null)
        {
            CfgSt.Roster.Teams = CfgSt.GUI.IsMua ? CfgSt.Roster.TeamsMUA : CfgSt.Roster.TeamsXML2;
            if (CfgSt.Roster.Teams.Count == 0
                && GUIXML.Deserialize(OHSpath.Team_bonus, typeof(Bonuses)) is Bonuses TB)
            {
                for (int i = 0; i < TB.Teams.Count; i++) { TB.Teams[i].OnDeserialized(DeleteCommand); }
                CfgSt.Roster.Teams = new(TB.Teams); // Collection expression doesn't work, yet
                if (DeleteCommand != null)
                {   // Makes the bonuses recoverable
                    _ = CfgSt.GUI.IsMua
                        ? (CfgSt.Roster.TeamsMUA = CfgSt.Roster.Teams)
                        : (CfgSt.Roster.TeamsXML2 = CfgSt.Roster.Teams);
                }
            }
        }
        /// <summary>
        /// Serializes the Team <see cref="ObservableCollection{T}"/> to XML and saves it to the <paramref name="FileName"/> (team_bonus)
        /// </summary>
        /// <returns><see langword="True"/> if a new file was created; otherwise <see langword="false"/>.</returns>
        internal static bool Serialize(string FileName)
        {
            CfgSt.Roster.Teams = CfgSt.GUI.IsMua ? CfgSt.Roster.TeamsMUA : CfgSt.Roster.TeamsXML2;
            if (CfgSt.Roster.Teams.Count == 0) { return false; }
            Dictionary<string, string> PU = CfgSt.GUI.IsMua ? Powerups : PowerupsXML2;
            Bonuses B = new() { Teams = [.. CfgSt.Roster.Teams] };
            for (int i = 0; i < B.Teams.Count; i++) { B.Teams[i].OnSerializing(PU); }
            XmlSerializer XS = new(typeof(Bonuses));
            try
            {
                using XmlWriter XW = XmlWriter.Create(FileName, GUIXML.xws);
                XS.Serialize(XW, B, GUIXML.ns);
                return true;
            }
            catch { return false; }
        }
    }
}
