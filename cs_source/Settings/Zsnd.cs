using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static Zsnd.Lib.Properties;

// IMPORTANT NOTE:
// THIS ASSUMES LITTLE ENDIAN MACHINES, WHICH IS RELEVANT FOR ZSM PARSING,
// BECAUSE THEIR ENDIANNESS DEPENDS ON THE PLATFORM THEY ARE FROM/FOR.
// WHEN BUILDING THE PROJECT FOR BIG ENDIAN MACHINES, THE LOGIC MUST BE CHANGED HERE.
namespace Zsnd.Lib
{
    // Note: ZsndReader and ZsndWriter could be expanded to support ZsndProperties independent endian check
    /// <summary>
    /// A partial class of <see cref="BinaryReader"/> for <see cref="FileStream"/> <paramref name="input"/>, adding endian aware <see cref="uint"/> and <see cref="ushort"/> read overrides (depending on <see cref="PlatIs7thGen"/>).
    /// </summary>
    public partial class ZReader(Stream input) : BinaryReader(input)
    {
        private readonly byte[] _buffer = new byte[4];

        public override ushort ReadUInt16() => PlatIs7thGen
            ? BinaryPrimitives.ReadUInt16BigEndian(ReadByteSpan(2))
            : BinaryPrimitives.ReadUInt16LittleEndian(ReadByteSpan(2));

        public override uint ReadUInt32() => PlatIs7thGen
            ? BinaryPrimitives.ReadUInt32BigEndian(ReadByteSpan(4))
            : BinaryPrimitives.ReadUInt32LittleEndian(ReadByteSpan(4));

        private ReadOnlySpan<byte> ReadByteSpan(int numBytes)
        {
            BaseStream.ReadExactly(_buffer.AsSpan(0, numBytes));
            return _buffer;
        }
    }
    /// <summary>
    /// A <see cref="FileStream"/> writer for <paramref name="input"/>, adding <see cref="uint"/> and <see cref="ushort"/> writes that depend on <paramref name="BigEndian"/>.
    /// </summary>
    public partial class ZWriter(FileStream input) : IDisposable
    {
        private readonly FileStream _stream = input;

