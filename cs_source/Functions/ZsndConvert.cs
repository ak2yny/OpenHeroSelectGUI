using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Zsnd.Lib;

namespace OpenHeroSelectGUI.Functions
{
    /// <summary>
    /// RIFF header as used by Microsoft digital sound files (WAVE, XNA formats, etc.), focused on the WAVE format.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PCM_RIFF_Header
    {
        public uint Riff;
        public uint FileSize;
        public uint RiffType;
        public uint Fmt;
        public uint FmtSize;
        public ushort AudioFormat;
        public ushort Channels;
        public uint SampleRate;
        public uint ByteRate;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        // other data depending on format
        public uint Data;
        public uint DataSize;

        public static uint FMT => 0x20746d66;
        public static uint DATA => 0x61746164;
        public readonly bool IsWave => Riff == SoundIDs.RIFF && RiffType == 0x45564157;
        public readonly bool FmtCheck => Data == DATA;

        public PCM_RIFF_Header()
        {
            Riff = SoundIDs.RIFF;
            RiffType = 0x45564157; // "WAVE"
            Fmt = FMT;
            Data = DATA;
        }
        /// <summary>
        /// Parse data from a <paramref name="stream"/> as <see cref="PCM_RIFF_Header"/>.
        /// </summary>
        /// <remarks>Advances the stream by the size of <see cref="PCM_RIFF_Header"/>.</remarks>
        /// <returns>The data from the <paramref name="stream"/> as <see cref="PCM_RIFF_Header"/>, if the <paramref name="stream"/> provides sufficient data, otherwise <see langword="null"/>.</returns>
        public static PCM_RIFF_Header? FromStream(FileStream stream)
        {
            int headerSize = Marshal.SizeOf<PCM_RIFF_Header>();
            byte[] headerBytes = new byte[headerSize];
            return stream.Read(headerBytes, 0, headerSize) == headerSize ? MemoryMarshal.Read<PCM_RIFF_Header>(headerBytes.AsSpan()) : null;
            // Or instead of returning null, throw new EndOfStreamException("Failed to read the WAVE header.");
        }
        /// <summary>
        /// Write the data from this <see cref="PCM_RIFF_Header"/> instance to a <paramref name="stream"/>.
        /// </summary>
        /// <remarks>Advances the stream by the size of <see cref="PCM_RIFF_Header"/>.</remarks>
        public readonly void ToStream(FileStream stream)
        {
            int headerSize = Marshal.SizeOf<PCM_RIFF_Header>();
            byte[] headerBytes = new byte[headerSize];
            GCHandle handle = GCHandle.Alloc(this, GCHandleType.Pinned);
            try
            {
                // Writing directly to a file can only be used in an unsafe context CS0214
                // stream.Write(new ReadOnlySpan<byte>(handle.AddrOfPinnedObject().ToPointer(), Marshal.SizeOf<PCM_RIFF_Header>()));
                Marshal.Copy(handle.AddrOfPinnedObject(), headerBytes, 0, headerSize);
                stream.Write(headerBytes, 0, headerSize);
            }
            finally
            {
                handle.Free();
            }
        }
        /// <summary>
        /// Check if the <see cref="PCM_RIFF_Header"/> values match a valid 16bit header, including data sizes, according to the <paramref name="stream"/>.
        /// </summary>
        /// <returns> Whether the data looks valid.</returns>
        public readonly bool IsValid(FileStream stream)
        {
            return AudioFormat == 1 && Channels > 0 && SampleRate > 0 // little endian check
                   && BitsPerSample == 16 // compatibility check
                   && FmtCheck && FileSize < stream.Length // valid stream check
                   && DataSize > 1 && stream.Position + DataSize <= FileSize + 8; // valid data check
        }
    }
    /// <summary>
    /// XMA per sample sub-header for the XMA RIFF header as used by Microsoft's .xma sound format for consoles.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct XMA_Stream_Header
    {
        public uint Unknown; // Related to size
        public uint SampleRate;
        public uint LoopStart;
        public uint LoopEnd;
        public byte LoopSubFrame;
        public byte Channels;
        public ushort UnknownFlag;
    }
    /// <summary>
    /// XMA RIFF header as used by Microsoft's .xma sound format for consoles.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XMA_RIFF_Header
    {
        public uint Riff;
        public uint FileSize;
        public uint RiffType;
        public uint Fmt;
        public uint FmtSize;
        public ushort AudioFormat;
        public ushort Unk2;
        public uint Unk4;
        public ushort NumStreams;
        public byte Loop;
        public byte UnkB;

        public uint SampleRate { get; set; }
        public uint Channels { get; set; }
        public bool IsValid { get; set; }

        /// <summary>
        /// Parse data from a <paramref name="stream"/> as <see cref="XMA_RIFF_Header"/>.
        /// </summary>
        /// <remarks>Advances the stream by the size of <see cref="XMA_RIFF_Header"/>.</remarks>
        /// <returns>The data from the <paramref name="stream"/> as <see cref="XMA_RIFF_Header"/>, if the <paramref name="stream"/> provides valid data, otherwise <see langword="null"/>.</returns>
        public static XMA_RIFF_Header? FromStream(FileStream stream)
        {
            int headerSize = Marshal.SizeOf<XMA_RIFF_Header>();
            byte[] headerBytes = new byte[headerSize];
            if (stream.Read(headerBytes, 0, headerSize) == headerSize)
            {
                XMA_RIFF_Header header = MemoryMarshal.Read<XMA_RIFF_Header>(headerBytes.AsSpan());
                if (header.Riff == 0x46464952 && header.RiffType == 0x45564157 && header.Fmt == 0x20746d66) // RIFF WAVEfmt check
                {
                    for (int i = 0; i < header.NumStreams; i++)
                    {
                        int SHSZ = Marshal.SizeOf<XMA_Stream_Header>();
                        if (stream.Read(headerBytes, 0, SHSZ) != SHSZ) { return null; }
                        XMA_Stream_Header SH = MemoryMarshal.Read<XMA_Stream_Header>(headerBytes.AsSpan(0, SHSZ));
                        header.Channels += SH.Channels;
                        header.SampleRate = SH.SampleRate;
                    }
                    header.IsValid = header.AudioFormat == 0x0165 && header.Channels > 0 && header.SampleRate > 0 // little endian check
                                     && header.FileSize < stream.Length; // valid stream check
                    return header;
                }
            }
            return null;
        }
    }
    /// <summary>
    /// Essential header info as used by Sony Playstation's .vag sound format for the PS2, PS3 and PSP consoles.
    /// </summary>
    public struct VagHeader(uint Size = 0, uint Rate = 0, string Name = "")
    {
        public uint Size = Size;
        public uint SampleRate = Rate;
        public string FileName = Name;

