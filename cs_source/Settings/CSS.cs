using Microsoft.UI.Xaml.Media.Imaging;
using OpenHeroSelectGUI.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Stage model with a name, creator, path and image data. Path should be checked separately for IGB model.
    /// </summary>
    public class CSSModel(Model M, string Igb, bool Favourite)
    {
        public string Name { get; set; } = M.Name;
        public string? Creator { get; set; } = M.Creator;
        public BitmapImage? Image { get; set; }
        public bool Favourite { get; set; } = Favourite;
        public string IgbPath { get; set; } = Igb;
        public override string ToString() => Name is null ? "" : $"Model: {Name} by {Creator}";
    }
    /// <summary>
    /// Stage model with a name, creator, and path.
    /// </summary>
    public class Model
    {
        public string Name { get; set; } = "";
        public string? Creator { get; set; }
        public string? Path { get; set; }
        /// <summary>
        /// Convert to <see cref="CSSModel"/>, using <see cref="Name"/> and <see cref="Creator"/>, converting <see cref="Path"/> to <see cref="CSSModel.IgbPath"/> and <see cref="CSSModel.Image"/>.
        /// </summary>
        /// <returns>The new <see cref="CSSModel"/>, if not filtered by favourites (only if <paramref name="AllowNonFavourites"/> is <see langword="false"/>) and model has an .igb (no file exceptions); otherwise <see langword="null"/>.</returns>
        public CSSModel? ToCSSModel(bool AllowNonFavourites = true)
        {
            bool IsFavourite = CfgSt.GUI.StageFavourites.Contains(Name);
            if (!(AllowNonFavourites || IsFavourite) || string.IsNullOrWhiteSpace(Path)) { return null; }
            string? IgbPath = null;
            string? ImgPath = null;
            try
            {
                foreach (string ModelFile in System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(OHSpath.Model, Path)))
                {
                    if (ModelFile.EndsWith(".igb", StringComparison.OrdinalIgnoreCase)) { IgbPath ??= ModelFile; }
                    else { ImgPath ??= ModelFile; }
                    if (!(IgbPath is null || ImgPath is null)) { break; }
                }
            }
            catch { }
            return IgbPath is null ? null : new CSSModel(this, IgbPath, IsFavourite) { Image = new BitmapImage(new Uri(ImgPath ?? OHSpath.NoPreview)) };
        }
    }
    /// <summary>
    /// For searializing and deserializing the stage model configuration XML file. Contains the <see cref="Name"/> and holds the <see cref="Model"/>s.
    /// </summary>
    public class Category
    {
        [XmlAttribute("category")]
        public string Name { get; set; } = "";

        [XmlElement("Model")]
        public List<Model> Models { get; set; } = [];

    }
    /// <summary>
    /// For searializing and deserializing the stage model configuration XML file. Contains the core <see cref="Category"/> elements.
    /// </summary>
    public class Models
    {
        [XmlIgnore]
        public Dictionary<string, List<Model>> categories = [];

        [XmlElement("Category")]
        public List<Category> Categories { get; set; } = [];

        //categories ??= Categories.ToDictionary(c => c.Name, c => c.Models); // StringComparer.OrdinalIgnoreCase
        //public void Add(string Key, List<Model> Value)
        //{
        //    categories.Add(Key, Value);
        //    Categories.Add(new Category() { Name = Key, Models = Value });
        //}
    }

    public class Vector2D { public double X; public double Y; }

    public class Information
    {
        [XmlElement("name")]
        public string? Name { get; set; }

        [XmlElement("platform")]
        public string? Platform { get; set; }
        public override string ToString()
        {
            string str = $"Layout: {Name} for {Platform}";
            return str.Length == 13 ? "" : str;
        }
    }

    public class Location
    {
        [XmlAttribute]
        public int Number { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }

    public class LocationSetup
    {
        [XmlAttribute("spacious")]
        public bool Spacious { get; set; }

        [XmlElement("Location")]
        public List<Location> Locations { get; set; } = [];
    }

    public class CompatibleModel
    {
        [XmlText]
        public string? Name { get; set; }

        [XmlAttribute]
        public bool Riser { get; set; }
    }

    public class DefaultRoster
    {
        [XmlElement("menulocations")]
        public string Menulocations { get; set; } = "";

        [XmlElement("roster")]
        public string Roster { get; set; } = "";
    }
    /// <summary>
    /// For searializing and deserializing stage layout configuration XML files. This class contains the core elements.
    /// </summary>
    public class Layout
    {
        public Information Information = new();

        [XmlElement("Location_Setup")]
        public LocationSetup LocationSetup { get; set; } = new();

        [XmlArray("Compatible_Models")]
        [XmlArrayItem("Model")]
        public CompatibleModel[] CompatibleModels { get; set; } = [];

        [XmlElement("Default_Roster")]
        public DefaultRoster Default = new();

        [XmlIgnore]
        public bool SelectedStageRiser { get; set; }

        [XmlIgnore]
        public int[] Locs { get; private set; } = [];

        [XmlIgnore]
        private readonly byte[] LocIx = new byte[128]; // Limited to 256 locs from 00 to 127

        [XmlIgnore]
        public System.Collections.ObjectModel.ObservableCollection<LocationButton> Buttons { get; } = [];

        [XmlIgnore]
        public Vector2D[] BVLayoutCoordinates
        {
            get
            {
                if (field is null)
                {
                    field = new Vector2D[Locs.Length];
                    double Multiplier = LocationSetup.Spacious ? 1 : 1.4;
                    int minX = LocationSetup.Locations.Min(static l => l.X);
                    int minY = LocationSetup.Locations.Min(static l => l.Y);
                    for (int i = 0; i < Locs.Length; i++)
                    {
                        Location ML = LocationSetup.Locations[i];
                        field[i] = new()
                        {
                            X = (ML.X - minX) * Multiplier,
                            Y = (ML.Y - minY) * Multiplier
                        };
                    }
                }
                return field;
            }
        }
        [XmlIgnore]
        public Vector2D[] RowLayoutCoordinates
        {
            get
            {
                if (field is null)
                {
                    field = new Vector2D[Locs.Length];
                    const int CamX = 0;
                    const int CamY = -200;
                    const int CamZ = 110;
                    const int FocalLength = 460; // infl. spacing (we could add an angle factor later if needed)
                    double Multiplier = LocationSetup.Spacious ? 1 : 1.4;
                    double minX = double.MaxValue;
                    double minY = double.MaxValue;
                    for (int i = 0; i < Locs.Length; i++)
                    {
                        Location ML = LocationSetup.Locations[i];
                        double depth = Math.Max(ML.Y - CamY, 0.1); // Prevent division by zero/inverse projection
                        Vector2D Co = new()
                        {
                            X = (ML.X - CamX) * FocalLength / depth,
                            Y = (ML.Z - CamZ) * FocalLength * Multiplier / depth
                        };
                        field[i] = Co;
                        if (minX > Co.X) { minX = Co.X; }
                        if (minY > Co.Y) { minY = Co.Y; }
                    }
                    for (int i = 0; i < field.Length; i++)
                    {
                        field[i].X = (field[i].X - minX) * Multiplier;
                        field[i].Y = (field[i].Y - minY) * Multiplier;
                    }
                }
                return field;
            }
        }
        /// <summary>
        /// Define the <see cref="DefaultRoster.Roster"/> and set the <see cref="Locs"/>, according to <paramref name="Size"/>.
        /// </summary>
        public void SetDefaultRosterXML2(int Size)
        {
            CfgSt.XML2.RosterSize = Size;
            SetDefaultRosterXML2();
        }
        /// <summary>
        /// Define the <see cref="DefaultRoster.Roster"/> and set the <see cref="Locs"/>, according to <see cref="XML2settings.RosterSize"/>.
        /// </summary>
        public void SetDefaultRosterXML2()
        {
            CfgSt.Roster.Total = CfgSt.XML2.RosterSize;
            Default.Roster = CfgSt.XML2.RosterSize >= 23
                ? "Default 22 Character (PSP) Roster"
                : CfgSt.XML2.RosterSize >= 21
                ? "Default 20 Character (PC) Roster"
                : "Default 18 Character (GC, PS2, Xbox) Roster";
            Locs = [.. Enumerable.Range(1, CfgSt.XML2.RosterSize)];
        }
        /// <summary>
        /// Initialize the <see cref="Locs"/> and <see cref="Buttons"/>, according to the loaded <see cref="LocationSetup"/>.
        /// </summary>
        public void SetLocationsMUA()
        {
            // Checking Locs length to avoid re-initialization
            if (Locs.Length != 0) { return; }
            Locs = [.. LocationSetup.Locations.Select(static l => l.Number)];
            if (Locs.Length == 0) { return; }
            Vector2D[]? Co = CfgSt.GUI.RowLayout ? RowLayoutCoordinates : BVLayoutCoordinates;
            CfgSt.Roster.Total = Locs.Length;
            for (int i = 0; i < Locs.Length; i++)
            {
                int Number = LocationSetup.Locations[i].Number;
                Buttons.Add(new LocationButton
                {
                    Number = Number,
                    Margin = new(Co[i].X, 0, 0, Co[i].Y),
                    IsChecked = CfgSt.Roster.SelectedLocs.Get(Number)
                });
                LocIx[Number] = (byte)i;
            }
        }
        /// <summary>
        /// Switches the layout of the location <see cref="Buttons"/> between row and bird views, based on <see cref="GUIsettings.RowLayout"/>, by modifying the margin values.
        /// </summary>
        public void SwitchButtonLayout(bool RowLayout)
        {
            if (Locs.Length == 0) { return; }
            Vector2D[]? Co = RowLayout ? RowLayoutCoordinates : BVLayoutCoordinates;
            for (int i = 0; i < Locs.Length; i++) { Buttons[i].Margin = new(Co[i].X, 0, 0, Co[i].Y); }
        }
        /// <summary>
        /// Set the state of location <see cref="Buttons"/> with <paramref name="Loc"/> to <paramref name="IsChecked"/>.
        /// </summary>
        public void UpdateLocBoxes(int Loc, bool IsChecked)
        {
            Buttons[LocIx[Loc]].IsChecked = IsChecked;
        }
        /// <summary>
        /// Update the state of all location <see cref="Buttons"/>. Call this after bulk operations on <see cref="CharacterLists.Selected"/> (except clear).
        /// </summary>
        public void UpdateLocBoxes()
        {
            for (int i = 0; i < Buttons.Count; i++) { Buttons[i].IsChecked = CfgSt.Roster.SelectedLocs.Get(Buttons[i].Number); }
        }
        /// <summary>
        /// Update the state of all location <see cref="Buttons"/> to <see langword="false"/> (unchecked). Call this after clearing <see cref="CharacterLists.Selected"/>.
        /// </summary>
        public void DeselectLocBoxes()
        {
            for (int i = 0; i < Buttons.Count; i++) { Buttons[i].IsChecked = false; }
        }
    }

    public class Offset
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }

    public class CSSEffect
    {
        public string? Name { get; set; }
        public string? File { get; set; }
        public Offset Offset { get; set; } = new();
    }

    [XmlRoot("Effects")]
    public class CSSEffects
    {
        [XmlElement("Effect")]
        public List<CSSEffect> EffectList { get; set; } = [];
    }
}