        protected virtual void Dispose(bool disposing)
        {
            _stream.Close();
        }
        /// <summary>
        /// Close the <see cref="FileStream"/> <see cref="_stream"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Writes a <paramref name="buffer"/> to this stream. The current position of the stream is advanced by the buffer length. (or better span?: public virtual void Write(ReadOnlySpan{byte} buffer))
        /// </summary>
        public virtual void Write(byte[] buffer)
        {
            _stream.Write(buffer, 0, buffer.Length);
        }
        /// <summary>
        /// Writes a two-byte unsigned <paramref name="integer"/> to this stream, depending on <see cref="PlatIs7thGen"/>. The current position of the stream is advanced by two.
        /// </summary>
        public virtual void Write(ushort integer)
        {
            Span<byte> buffer = stackalloc byte[2];
            if (PlatIs7thGen)
            {
                BinaryPrimitives.WriteUInt16BigEndian(buffer, integer);
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(buffer, integer);
            }
            _stream.Write(buffer);
        }
        /// <summary>
        /// Writes a four-byte unsigned <paramref name="integer"/> to this stream, depending on <see cref="PlatIs7thGen"/>. The current position of the stream is advanced by four.
        /// </summary>
        public virtual void Write(uint integer)
        {
            Span<byte> buffer = stackalloc byte[4];
            if (PlatIs7thGen)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, integer);
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(buffer, integer);
            }
            _stream.Write(buffer);
        }
        /// <summary>
        /// Writes a <paramref name="String"/> to this stream. Caps at or pads to 64 characters, depending on string length. The current position of the stream is advanced by 64.
        /// </summary>
        public virtual void Write(string String)
        {
            byte[] buffer = new byte[64]; // needed each time to ensure 0
            _ = Encoding.UTF8.GetBytes(String, 0, Math.Min(String.Length, 64), buffer, 0);
            Write(buffer);
        }
        /// <summary>
        /// Writes a <see cref="uint"/> <paramref name="array"/> to this stream. The current position of the stream is advanced by array.Length * four.
        /// </summary>
        //public virtual void Write(uint[] array)
        //{
        //    _stream.Write(MemoryMarshal.Cast<uint, byte>(array.AsSpan()));
        //}
    }
    /// <summary>
    /// Zsnd header <see langword="struct"/> for parsing and writing the header data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ZsndHeader(uint InfoSize, uint SoundCount, uint SampleCount)
    {
        public uint Size;
        public uint HeaderSize = InfoSize;
        public uint SoundCount = SoundCount;
        public uint SoundHashesOffset;
        public uint SoundsOffset;
        public uint SampleCount = SampleCount;
        public uint SampleHashesOffset;
        public uint SamplesOffset;
        public uint SampleFileCount = SampleCount;
        public uint SampleFileHashesOffset;
        public uint SampleFilesOffset;
        public uint PhraseCount; // 0
        public uint PhraseHashesOffset = InfoSize;
        public uint PhrasesOffset = InfoSize;
        public uint TrackDefCount; // 0
        public uint TrackDefHashesOffset = InfoSize;
        public uint TrackDefsOffset = InfoSize;
        public uint ReservedCount; // 0
        public uint ReservedHashesOffset = InfoSize;
        public uint ReservedOffset = InfoSize;
        public uint KeymapCount; // 0
        public uint KeymapHashesOffset = InfoSize;
        public uint KeymapsOffset = InfoSize;

        public static ZsndHeader? FromZsndStream(ZReader reader)
        {
            int headerSize = Marshal.SizeOf<ZsndHeader>();
            byte[] headerBytes = new byte[headerSize];
            return reader.Read(headerBytes, 0, headerSize) == headerSize
                ? PlatIs7thGen
                ? MemoryMarshal.Read<ZsndHeader>(ReverseEndianness(headerBytes).AsSpan())
                : MemoryMarshal.Read<ZsndHeader>(headerBytes.AsSpan())
                : null;
        }
        // LE MACHINES!
        private static byte[] ReverseEndianness(byte[] B)
        {
            for (byte i = 0; i < B.Length - 4; i += 4)
            {
                (B[i], B[i + 1], B[i + 2], B[i + 3]) = (B[i + 3], B[i + 2], B[i + 1], B[i]);
            }
            return B;
        }
    }
    /// <summary>
    /// Zsnd Sound JSON representaion
    /// </summary>
    public class JsonSound
    {
        public string Hash { get; set; } = "";
        public int Sample_index { get; set; }
        public SoundF Flags { get; set; }

        private static JsonSound FromUISound(UISoundBase value)
        {
            return new JsonSound
            {
                Hash = value.Hash,
                Sample_index = value.SampleIndex,
                Flags = (SoundF)value.Flags
            };
        }
        public static implicit operator JsonSound(XVSound value) => FromUISound(value);
        public static implicit operator JsonSound(UISound value) => FromUISound(value);
    }
    /// <summary>
    /// Zsnd Sample JSON representaion
    /// </summary>
    public class JsonSample
    {
        public string? File { get; set; }
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public uint Format { get; set; }
        public uint Sample_rate { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public SampleF Flags { get; set; }
    }
    /// <summary>
    /// Zsnd main JSON representaion using lists. Note: not observable for performance reason.
    /// </summary>
    public class JsonMain
    {
        public string Platform { get; set; } = "PC";
        public List<JsonSound> Sounds { get; set; } = [];
        public List<JsonSample> Samples { get; set; } = [];
    }
    /// <summary>
    /// Base Sound <see langword="interface"/> for UI sound classes.
    /// </summary>
    public interface IUISound
    {
        string Hash { get; set; }
        int SampleIndex { get; set; }
        byte Flags { get; set; }
        string? Sample { get; set; }
        bool F1 { get; set; }
        bool F2 { get; set; }
        bool F3 { get; set; }
        bool F4 { get; set; }
        bool F5 { get; set; }
        bool F6 { get; set; }
        bool F7 { get; set; }
        bool F8 { get; set; }
    }
    /// <summary>
    /// Zsnd UI Sound Base <see cref="ObservableObject"/>
    /// </summary>
    public partial class UISoundBase : ObservableObject, IUISound
    {
        public string Hash { get; set; } = "";

        [ObservableProperty]
        public partial int SampleIndex { get; set; } = -1;

        [ObservableProperty]
        public partial byte Flags { get; set; } = 0xFF;

        [ObservableProperty]
        public partial string? Sample { get; set; } = "-- Select a sound file --";

        [ObservableProperty]
        public partial bool F1 { get; set; } = true;

        [ObservableProperty]
        public partial bool F2 { get; set; } = true;

        [ObservableProperty]
        public partial bool F3 { get; set; } = true;

        [ObservableProperty]
        public partial bool F4 { get; set; } = true;

        [ObservableProperty]
        public partial bool F5 { get; set; } = true;

        [ObservableProperty]
        public partial bool F6 { get; set; } = true;

        [ObservableProperty]
        public partial bool F7 { get; set; } = true;

        [ObservableProperty]
        public partial bool F8 { get; set; } = true;

        public UISoundBase() { }

        public UISoundBase(JsonSound value)
        {
            Hash = value.Hash;
            SampleIndex = value.Sample_index;
            Flags = (byte)value.Flags;
            F1 = value.Flags.HasFlag(SoundF.Unk1);
            F2 = value.Flags.HasFlag(SoundF.Unk2);
            F3 = value.Flags.HasFlag(SoundF.Unk3);
            F4 = value.Flags.HasFlag(SoundF.Unk4);
            F5 = value.Flags.HasFlag(SoundF.Unk5);
            F6 = value.Flags.HasFlag(SoundF.Unk6);
            F7 = value.Flags.HasFlag(SoundF.Unk7);
            F8 = value.Flags.HasFlag(SoundF.Unk8);
        }

        partial void OnSampleIndexChanged(int value) => Sample = value < Lists.Samples.Count && value > -1 ? Path.GetFileName(Lists.Samples[value].File) : "-- Select a sound file --";

        partial void OnF1Changed(bool value) => Flags = (byte)(value ? Flags | 1 : Flags & ~1);
        partial void OnF2Changed(bool value) => Flags = (byte)(value ? Flags | 2 : Flags & ~2);
        partial void OnF3Changed(bool value) => Flags = (byte)(value ? Flags | 4 : Flags & ~4);
        partial void OnF4Changed(bool value) => Flags = (byte)(value ? Flags | 8 : Flags & ~8);
        partial void OnF5Changed(bool value) => Flags = (byte)(value ? Flags | 16 : Flags & ~16);
        partial void OnF6Changed(bool value) => Flags = (byte)(value ? Flags | 32 : Flags & ~32);
        partial void OnF7Changed(bool value) => Flags = (byte)(value ? Flags | 64 : Flags & ~64);
        partial void OnF8Changed(bool value) => Flags = (byte)(value ? Flags | 128 : Flags & ~128);
        /// <summary>
        /// Switch the flag to the 31 representation, keeping the lower 5 flags as they are and disabling the high 3. Nees better descriptions after knowing what the flags do.
        /// </summary>
        //public void To31()
        //{
        //    F6 = false;
        //    F7 = false;
        //    F8 = false;
        //}
    }
    /// <summary>
    /// Zsnd UI Sound Entry <see cref="ObservableObject"/>
    /// </summary>
    public partial class UISound(JsonSound value) : UISoundBase(value)
    {
        [ObservableProperty]
        public new partial string Hash { get; set; } = value.Hash; // no need for Flags?

        public static implicit operator UISound(JsonSound value) => new(value);
    }
    /// <summary>
    /// X_voice info class with the required details for Zsnd files (<see cref="ObservableObject"/>).
    /// </summary>
    public partial class XVSound : UISoundBase
    {
        [ObservableProperty]
        public partial Events.XVprefix? Pref { get; set; }

        [ObservableProperty]
        public partial string? IntName { get; set; }
        /// <summary>
        /// Updates the <see cref="UISoundBase.Hash"/> based on the changed <paramref name="newValue"/> and <see cref="IntName"/> (must always be set first).
        /// </summary>
        partial void OnPrefChanged(Events.XVprefix? oldValue, Events.XVprefix? newValue)
        {
            if (newValue is null || IntName is null || oldValue is null) { return; }
            if (newValue is Events.XVprefix.AN or Events.XVprefix.BREAK)
            {
                Hash = $"COMMON/MENUS/CHARACTER/{newValue}_{IntName}";
                Events.LastCharPrefix = (Events.XVprefix)newValue;
            }
            else if (newValue is Events.XVprefix.TEAM)
            {
                // If no internal name, hash stays unchanged. Hash must be managed externally in such cases.
                Hash = $"COMMON/TEAM_BONUS_{(IntName.StartsWith("TEAM_BONUS_", StringComparison.OrdinalIgnoreCase)
                    ? IntName[11..]
                    : IntName.StartsWith("BONUS_", StringComparison.OrdinalIgnoreCase)
                    ? IntName[6..] : IntName)}";
            }
        }

        public XVSound() { }

        public XVSound(JsonSound value) : base(value)
        {
            string[] HE = Hashing.EnsureHash(value.Hash).Split('/')[^1].Split('_', 2);
            if (Enum.TryParse(HE[0], out Events.XVprefix HEP)) { Pref = HEP; }
            if (HE.Length > 1) { IntName = HE[1]; } // After (Important to avoid OnPrefChanged)
            _ = Pref is Events.XVprefix.AN or Events.XVprefix.BREAK
                && Lists.XVInternalNames.Add(IntName);
        }

        public static implicit operator XVSound(JsonSound value) => new(value);
    }
    /// <summary>
    /// Zsnd UI Sample Entry <see cref="ObservableObject"/>
    /// </summary>
    //public partial class UISample : ObservableObject
    //{
    //    [ObservableProperty]
    //    public partial string? Filename { get; set; }
    //
    //    [ObservableProperty]
    //    public partial bool Loop { get; set; }
    //
    //    public static implicit operator UISample(JsonSample value)
    //    {
    //        return new UISample
    //        {
    //            Filename = value.File,
    //            Loop = value.Flags.HasFlag(SampleF.Loop)
    //        };
    //    }
    //}
    /// <summary>
    /// Zsnd UI Lists
    /// </summary>
    public static class Lists
    {
        public static List<XVSound> Sounds { get; set; } = [];
        public static ObservableCollection<JsonSample> Samples { get; set; } = [];
        public static HashSet<string?> XVInternalNames { get; set; } = [];
    }
    /// <summary>
    /// The main bind class, NOTE: Needed when lists are shared among pages.
    /// </summary>
    //public class Zsnd
    //{
    //    //public Json_Main Json { get; set; } = Sjson.Data; -> removed, would be static class
    //}
    /// <summary>
    /// Enum classes for Zsnd values
    /// </summary>
    public static class Properties
    {
        [Flags]
        public enum SoundF : byte
        {
            None = 0,
            Unk1 = 1,
            Unk2 = 2,
            Unk3 = 4,
            Unk4 = 8,
            Unk5 = 16,
            Unk6 = 32,
            Unk7 = 64,
            Unk8 = 128
        }

        [Flags]
        public enum SampleF : ushort
        {
            None = 0,
            Loop = 1,
            Stereo = 2,
            Unknown1 = 4,
            Unknown2 = 8,
            Unknown3 = 16,
            AmbientEmbedded = 32,
            FourChannels = Stereo | AmbientEmbedded,
            Unknown4 = 64,
            Unknown5 = 128
        }

        public enum ZPlatform
        {
            PC,
            XBOX,
            XENO,
            PS2,
            PS3,
            GCUB
        }
        /// <summary>
        /// The saved <see cref="ZPlatform"/>.
        /// </summary>
        public static ZPlatform? Platform { get; set; } = 0;
        /// <summary>
        /// Whether the saved <see cref="Platform"/> is <see cref="ZPlatform.PC"/>, <see cref="ZPlatform.XBOX"/> or <see cref="ZPlatform.XENO"/>.
        /// </summary>
        public static bool PlatIsMicrosoft { get; set; } = true;
        /// <summary>
        /// Whether the saved <see cref="Platform"/> is <see cref="ZPlatform.PS2"/> or <see cref="ZPlatform.PS3"/>.
        /// </summary>
        public static bool PlatIsPS { get; set; }
        /// <summary>
        /// Whether the saved <see cref="Platform"/> is <see cref="ZPlatform.XENO"/>, <see cref="ZPlatform.PS3"/> or Wii (<see cref="ZPlatform.GCUB"/>, possibly includes Gamecube as well). These are all Big Endian.
        /// </summary>
        public static bool PlatIs7thGen { get; set; }
        /// <summary>
        /// The Zsnd initial identifier "ZSND" plus the <see cref="Platform"/> ID as <see cref="string"/> (padded with white space to total of 8 bytes).
        /// </summary>
        public static byte[] PlatformMagic => Encoding.ASCII.GetBytes($"ZSND{Platform}".PadRight(8));
        /// <summary>
        /// The Size <see cref="uint"/>, depending on the saved <see cref="Platform"/>.
        /// </summary>
        public static uint PlatFileHashSz => (uint)(Platform == ZPlatform.XBOX ? 28 : Platform == ZPlatform.XENO ? 36 : PlatIsPS ? 16 : 24);
        /// <summary>
        /// The Size <see cref="uint"/>, depending on the saved <see cref="Platform"/>.
        /// </summary>
        public static uint PlatFileInfoSz => (uint)(Platform == ZPlatform.PC ? 76 : PlatIsMicrosoft ? 84 : PlatIsPS ? 8 : 12);
        /// <summary>
        /// Save <paramref name="platform"/> to the <see cref="Platform"/> <see cref="ZPlatform"/> <see cref="Enum"/>.
        /// </summary>
        public static void SetPlatform(string platform)
        {
            if (Enum.TryParse(platform, out ZPlatform P))
            {
                Platform = P;
                PlatIsMicrosoft = (int)P < 3;
                PlatIsPS = P is ZPlatform.PS2 or ZPlatform.PS3;
                PlatIs7thGen = P is ZPlatform.XENO or ZPlatform.PS3 or ZPlatform.GCUB;
            }
        }
        /// <summary>
        /// Create a template for the sound details (flags, index, and unkonw, platform specific static values).
        /// </summary>
        /// <returns>The <see cref="byte"/> array template without flags and index saved.</returns>
        public static byte[] GetSoundTemplate()
        {
            byte[] SH = new byte[24];
            byte Unk19_21 = (byte)(Platform == ZPlatform.PS3 ? 0x20 : 0x00);
            if (PlatIs7thGen)
                SH[3] = 0x10;
            else
                SH[2] = 0x10;
            SH[4] = 0x7F;
            SH[9] = 0x7F;
            SH[11] = (byte)(PlatIsPS ? 0x0F : 0x7F);
            SH[19] = Unk19_21;
            SH[20] = Unk19_21;
            SH[21] = Unk19_21;
            return SH;
        }
    }
    /// <summary>
    /// Zsound hash event definitions
    /// </summary>
    public static class Events
    {
        private static readonly int MPowersMultiplier = 12;
        private static readonly int RandomMultiiplier = 21;
        private static readonly string[] MPowers =
        [
            "CHARGE",
            "POWER",
            "IMPACT",
            "CHARGE_LOOP",
            "LOOP"
        ];
        private static readonly string[] M =
        [
            "DEATH",
            "FLYBEGIN",
            "FLYEND",
            "JUMP",
            "LAND",
            "PAIN",
            "PICKUP",
            "PUNCHED",
            "STRUGGLE",
            "THROW",
            // Rare:
            "LIFT",
            "LIFT_SHORT",
            "XTREME_LAUNCH",
            // Used in mods:
            "EXPLODE",
            "DRAW_GUN",
            "HOLSTER_GUN",
            "MUSIC",
            "STEP",
            "TELEPORT",
            "WEB_ZIP"
        ];
        private static readonly string[] V =
        [
            "BORED",
            "CANTGO",
            "CMDATTACKANY",
            "CMDATTACKTARGET",
            "CMDFOLLOW",
            "EPITAPH",
            "LEVELUP",
            "LOWHEALTH",
            "NOPOWER",
            "NOWORK",
            "RESPAFFIRM",
            "STATS",
            "TAUNTKD",
            "THROWTAUNT",
            "TOOHEAVY",
            "VICTORY",
            "XTREME",
            // Enemies only:
            "ISEEYOU",
            "TAUNT",
            "YELL",
            // XML games:
            "BANTER_BISHOP",
            "BANTER_COLOSSUS",
            "BANTER_CYCLOPS",
            "BANTER_GAMBIT",
            "BANTER_ICEMAN",
            "BANTER_IRONMAN",
            "BANTER_JUGGERNAUT",
            "BANTER_MAGNETO",
            "BANTER_NIGHTCRAWLER",
            "BANTER_PHOENIX",
            "BANTER_ROGUE",
            "BANTER_SCARLETWITCH",
            "BANTER_STORM",
            "BANTER_SUNFIRE",
            "BANTER_TOAD",
            "BANTER_WOLVERINE",
            "BANTER_BLADE",
            "BANTER_CAP",
            "BANTER_DD",
            "BANTER_DEADPOOL",
            "BANTER_ELEKTRA",
            "BANTER_GHOSTRIDER",
            "BANTER_PANTHER",
            "BANTER_STRANGE",
            "BANTER_TORCH",
            "BOSSTAUNT",
            "CANTTALK",
            "INCOMING",
            "LAUGH",
            "LOCKED",
            "SIGHT",
            "SOLO_BEGIN",
            "SOLO_END",
            "XTREME2"
        ];
        //public static string[] Master { get; }
        //public static ReadOnlyMemory<string> Master => MRand.AsMemory(0, M.Length + MPowers.Length * MPowersMultiplier);
        //public static string[] Voice => V;
        public static readonly string[] MRand = new string[(M.Length + MPowers.Length * MPowersMultiplier) * RandomMultiiplier];
        public static readonly string[] VRand = new string[V.Length * RandomMultiiplier];
        public static readonly string[] XVoice =
        [
        "MENUS/CHARACTER/",
        "MENUS/CHARACTER/AN_",
        "MENUS/CHARACTER/BREAK_",
        "TEAM_BONUS_",
        ""
        ];
        public static readonly string[] Music = new string[3];
        // Additional music events:
        // "MUSIC/CUES/OUT" covered by JsonHashes
        // "MUSIC/CUES/IN"  covered by JsonHashes
        // "MUSIC/MUSIC_CUES/" Unknown, possibly unused

        public enum Category
        {
            CHAR,
            CHARACTER,
            COMMON,
            MUSIC,
            VOICE
        };

        public enum XVprefix
        {
            OTHER,
            AN,
            BREAK,
            TEAM
        };
        // WIP: Enums To Add:
        // - Music (Ambient: A, Combat: C, Extra: X)
        // - other Menu?
        // For binding:
        public static readonly XVprefix[] XVprefixes = Enum.GetValues<XVprefix>();
        public static XVprefix LastCharPrefix { get; set; } = XVprefix.AN;

        static Events()
        {
            // 20 + 5 (ZsndEvents.MPowers.Length) * 12 = 80, * 21 = 1680 | 54 * 21 = 1134
            M.CopyTo(MRand, 0);
            V.CopyTo(VRand, 0);
            int mp = MPowers.Length;
            int ms = M.Length - mp;
            int m = MRand.Length / RandomMultiiplier;
            int v = V.Length;
            int r = RandomMultiiplier - 1;
            for (int i = 1; i <= MPowersMultiplier; i++)
            {
                for (int j = 0; j < mp; j++)
                    MRand[ms + i * mp + j] = $"P{i}_{MPowers[j]}";
            }
            // Master = MRand[0..(ms + mp + mp * MPowersMultiplier)];
            for (int i = 0; i < r; i++)
            {
                for (int j = 0; j < m; j++)
                    MRand[m + m * i + j] = $"{MRand[j]}/***RANDOM***/{i}";
                for (int j = 0; j < v; j++)
                    VRand[v + v * i + j] = $"{VRand[j]}/***RANDOM***/{i}";
            }
        }
    }
    /// <summary>
    /// Zsound hash event conversion
    /// </summary>
    public static partial class Hashing
    {
        [GeneratedRegex(@"\/\*\*\*RANDOM\*\*\*\/\d+", RegexOptions.IgnoreCase)]
        private static partial Regex Random();

        [GeneratedRegex(@"\d*_ALT$")]
        private static partial Regex AltSfx();

        [GeneratedRegex(@"(\d*_?\d*|\d*_\w\d?)$")]
        private static partial Regex DigitSfx();

        private static Dictionary<uint, string>? JsonHashes;
        private static bool MFirst;
        private static readonly string[] Znames = new string[2];
        private static readonly string[] CharPrefix = ["CHAR", "CHARACTER"];
        private static uint PJWTest;
        /// <summary>
        /// PJW hash generator from <paramref name="str"/>ing.
        /// </summary>
        /// <returns>Hash <see cref="uint"/> generated from <paramref name="str"/>ing.</returns>
        public static uint PJW(string str)
        {
            //const uint BitsInUnsignedInt = (uint)(4 * 8);                                     //32
            //const uint ThreeQuarters = (uint)(BitsInUnsignedInt * 3 / 4);                     //24
            //const uint OneEighth = (uint)(BitsInUnsignedInt / 8);                             //4
            //const uint HighBits = (uint)(0xFFFFFFFF) << (int)(BitsInUnsignedInt - OneEighth); //0xF0000000
            uint hash = 0;
            for (int i = 0; i < str.Length; i++)
            {
                hash = (hash << 4) + ((byte)str[i]);
                if ((PJWTest = hash & 0xF0000000) != 0)
                    hash = (hash ^ (PJWTest >> 24)) & (~0xF0000000);
            }
            return hash; // & 0x7FFFFFFF
        }
        /// <summary>
        /// PJW hash generator from upper-case instance of <paramref name="str"/>ing.
        /// </summary>
        public static uint PJWUPPER(string str)
        {
            return PJW(str.ToUpperInvariant());
        }
        /// <summary>
        /// Convert a <paramref name="HashNum"/> (number) to a hash string, depending on <paramref name="Fname"/> (must be UPPERCASE) and saved events.
        /// </summary>
        /// <param name="Fname">The file base name in UPPERCASE for the reverse generators.</param>
        /// <returns>The hash as a <see cref="string"/>, if identified by <see cref="JsonHashes"/> or events, otherwise <paramref name="HashNum"/> as <see cref="string"/>.</returns>
        public static string ToStr(uint HashNum, string Fname)
        {
            try
            {
                // WIP! For release: OHSpath.CD, "OHSGUI" instead of Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!
                JsonHashes ??= JsonSerializer.Deserialize<Dictionary<uint, string>>(File.ReadAllText(Path.Combine(OpenHeroSelectGUI.Functions.OHSpath.CD, "OHSGUI", "Assets", "zsnd_hashes.json")));
            }
            catch { } // leave JsonHashes null. Does it make sense to report somewhere?
            return Random().Replace(JsonHashes is not null
                && JsonHashes.TryGetValue(HashNum, out string? Hash)
                ? Hash : PJWReverse(HashNum, Fname), "");
        }
        /// <summary>
        /// Check whether the <paramref name="Hash"/> is a <see cref="uint"/> or a <see cref="string"/>.
        /// </summary>
        /// <param name="Fname">The file base name in UPPERCASE for the reverse generators.</param>
        /// <returns>The <paramref name="Hash"/> with replaced random suffix, if it's a <see cref="string"/>, otherwise the attempted reverse generated hash as a <see cref="string"/>.</returns>
        public static string EnsureHash(string Hash) // , string Fname = ""
        {
            return uint.TryParse(Hash, out uint HashNum)
                ? ToStr(HashNum, "")
                : Random().Replace(Hash, "");
        }

        private static string PJWReverse(uint HashNum, string Fname)
        {
            // if the hash wasn't listed, we try to reverse generate it
            if (PlatIsMicrosoft && Fname != "")
            {
                string CleanFn = Fname.StartsWith("XTREME2") ? "XTREME2"
                    : AltSfx().Replace(DigitSfx().Replace(Fname, ""), "");
                string[] FNevents = new string[2 * 21];
                for (int i = -1; i < 20; i++)
                    for (int j = 0; j < 2; j++)
                        FNevents[2 + 2 * i + j] = $"{(j == 0 ? CleanFn : Fname)}{(i == -1 ? "" : $"/***RANDOM***/{i}")}";
                if (PJWReverseChar(HashNum, FNevents) is string HFN1)
                    return HFN1;
                // try voice and pure file name (no random)
                if (PJWReverseChar(HashNum, [$"VOICE/{Fname}", CleanFn, Fname]) is string HFN2)
                    return HFN2;
            }
            // if the file name isn't the hash, try character events
            if (PJWReverseChar(HashNum, MFirst ? Events.MRand : Events.VRand) is string HChar1)
                return HChar1;
            if (PJWReverseChar(HashNum, MFirst ? Events.VRand : Events.MRand) is string HChar2)
                return HChar2;
            // if not character hash, try music events
            for (int e = 0; e < Events.Music.Length; e++)
                if (PJW(Events.Music[e]) == HashNum)
                    return Events.Music[e];
            if (Fname == "") { return HashNum.ToString(); }
            // if not music hash, try x_voice events
            for (int i = -1; i < 20; i++)
                for (int e = 0; e < 5; e++)
                {
                    string Hash = $"COMMON/{Events.XVoice[e]}{Fname}{(i == -1 ? "" : $"/***RANDOM***/{i}")}";
                    if (PJW(Hash) == HashNum)
                        return Hash;
                }
            return HashNum.ToString();
        }

        private static string? PJWReverseChar(uint HashNum, string[] Events)
        {
            for (int n = 0; n < 2; n++)
                for (int c = 0; c < 2; c++)
                    for (int e = 0; e < Events.Length; e++)
                    {
                        string Hash = $"{CharPrefix[c]}/{Znames[n]}/{Events[e]}";
                        if (PJW(Hash) == HashNum)
                            return Hash;
                    }
            return null;
        }
        /// <summary>
        /// Initialize the <see cref="Events.MRand"/>/<see cref="Events.VRand"/> priority, the <see cref="Events.Music"/> and <see cref="Znames"/> events with the <paramref name="Zname"/>.
        /// </summary>
        /// <param name="Zname">The .zss/.zsm file base name in UPPERCASE for the reverse generators.</param>
        /// <remarks>Must be done before using <see cref="ToStr"/> or <see cref="EnsureHash"/>. <paramref name="Zname"/> must be UPPERCASE.</remarks>
        public static void PrepareEvents(string Zname)
        {
            char ZsSfx = Zname[^1];
            string BaseZname = Zname[0..^1];
            MFirst = ZsSfx == 'M';
            Znames[0] = Zname;
            Znames[1] = $"{BaseZname}{(MFirst ? "V" : "M")}";
            for (int i = 0; i < 3; i++)
                Events.Music[i] = $"MUSIC/{BaseZname}{"ACX"[i]}";
        }
        /// <summary>
        /// Add random suffixes to the duplicate hashes in <see cref="Lists.Sounds"/>, and make them UPPERCASE.
        /// </summary>
        public static void AddRandomSuffix()
        {
            // Note: This is slightly faster than any single pass solutions
            Dictionary<string, int> Randoms = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> Uniques = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Lists.Sounds.Count; i++)
            {
                string Hash = Lists.Sounds[i].Hash;
                if (!Uniques.Add(Hash)) { Randoms[Hash] = 0; }
            }
            for (int i = 0; i < Lists.Sounds.Count; i++)
            {
                string Hash = Lists.Sounds[i].Hash;
                if (Randoms.TryGetValue(Hash, out int index))
                {
                    Lists.Sounds[i].Hash = $"{Hash.ToUpperInvariant()}/***RANDOM***/{index}";
                    Randoms[Hash]++;
                }
            }
        }
    }

    public static class Cmd
    {
        private static readonly JsonSerializerOptions JsonOptionsD = new() { PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        private static JsonMain? LoadJson(string JsonFile)
        {
            if (File.Exists(JsonFile))
            {
                try
                {
                    return JsonSerializer.Deserialize<JsonMain>(File.ReadAllText(JsonFile), JsonOptionsD)!;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
        /// <summary>
        /// Save the <see cref="Lists.Sounds"/> and <see cref="Lists.Samples"/> lists as a Raven-Formats <paramref name="JsonFile"/>, using the saved <see cref="Platform"/>.
        /// </summary>
        /// <returns><see langword="Null"/>, if saved successfully, otherwise an error message as <see cref="string"/>.</returns>
        public static string? SaveJson(string JsonFile)
        {
            if (Platform is null)
            {
                return "Platform is not defined.";
            }
            JsonMain? Json = new()
            {
                Platform = Platform.ToString()!,
                Samples = [.. Lists.Samples], // ToList(), merely references
                Sounds = [.. Lists.Sounds]
            };
            try
            {
                _ = Directory.CreateDirectory(Path.GetDirectoryName(JsonFile)!);
                File.WriteAllText(JsonFile, JsonSerializer.Serialize(Json, JsonOptions));
            }
            catch
            {
                return $"Failed to save configurations to {JsonFile}.";
            }
            return null;
        }
        /// <summary>
        /// Read x_voice info from <paramref name="JsonFile"/>.
        /// </summary>
        public static void ReadXVoice(string JsonFile)
        {
            if (LoadJson(JsonFile) is JsonMain XV)
            {
                // Observablec. don't have AddRange (yet). Planned in .Net future versions.
                // Best unofficial extension, performance-wise (no reflection):
                // https://stackoverflow.com/questions/670577/observablecollection-doesnt-support-addrange-method-so-i-get-notified-for-each/45364074#45364074
                Lists.XVInternalNames.Clear();
                Hashing.PrepareEvents("X_VOICE");
                SetPlatform(XV.Platform);
                Lists.Samples = new(XV.Samples);
                Lists.Sounds = [.. XV.Sounds];
            }
        }
        /// <summary>
        /// Incomplete: Read sound info from <paramref name="JsonFile"/>.
        /// </summary>
        public static void ReadJson(string JsonFile)
        {
            if (LoadJson(JsonFile) is JsonMain ZJ)
            {
                string Zname = Path.GetFileNameWithoutExtension(JsonFile).ToUpperInvariant();
                Hashing.PrepareEvents(Zname);
                SetPlatform(ZJ.Platform);
                Lists.Samples = new(ZJ.Samples);
                if (Zname.StartsWith("X_VOICE"))
                    Lists.Sounds = [.. ZJ.Sounds];
                //else
                //    Lists.NonXVSounds = [.. ZJ.Sounds];
            }
        }
        // Load and write Zsnd could have more combined loops
        /// <summary>
        /// Read sound info from a .zss/.zsm file, as defined by <paramref name="Zfile"/>, and write sound files to <paramref name="OutPath"/> (defaults to <paramref name="Zfile"/>'s directory).
        /// </summary>
        /// <returns><see langword="True"/>, if parsed, otherwise <see langword="false"/>.</returns>
        public static bool LoadZsnd(string Zfile, string OutPath = "") // , bool IsXvoice = false
        {
            using FileStream fs = new(Zfile, FileMode.Open, FileAccess.Read);
            using ZReader reader = new(fs); // UTF8
            if (new string(reader.ReadChars(4)) == "ZSND")
            {
                string Platform = new string(reader.ReadChars(4)).TrimEnd();
                SetPlatform(Platform);
                if (ZsndHeader.FromZsndStream(reader) is ZsndHeader Header
                    && Header.SoundCount > 0 && Header.SampleCount > 0 && Header.SampleFileCount == Header.SampleCount)
                {
                    string Zname = Path.GetFileNameWithoutExtension(Zfile);
                    Hashing.PrepareEvents(Zname.ToUpperInvariant());

                    reader.BaseStream.Position = Header.SoundHashesOffset; // should be here already
                    uint[] SoundHashes = new uint[Header.SoundCount];
                    ushort[] SIndx = new ushort[Header.SoundCount];
                    byte[] SoundFl = new byte[Header.SoundCount];
                    uint[] Offsets = new uint[Header.SampleFileCount];
                    uint[] Sizes = new uint[Header.SampleFileCount];
                    // uint[] Formats = new uint[Header.SampleFileCount]; // Not doing formats at this time, going by platform instead
                    string[] Names = new string[Header.SampleFileCount];
                    SampleF[] Flags = new SampleF[Header.SampleFileCount];
                    uint[] Rates = new uint[Header.SampleFileCount];
                    for (uint i = 0, hash = 0; i < Header.SoundCount; i++)
                    {
                        hash = reader.ReadUInt32();
                        SoundHashes[reader.ReadUInt32()] = hash;
                    }
                    reader.BaseStream.Position = Header.SoundsOffset; // should be here already
                    for (int i = 0; i < Header.SoundCount; i++)
                    {
                        SIndx[i] = reader.ReadUInt16();
                        reader.BaseStream.Position += 4;
                        SoundFl[i] = reader.ReadByte();
                        reader.BaseStream.Position += 17;
                    }
                    // skip sample hashes and sample file hashes (can be generated)
                    reader.BaseStream.Position = Header.SamplesOffset;
                    for (int i = 0; i < Header.SampleFileCount; i++)
                    {
                        reader.BaseStream.Position += 2; // unknown uint16
                        ushort PitchOrFlags = reader.ReadUInt16();
                        if (PlatIsPS)
                        {
                            Flags[i] = (SampleF)reader.ReadUInt16();
                            double Rate = PitchOrFlags * 44100 / 0x1000;
                            Rates[i] = (uint)(Rate % 1 == 0 ? Rate : Math.Round(Rate / 10.0) * 10);
                        }
                        else
                        {
                            Flags[i] = (SampleF)PitchOrFlags;
                            Rates[i] = reader.ReadUInt32();
                        }
                        reader.BaseStream.Position += PlatIsPS ? 10
                            : Platform is "XBOX" ? 20 : Platform is "XENO" ? 28
                            : 16;
                    }
                    reader.BaseStream.Position = Header.SampleFilesOffset;
                    for (int i = 0; i < Header.SampleFileCount; i++)
                    {
                        Offsets[i] = reader.ReadUInt32();
                        Sizes[i] = reader.ReadUInt32();
                        if (Platform is "GCUB")
                        {
                            _ = reader.ReadBytes(4); // unknown
                        }
                        if (PlatIsMicrosoft)
                        {
                            _ = reader.ReadUInt32(); // Formats[i]
                            if (Platform is "XENO" or "XBOX")
                            {
                                reader.BaseStream.Position += 8;
                            }
                            Names[i] = new string(reader.ReadChars(64)).Trim('\0').TrimEnd();
                        }
                        else
                        {
                            Names[i] = $"{i}.{(Platform is "GCUB" ? "dsp" : "vag")}";
                        }
                    }

                    Lists.Sounds.Clear();
                    Lists.Samples.Clear();
                    if (OutPath == "") { OutPath = Path.GetDirectoryName(Zfile) ?? ""; }
                    string BaseDir = Directory.CreateDirectory($"{OutPath}/{Zname}").FullName;
                    byte[] buffer = new byte[Sizes.Max()];
                    int RealSize = 0;
                    for (int i = 0; i < Header.SampleFileCount; i++)
                    {
                        try
                        {
                            using FileStream ss = new(Path.Combine(BaseDir, Names[i]), FileMode.Create, FileAccess.Write);
                            fs.Position = Offsets[i];
                            RealSize = fs.Read(buffer, 0, (int)Sizes[i]);
                            ss.Write(buffer, 0, RealSize);
                        }
                        catch // (Exception e) > Might need return messages
                        {
                            return false;
                        }
                        Lists.Samples.Add(new JsonSample
                        {
                            File = $"{Zname}/{Names[i]}",
                            Sample_rate = Rates[i],
                            Flags = Flags[i]
                        });
                    }
                    for (int i = 0; i < Header.SoundCount; i++)
                    {
                        Lists.Sounds.Add(new JsonSound()
                        {
                            Sample_index = SIndx[i],
                            Flags = (SoundF)SoundFl[i],
                            Hash = Hashing.ToStr(SoundHashes[i],
                                Path.GetFileNameWithoutExtension(Names[SIndx[i]]).ToUpperInvariant())
                        });
                        //Lists.Sounds.Add(IsXvoice ? (XVSound)sound : (UISound)sound);
                    }
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Save the <see cref="Lists.Sounds"/> and <see cref="Lists.Samples"/> lists as a .zss/.zsm file, as defined by <paramref name="Zfile"/>, using the saved <see cref="Platform"/>.
        /// </summary>
        /// <remarks>Resolves file paths that aren't an absolute path, using <paramref name="RelativePath"/> (defaults to <paramref name="Zfile"/>'s directory).</remarks>
        /// <returns>A message with an error that was encountered, or <see langword="null"/>, if succeeded.</returns>
        public static string? WriteZsnd(string Zfile, string? RelativePath = null)
        {
            // WIP: Possibly combine some loops
            string ZsndName = Path.GetFileNameWithoutExtension(Zfile).ToUpperInvariant();
            RelativePath ??= Path.GetDirectoryName(Zfile)!;
            byte StepSize = (byte)(Platform == ZPlatform.GCUB ? 0x10 : 0x04);
            uint SoundCount = (uint)Lists.Sounds.Count;
            uint SampleCount = Math.Min((uint)Lists.Samples.Count, ushort.MaxValue + 1);
            uint HashesSize = SampleCount * 8; // 2 * uint
            uint HeaderSize = (uint)(8 + Marshal.SizeOf<ZsndHeader>());
            uint soundsOffset = HeaderSize + (SoundCount * 8); // 2 * uint
            uint sampleHOffset = soundsOffset + (SoundCount * 24);
            uint samplesOffset = sampleHOffset + HashesSize;
            uint sampleFHOffset = samplesOffset + (SampleCount * PlatFileHashSz);
            uint sampleFOffset = sampleFHOffset + HashesSize;
            uint InfoSize = sampleFOffset + (SampleCount * PlatFileInfoSz);
            InfoSize += (StepSize - InfoSize % StepSize) % StepSize;

            MemoryStream TemporarySampleStream = new();
            uint[] Offsets = new uint[SampleCount];
            uint[] Sizes = new uint[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                Offsets[i] = InfoSize + (uint)TemporarySampleStream.Position;
                JsonSample S = Lists.Samples[i];
                if (S.File is not null
                    && (Path.IsPathFullyQualified(S.File) ? S.File : Path.Combine(RelativePath, S.File)) is string Filename
                    && File.Exists(Filename))
                {
                    try
                    {
                        using FileStream sfs = new(Filename, FileMode.Open, FileAccess.Read);
                        uint padding = (StepSize - (uint)sfs.Length % StepSize) % StepSize;
                        Sizes[i] = (uint)sfs.Length + padding;
                        sfs.CopyTo(TemporarySampleStream);
                        TemporarySampleStream.Write(new byte[padding]);
                    }
                    catch
                    {
                        TemporarySampleStream.Close();
                        return $"Failed to read {Filename}";
                    }
                }
            }
            Hashing.AddRandomSuffix();
            IOrderedEnumerable<(uint Hash, uint Index)> SortedSoundHashes = Lists.Sounds.Select(
                    (v, i) => (Hash: Hashing.PJW(v.Hash), Index: (uint)i)
                ).OrderBy(i => i.Hash);
            IOrderedEnumerable<(string HashPart, uint Index)> SortedSampleHashes = Lists.Samples.Take((int)SampleCount).Select(
                    (v, i) => (HashPart: $"/{ZsndName}/{Path.GetFileNameWithoutExtension(v.File)?.ToUpperInvariant()}", Index: (uint)i)
                ).OrderBy(i => i.HashPart);

            ZsndHeader Header = new(InfoSize, SoundCount, SampleCount)
            {
                //Size,
                SoundHashesOffset = HeaderSize,
                SoundsOffset = soundsOffset,
                SampleHashesOffset = sampleHOffset,
                SamplesOffset = samplesOffset,
                SampleFileHashesOffset = sampleFHOffset,
                SampleFilesOffset = sampleFOffset
            };
            using FileStream fs = new(Zfile, FileMode.Create, FileAccess.Write);
            using ZWriter zs = new(fs);
            zs.Write(PlatformMagic);
            byte[] headerBytes = new byte[HeaderSize - 8];
            GCHandle handle = GCHandle.Alloc(Header, GCHandleType.Pinned);
            Marshal.Copy(handle.AddrOfPinnedObject(), headerBytes, 0, headerBytes.Length);
            handle.Free();
            if (PlatIs7thGen)
            {
                for (int i = 0; i < HeaderSize - 8; i += 4)
                {
                    (headerBytes[i], headerBytes[i + 1], headerBytes[i + 2], headerBytes[i + 3]) =
                        (headerBytes[i + 3], headerBytes[i + 2], headerBytes[i + 1], headerBytes[i]);
                }
            }
            zs.Write(headerBytes);
            // Debug.Assert(zs.Position == Header.SoundHashesOffset);
            foreach ((uint Hash, uint Index) TH in SortedSoundHashes)
            {
                zs.Write(TH.Hash);
                zs.Write(TH.Index);
            }
            // Debug.Assert(zs.Position == soundsOffset);
            byte[] SoundTemplate = GetSoundTemplate();
            for (int i = 0; i < SoundCount; i++)
            {
                int index = Lists.Sounds[i].SampleIndex;
                if (index <= 0xFFFF)
                {
                    SoundTemplate[0] = (byte)(PlatIs7thGen ? index >> 8 : index & 0xFF);
                    SoundTemplate[1] = (byte)(PlatIs7thGen ? index & 0xFF : index >> 8);
                    SoundTemplate[7] = Lists.Sounds[i].Flags;
                    zs.Write(SoundTemplate);
                }
                else
                {
                    // or raise exception?
                    zs.Write(new byte[24]);
                }
            }
            // Debug.Assert(zs.Position == sampleHOffset);
            foreach ((string HashPart, uint Index) TH in SortedSampleHashes)
            {
                zs.Write(Hashing.PJW($"CHARS3/7R{TH.HashPart}"));
                zs.Write(TH.Index);
            }
            // Debug.Assert(zs.Position == samplesOffset);
            byte[] Padding = new byte[PlatFileHashSz - (PlatIsPS ? 6 : 8)];
            for (ushort i = 0; i < SampleCount; i++)
            {
                zs.Write(i);
                if (PlatIsPS)
                {
                    zs.Write((ushort)Math.Round(Lists.Samples[i].Sample_rate * 0x1000 / 44100.0)); // Rate to pitch
                    zs.Write((ushort)Lists.Samples[i].Flags);
                }
                else
                {
                    zs.Write((ushort)Lists.Samples[i].Flags);
                    zs.Write(Lists.Samples[i].Sample_rate);
                }
                zs.Write(Padding);
            }
            // Debug.Assert(zs.Position == sampleFHOffset);
            foreach ((string HashPart, uint Index) TH in SortedSampleHashes)
            {
                zs.Write(Hashing.PJW($"FILE{TH.HashPart}"));
                zs.Write(TH.Index);
            }
            // Debug.Assert(zs.Position == sampleFOffset);
            Padding = Padding[0..8];
            for (int i = 0; i < SampleCount; i++)
            {
                zs.Write(Offsets[i]);
                zs.Write(Sizes[i]);
                // Might need better format info/support
                if (PlatIsMicrosoft)
                {
                    zs.Write((uint)(Platform == ZPlatform.PC && (Lists.Samples[i].Flags & SampleF.FourChannels) == SampleF.None
                        ? 106 : PlatIsMicrosoft ? 1 : 0));
                    if (Platform != ZPlatform.PC)
                        zs.Write(Padding);
                    zs.Write(Path.GetFileName(Lists.Samples[i].File) ?? "");
                }
                else if (Platform == ZPlatform.GCUB)
                {
                    zs.Write([0x44, 0x53, 0x50, 0x20]);
                }
            }
            // Debug.Assert(zs.Position == InfoSize);
            TemporarySampleStream.Position = 0;
            TemporarySampleStream.CopyTo(fs);
            TemporarySampleStream.Close();
            return null;
        }
    }
}