        /// <summary>
        /// Read essential data from a <paramref name="stream"/> and save it to this <see cref="VagHeader"/>.
        /// </summary>
        /// <remarks>Advances the stream by 48 (complete header size).</remarks>
        /// <returns><see langword="True"/>, if the <paramref name="stream"/> length is sufficient for the header and parsed data size; otherwise <see langword="false"/>.</returns>
        public bool FromStream(FileStream stream)
        {
            byte[] buffer = new byte[0x30];
            if (stream.Read(buffer, 0, buffer.Length) == 0x30 && BinaryPrimitives.ReadUInt32LittleEndian(buffer) == SoundIDs.VAG)
            {
                Size = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(12, 4));
                SampleRate = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(16, 4));
                // don't need file name for Zsnd purposes
                return stream.Length - 0x30 >= Size;
            }
            return false;
        }
        /// <summary>
        /// Write the data from this <see cref="VagHeader"/> instance to a <paramref name="stream"/>, filling the missing info with default values.
        /// </summary>
        /// <remarks>Advances the stream by 48 (complete header size).</remarks>
        public readonly void ToStream(FileStream stream)
        {
            byte[] buffer = new byte[0x30]; // alt.: Encoding.ASCII.GetBytes("VAGp", 0, 4, buffer, 0);
            (buffer[0], buffer[1], buffer[2], buffer[3]) = ((byte)'V', (byte)'A', (byte)'G', (byte)'p');
            buffer[7] = 0x20;
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(12, 4), Size);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(16, 4), SampleRate);
            _ = Encoding.UTF8.GetBytes(FileName, 0, Math.Min(FileName.Length, 0x10), buffer, 0x20);
            stream.Write(buffer, 0, 0x30);
        }
    }
    /// <summary>
    /// Minimalistic class with sound ID constants
    /// </summary>
    internal static class SoundIDs
    {
        private static readonly byte[] _magic = new byte[4];
        public static readonly uint RIFF = 0x46464952; // "RIFF" LE
        public static readonly uint VAG = 0x70474156; // "VAGp" LE

        /// <summary>
        /// Reads the "magic" ID from a <see cref="FileStream"/> as <see cref="uint"/> and checks if it's a known ID. <paramref name="fs"/> must be at ID position (0) and position is advanced by 4.
        /// </summary>
        /// <returns><see langword="True"/>, if the read ID is known, otherwise, <see langword="false"/>.</returns>
        public static bool Contains(FileStream fs)
        {
            fs.ReadExactly(_magic);
            uint magic = BinaryPrimitives.ReadUInt32LittleEndian(_magic);
            return magic == RIFF || magic == VAG;
        }
    }
    /// <summary>
    /// Conversion class from and to .zss/.zsm data.
    /// </summary>
    internal static class ZsndConvert
    {
        /// <summary>
        /// Convert a Zsnd sound file (.zss/.zsm entry) to a standard file in a specific <paramref name="Format"/>, according to the <paramref name="SampleInfo"/>.
        /// </summary>
        /// <param name="SourcePath">The Zsnd sound file (.zss/.zsm entry)</param>
        /// <returns>The path of itself (<paramref name="SourcePath"/>) if it's already a readable .wav file, otherwise the path to a temporary file in <see cref="OHSpath.Temp"/> or the <paramref name="SourcePath"/> with a <paramref name="Format"/> suffix.</returns>
        public static string? To(JsonSample SampleInfo, string SourcePath, string Format)
        {
            return To(SampleInfo, SourcePath) is string TargetPath
                ? Format == ".wav" && Path.GetExtension(TargetPath).ToLowerInvariant() is ".wav" or ".xbadpcm"
                ? TargetPath
                : Util.RunExeInCmd(Path.Combine(OHSpath.CD, "vgmstream", "vgmstream-cli.exe"), Format == ".wav" ? TargetPath : $"{TargetPath} -o ?f{Format}")
                ? $"{TargetPath}{Format}"
                : null : null;
        }
        /// <summary>
        /// Convert a Zsnd sound file (.zss/.zsm entry) to a standard file, according to the <paramref name="SampleInfo"/>.
        /// </summary>
        /// <param name="SourcePath">The Zsnd sound file (.zss/.zsm entry)</param>
        /// <returns>The path of itself (<paramref name="SourcePath"/>) if it's already a readable .wav file, otherwise the path to a temporary file in <see cref="OHSpath.Temp"/> or <see langword="null"/> (if conversion fails or isn't supported).</returns>
        public static string? To(JsonSample SampleInfo, string SourcePath)
        {
            try
            {
                using FileStream fs = new(SourcePath, FileMode.Open, FileAccess.Read);
                if (SoundIDs.Contains(fs)) { return SourcePath; }
                fs.Position = 0;
                string Ext = Path.GetExtension(SourcePath).ToLowerInvariant();
                using FileStream? tfs = Ext is ".xbadpcm" or ".wav" or ".vag"
                    ? new(Path.Combine(OHSpath.Temp, Path.GetFileName(SourcePath)), FileMode.Create, FileAccess.Write) : null;
                switch (Ext)
                {
                    case ".xbadpcm":
                        XNA_ADPCM.Decode(fs, tfs!,
                            (ushort)(SampleInfo.Flags.HasFlag(Properties.SampleF.Stereo) ? SampleInfo.Flags.HasFlag(Properties.SampleF.AmbientEmbedded) ? 4 : 2 : 1),
                            SampleInfo.Sample_rate, 0x20);
                        return tfs!.Name;
                    case ".wav":
                        XNA_ADPCM.Decode(fs, tfs!,
                            (ushort)(SampleInfo.Flags.HasFlag(Properties.SampleF.Stereo) ? SampleInfo.Flags.HasFlag(Properties.SampleF.AmbientEmbedded) ? 4 : 2 : 1),
                            SampleInfo.Sample_rate);
                        return tfs!.Name;
                    case ".vag":
                        VagHeader Header = new((uint)fs.Length - 0x30, SampleInfo.Sample_rate, Path.GetFileName(SourcePath));
                        Header.ToStream(tfs!);
                        fs.CopyTo(tfs!);
                        return tfs!.Name;
                    case ".dsp": // Don't have magic but no conversion necessary
                        return SourcePath;
                    case ".xma": // XMA should always be RIFF
                    default: // unknown format
                        return null;
                }
            }
            catch { return null; }
        }
        /// <summary>
        /// Convert a standard sound file to a file to be used in Zsnd files (.zss/.zsm), according to the lowercase <paramref name="Ext"/>ension, and write the info to <paramref name="SampleInfo"/>.
        /// </summary>
        /// <param name="SamplePath">The standard sound file</param>
        /// <returns>A <see cref="byte"/> array with the converted data (empty if conversion fails).</returns>
        public static byte[] From(string Ext, string SamplePath, JsonSample SampleInfo)
        {
            try
            {
                return Ext.Equals(".wav", StringComparison.OrdinalIgnoreCase) && Properties.Platform is Properties.ZPlatform.PC or Properties.ZPlatform.XBOX
                       ? XNA_ADPCM.Encode(SamplePath, SampleInfo, Properties.Platform is Properties.ZPlatform.PC ? 0 : 0x20)
                       : Ext.Equals(".xbadpcm", StringComparison.OrdinalIgnoreCase) && Properties.Platform is Properties.ZPlatform.XBOX
                       ? XboxHeaderStrip(SamplePath, SampleInfo)
                       : Ext.Equals(".vag", StringComparison.OrdinalIgnoreCase) && Properties.PlatIsPS
                       ? VagHeaderStrip(SamplePath, SampleInfo)
                       : Ext.Equals(".dsp", StringComparison.OrdinalIgnoreCase) && Properties.Platform is Properties.ZPlatform.GCUB
                       ? DspRead(SamplePath, SampleInfo)
                       : Ext.Equals(".xma", StringComparison.OrdinalIgnoreCase) && Properties.Platform is Properties.ZPlatform.XENO
                       ? XmaRead(SamplePath, SampleInfo)
                       : [];
                // Note: vgmstream can ONLY convert TO WAV. Might add other formats in the future
                //       vgmstream supports returning data to stdout with the -p flag, but format is unknown, and we prefer the info
            }
            catch { return []; }
        }

        private static byte[] VagHeaderStrip(string SamplePath, JsonSample SampleInfo)
        {
            VagHeader Header = new();
            using FileStream fs = new(SamplePath, FileMode.Open, FileAccess.Read);
            if (Header.FromStream(fs))
            {
                byte[] Samples = new byte[Header.Size];
                if (fs.Read(Samples, 0, Samples.Length) == Header.Size)
                {
                    // Note: VAG are always mono, as the official format doesn't support stereo
                    SampleInfo.Sample_rate = Header.SampleRate;
                    return Samples;
                }
            }
            return [];
        }

        private static byte[] XboxHeaderStrip(string SamplePath, JsonSample SampleInfo)
        {
            // Universal WAV header strip, but with AudioFormat == 0x69 check
            using FileStream fs = new(SamplePath, FileMode.Open, FileAccess.Read);
            PCM_WAVE_Reader WAV = new(fs);
            if (WAV.Header.AudioFormat == 0x69)
            {
                byte[] Samples = new byte[WAV.Header.DataSize];
                if (fs.Read(Samples, 0, (int)WAV.Header.DataSize) == WAV.Header.DataSize)
                {
                    SampleInfo.Sample_rate = WAV.Header.SampleRate;
                    if (WAV.Header.Channels > 1) { SampleInfo.Flags |= WAV.Header.Channels > 3 ? Properties.SampleF.FourChannels : Properties.SampleF.Stereo; }
                    return Samples;
                }
            }
            return [];
        }

        private static byte[] DspRead(string SamplePath, JsonSample SampleInfo)
        {
            using FileStream fs = new(SamplePath, FileMode.Open, FileAccess.Read);
            byte[] DSP = new byte[fs.Length];
            fs.ReadExactly(DSP);
            // Sample count should match the channel data length, but this is just a quick way to guess the channels
            float Channels = (float)fs.Length / BinaryPrimitives.ReadUInt32BigEndian(DSP.AsSpan(0, 4));
            SampleInfo.Sample_rate = BinaryPrimitives.ReadUInt32BigEndian(DSP.AsSpan(8, 4));
            // Unknown if the Zsnd format supports more than 2 channel DSP or if the flags are accurate
            if (Channels > 1) { SampleInfo.Flags |= Channels > 3 ? Properties.SampleF.FourChannels : Properties.SampleF.Stereo; }
            if (DSP[0xD] == 1) { SampleInfo.Flags |= Properties.SampleF.Loop; } // otherwise should be 0
            return DSP;
        }

        private static byte[] XmaRead(string SamplePath, JsonSample SampleInfo)
        {
            // https://stackoverflow.com/questions/70992562/c-xbox360-application-xaudio2-playing-a-xma-sound
            using FileStream fs = new(SamplePath, FileMode.Open, FileAccess.Read);
            if (XMA_RIFF_Header.FromStream(fs) is XMA_RIFF_Header header && header.IsValid)
            {
                SampleInfo.Sample_rate = header.SampleRate;
                if (header.Channels > 1) { SampleInfo.Flags |= header.Channels > 3 ? Properties.SampleF.FourChannels : Properties.SampleF.Stereo; }
                if (header.Loop > 0) { SampleInfo.Flags |= Properties.SampleF.Loop; }
                fs.Position = 0;
                byte[] XMA = new byte[fs.Length];
                fs.ReadExactly(XMA);
                return XMA;
            }
            return [];
        }
    }

    internal class PCM_WAVE_Reader
    {
        private readonly FileStream _stream;
        private readonly byte[] _buffer = new byte[8];

        public readonly PCM_RIFF_Header Header;
        public readonly bool IsValid;

        public PCM_WAVE_Reader(FileStream fs)
        {
            _stream = fs;

            if (PCM_RIFF_Header.FromStream(fs) is PCM_RIFF_Header header && header.IsWave && header.Fmt == PCM_RIFF_Header.FMT)
            {
                // Handle special formats and header info, but only if the first chunk is fmt (is this guaranteed?)
                fs.Position = 20 + header.FmtSize;
                while (fs.Read(_buffer, 0, 8) == 8)
                {
                    uint chunkId = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(0, 4));
                    uint chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(4, 4));
                    if (fs.Position + chunkSize > header.FileSize + 8)
                    {
                        break;
                    }
                    else if (chunkId == PCM_RIFF_Header.DATA)
                    {
                        header.DataSize = chunkSize;
                        break; // Stop reading after the 'data' chunk, so the _stream pos is at the data
                    }
                    _ = fs.Seek(chunkSize, SeekOrigin.Current);
                }
                IsValid = header.IsValid(fs);
                Header = header;
            }
        }
        /// <summary>
        /// Calls <see cref="BinaryPrimitives.ReadUInt16LittleEndian"/> using the <see cref="_stream"/> at the current position (doesn't handle end of data!).
        /// </summary>
        /// <returns>The <see cref="short"/> as returned by <see cref="BinaryPrimitives.ReadUInt16LittleEndian"/>.</returns>
        public short ReadSample()
        {
            _stream.ReadExactly(_buffer.AsSpan(0, 2));
            return BinaryPrimitives.ReadInt16LittleEndian(_buffer);
        }
    }

    internal class IMAState
    {
        public short PredictedSample { get; set => field = Math.Clamp(value, short.MinValue, short.MaxValue); }
        public int StepIndex { get; set => field = Math.Clamp(value, 0, 88); }
    }
    /// <summary>
    /// Static methods for encoding and decoding audio data using the XNA ADPCM (Adaptive Differential Pulse Code Modulation) format.
    /// </summary>
    /// <remarks>Based on RavenAudio <see href="https://github.com/nikita488/ravenAudio/blob/master/src/main.cpp"/>. Thread safety is not guaranteed.</remarks>
    internal static class XNA_ADPCM
    {
        private static readonly short[] StepSizes =
        [
        7, 8, 9, 10, 11, 12, 13, 14,
        16, 17, 19, 21, 23, 25, 28, 31,
        34, 37, 41, 45, 50, 55, 60, 66,
        73, 80, 88, 97, 107, 118, 130, 143,
        157, 173, 190, 209, 230, 253, 279, 307,
        337, 371, 408, 449, 494, 544, 598, 658,
        724, 796, 876, 963, 1060, 1166, 1282, 1411,
        1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024,
        3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484,
        7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
        15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794,
        32767
        ];

        // https://github.com/Sergeanur/XboxADPCM/blob/767dc2640f8de4ac1f4fd6badfd13a402b3d1713/XboxADPCM/ImaADPCM.cpp#L67
        private static readonly sbyte[] StepIndices = [-1, -1, -1, -1, 2, 4, 6, 8];

        private static readonly uint MusicBlockSize = 0x4000;
        private static readonly uint HalfBlockSize = MusicBlockSize / 2;
        private static short StepSize;
        private static IMAState[] States = [];

        public static void Decode(FileStream In, FileStream Out, ushort Channels, uint SampleRate, int BlockSize = 0)
        {
            uint Size = (uint)(Channels == 4 ? In.Length - MusicBlockSize + (In.Length - (In.Length / MusicBlockSize * MusicBlockSize)) : In.Length);
            byte[] Samples = new byte[In.Length];
            In.ReadExactly(Samples);
            byte[] DecodedSamples = new byte[Size * 4];
            int x = 0;
            if (BlockSize > 0)
            {
                States = new IMAState[Channels];
                for (byte i = 0; i < Channels; i++) { States[i] = new IMAState(); }
                // Microsoft XNA Game Studio [XNB]: First 4 bytes of a block contain initialization information.
                // https://stackoverflow.com/questions/9541471/problems-converting-adpcm-to-pcm-in-xna
                BlockSize += 4;
                for (int i = 0; i < In.Length / Channels; i += BlockSize)
                {
                    for (uint ch = 0; ch < Channels; ch++)
                    {
                        States[ch].PredictedSample = (short)(Samples[i] | Samples[i + 1] << 8);
                        States[ch].StepIndex = Samples[i + 2];
                        DecodedSamples[x++] = Samples[i];
                        DecodedSamples[x++] = Samples[i + 1];
                    }
                    for (int j = (i + 4) * Channels; j < (i + BlockSize) * Channels;)
                    {
                        for (byte ch = 0; ch < Channels; ch++, j++)
                        {
                            for (uint k = 0; k < 2; k++)
                            {
                                Decode((byte)((k == 0 ? Samples[j] : Samples[j] >> 4) & 0xF), ch);
                                // Store 16 bit PCM sample short as two (of four) bytes into output byte array
                                // Note: Using byte is faster than ushort and conversion, especially since BinaryFormatter should no longer be used
                                DecodedSamples[x++] = (byte)(States[ch].PredictedSample & 0xFF);
                                DecodedSamples[x++] = (byte)(States[ch].PredictedSample >> 8);
                            }
                        }
                    }
                }
                DecodedSamples = DecodedSamples[..x];
            }
            if (Channels == 1)
            {
                States = [new IMAState()];
                for (int i = 0; i < Size; i++)
                {
                    for (byte ch = 0; ch < 2; ch++)
                    {
                        Decode((byte)((ch == 0 ? Samples[i] : Samples[i] >> 4) & 0xF));
                        DecodedSamples[x++] = (byte)(States[0].PredictedSample & 0xFF);
                        DecodedSamples[x++] = (byte)(States[0].PredictedSample >> 8);
                    }
                }
            }
            else if (Channels == 2)
            {
                States = [new IMAState(), new IMAState()];
                for (int i = 0; i < Size; i++)
                {
                    for (byte ch = 0; ch < 2; ch++)
                    {
                        Decode((byte)((ch == 0 ? Samples[i] : Samples[i] >> 4) & 0xF), ch);
                        DecodedSamples[x++] = (byte)(States[ch].PredictedSample & 0xFF);
                        DecodedSamples[x++] = (byte)(States[ch].PredictedSample >> 8);
                    }
                }
            }
            else if (Channels == 4)
            {
                States = [new IMAState(), new IMAState(), new IMAState(), new IMAState()];
                uint size = (uint)Samples.Length;
                for (uint BI = 0; BI < size; BI += MusicBlockSize)
                {
                    uint LastIPerTrack = BI + Math.Min(MusicBlockSize, size - BI) - HalfBlockSize;
                    for (uint i = BI; i < LastIPerTrack; i++)
                    {
                        for (uint j = 0, t = i; j < 4; j += 2, t += HalfBlockSize)
                        {
                            for (byte ch = (byte)j; ch < j + 2; ch++)
                            {
                                Decode((byte)((ch % 2 == 0 ? Samples[t] : Samples[t] >> 4) & 0xF), ch);
                                DecodedSamples[x++] = (byte)(States[ch].PredictedSample & 0xFF);
                                DecodedSamples[x++] = (byte)(States[ch].PredictedSample >> 8);
                            }
                        }
                    }
                }
            }
            // else: not implemented, returns empty array (silence)
            PCM_RIFF_Header Header = new()
            {
                FileSize = (uint)(Marshal.SizeOf<PCM_RIFF_Header>() - 8 + x),
                FmtSize = 16, // PCM
                AudioFormat = 1,
                Channels = Channels,
                SampleRate = SampleRate,
                ByteRate = Channels * SampleRate * 2, // BitsPerSample / 8
                BlockAlign = (ushort)(Channels * 2),
                BitsPerSample = 16,
                DataSize = (uint)x
            };
            Header.ToStream(Out);
            Out.Write(DecodedSamples);
        }

        private static void Decode(byte EncodedSample, byte Ch = 0)
        {
            StepSize = StepSizes[States[Ch].StepIndex];
            // originalsample + 0.5 * stepSize / 4 + stepSize / 8 optimization.
            //http://www.cs.columbia.edu/~hgs/audio/dvi/p34.jpg
            //https://github.com/Sergeanur/XboxADPCM/blob/767dc2640f8de4ac1f4fd6badfd13a402b3d1713/XboxADPCM/ImaADPCM.cpp#L20
            int diff = StepSize >> 3;
            if ((EncodedSample & 1) != 0) { diff += StepSize >> 2; }
            if ((EncodedSample & 2) != 0) { diff += StepSize >> 1; }
            if ((EncodedSample & 4) != 0) { diff += StepSize; }
            // seemingly from relative to absolute
            States[Ch].PredictedSample += (short)((EncodedSample & 8) == 0 ? diff : -diff); // signed
            States[Ch].StepIndex += StepIndices[EncodedSample & 7]; // get from half sized StepIndices
        }

        public static byte[] Encode(string SamplePath, JsonSample SampleInfo, int BlockSize = 0)
        {
            using FileStream fs = new(SamplePath, FileMode.Open, FileAccess.Read);
            PCM_WAVE_Reader WAV = new(fs);
            if (WAV.IsValid)
            {
                // Export info
                SampleInfo.Sample_rate = WAV.Header.SampleRate;
                if (WAV.Header.Channels > 1) { SampleInfo.Flags |= WAV.Header.Channels == 4 ? Properties.SampleF.FourChannels : Properties.SampleF.Stereo; }
                else if (BlockSize == 0) { SampleInfo.Format = 106; } // WIP: Other formats?

                int LowSample = 0;
                uint EncodedSize = WAV.Header.DataSize / 4;
                if (BlockSize > 0)
                {
                    int WavBSz = (BlockSize * 4) + 2;
                    long ExtraSz = WAV.Header.DataSize % (WavBSz * WAV.Header.Channels);
                    EncodedSize = (uint)((WAV.Header.DataSize - ExtraSz) * (BlockSize + 4) / WavBSz);
                    if (ExtraSz > 0) { EncodedSize += (uint)((BlockSize + 4) * WAV.Header.Channels); }
                }
                else if (WAV.Header.Channels == 4)
                {
                    EncodedSize += ((EncodedSize + MusicBlockSize) / MusicBlockSize * MusicBlockSize - EncodedSize) / 2;
                }
                byte[] EncodedSamples = new byte[EncodedSize];

                if (BlockSize > 0)
                {
                    States = new IMAState[WAV.Header.Channels];
                    for (byte i = 0; i < WAV.Header.Channels; i++) { States[i] = new IMAState(); }
                    long LastSamplePos = fs.Length - 1;
                    for (uint i = 0; i < EncodedSize && fs.Position < LastSamplePos;)
                    {
                        for (byte ch = 0; ch < WAV.Header.Channels && fs.Position < LastSamplePos; ch++)
                        {
                            States[ch].PredictedSample = WAV.ReadSample();
                            EncodedSamples[i++] = (byte)(States[ch].PredictedSample & 0xFF);
                            EncodedSamples[i++] = (byte)(States[ch].PredictedSample >> 8);
                            EncodedSamples[i++] = (byte)States[ch].StepIndex;
                            i++;
                        }
                        for (int j = 0; j < BlockSize; j++)
                        {
                            for (uint ch = 0; ch < WAV.Header.Channels && fs.Position < LastSamplePos - 2; ch += 2)
                            {
                                LowSample = Encode(WAV.ReadSample(), ch);
                                EncodedSamples[i++] = (byte)((Encode(WAV.ReadSample(), (uint)(WAV.Header.Channels == 1 ? 0 : ch + 1)) << 4) | LowSample);
                            }
                        }
                    }
                }
                if (WAV.Header.Channels == 1)
                {
                    States = [new IMAState()];
                    for (uint i = 0; i < EncodedSize; i++)
                    {
                        LowSample = Encode(WAV.ReadSample());
                        EncodedSamples[i] = (byte)((Encode(WAV.ReadSample()) << 4) | LowSample);
                    }
                }
                if (WAV.Header.Channels == 2)
                {
                    States = [new IMAState(), new IMAState()];
                    for (uint i = 0; i < EncodedSize; i++)
                    {
                        LowSample = Encode(WAV.ReadSample(), 0); // Left
                        EncodedSamples[i] = (byte)((Encode(WAV.ReadSample(), 1) << 4) | LowSample);
                    }
                }
                if (WAV.Header.Channels == 4)
                {
                    States = [new IMAState(), new IMAState(), new IMAState(), new IMAState()];
                    for (uint BI = 0; BI < EncodedSize; BI += MusicBlockSize)
                    {
                        uint LastIPerTrack = BI + Math.Min(MusicBlockSize, EncodedSize - BI) - HalfBlockSize;
                        for (uint i = BI; i < LastIPerTrack; i++)
                        {
                            for (uint j = 0, t = i; j < 4; j += 2, t += HalfBlockSize)
                            {
                                LowSample = Encode(WAV.ReadSample(), j);
                                EncodedSamples[t] = (byte)((Encode(WAV.ReadSample(), j + 1) << 4) | LowSample);
                            }
                        }
                    }
                }
                // else: not implemented, returns 0 array (silence)
                return EncodedSamples;
            }
            return [];
        }

        private static byte Encode(short Sample, uint Ch = 0)
        {
            StepSize = StepSizes[States[Ch].StepIndex];
            int sampleDiff = Sample - States[Ch].PredictedSample;
            bool signed = sampleDiff < 0;
            if (signed) { sampleDiff = -sampleDiff; }
            byte EncodedSample = (byte)(signed ? 8 : 0);
            int diff = StepSize >> 3;
            // unlooped for faster performance (https://github.com/Sergeanur/XboxADPCM/blob/767dc2640f8de4ac1f4fd6badfd13a402b3d1713/XboxADPCM/ImaADPCM.cpp#L50)
            if (sampleDiff >= StepSize)
            {
                EncodedSample |= 4;
                sampleDiff -= StepSize;
                diff += StepSize;
            }
            StepSize >>= 1;
            if (sampleDiff >= StepSize)
            {
                EncodedSample |= 2;
                sampleDiff -= StepSize;
                diff += StepSize;
            }
            StepSize >>= 1;
            if (sampleDiff >= StepSize)
            {
                EncodedSample |= 1;
                diff += StepSize;
            }
            // seemingly from absolute to relative (adding difference)
            States[Ch].PredictedSample += (short)(signed ? -diff : diff);
            States[Ch].StepIndex += StepIndices[EncodedSample & 7]; // get from half sized StepIndices
            return EncodedSample;
        }
    }

    public static class TemporaryPlayer
    {
        // https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/interop/how-to-use-platform-invoke-to-play-a-wave-file
        [DllImport("winmm.DLL", EntryPoint = "PlaySound", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        private static extern bool PlaySound(string szSound, IntPtr hMod, PlaySoundFlags flags);

        [Flags]
        private enum PlaySoundFlags
        {
            /// <summary>play synchronously (default)</summary>
            SND_SYNC = 0x0000,
            /// <summary>play asynchronously</summary>
            SND_ASYNC = 0x0001,
            /// <summary>silence (!default) if sound not found</summary>
            SND_NODEFAULT = 0x0002,
            /// <summary>pszSound points to a memory file</summary>
            SND_MEMORY = 0x0004,
            /// <summary>loop the sound until next sndPlaySound</summary>
            SND_LOOP = 0x0008,
            /// <summary>don’t stop any currently playing sound</summary>
            SND_NOSTOP = 0x0010,
            /// <summary>Stop Playing Wave</summary>
            SND_PURGE = 0x40,
            /// <summary>don’t wait if the driver is busy</summary>
            SND_NOWAIT = 0x00002000,
            /// <summary>name is a registry alias</summary>
            SND_ALIAS = 0x00010000,
            /// <summary>alias is a predefined id</summary>
            SND_ALIAS_ID = 0x00110000,
            /// <summary>name is file name</summary>
            SND_FILENAME = 0x00020000,
            /// <summary>name is resource name or atom</summary>
            SND_RESOURCE = 0x00040004
        }

        public static bool Play(string FullPath, JsonSample SampleInfo)
        {
            return ZsndConvert.To(SampleInfo, FullPath, ".wav") is string PlayPath && PlaySound(PlayPath, IntPtr.Zero, PlaySoundFlags.SND_FILENAME | PlaySoundFlags.SND_ASYNC);
        }

    }
}
