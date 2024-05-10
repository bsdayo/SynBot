using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SynBot.Utilities;

public static class ByteExtension
{
    public static string Hex(this byte[] bytes, bool lower = false, bool space = false)
    {
        var data = (ReadOnlySpan<byte>)bytes.AsSpan();
        return space
            ? HexInternal<WithSpaceHexByteStruct>(data, lower)
            : HexInternal<NoSpaceHexByteStruct>(data, lower);
    }

    private static string HexInternal<TStruct>(ReadOnlySpan<byte> bytes, bool lower)
        where TStruct : struct, IHexByteStruct
    {
        if (bytes.Length == 0) return string.Empty;

        var casing = lower ? 0x200020u : 0;
        var structSize = Marshal.SizeOf<TStruct>();
        if (structSize % 2 == 1)
            throw new ArgumentException($"{nameof(TStruct)}'s size of must be a multiple of 2, currently {structSize}");

        var charCountPerByte = structSize / 2; // 2 is the size of char
        var result = new string('\0', bytes.Length * charCountPerByte);
        var resultSpan = MemoryMarshal.CreateSpan(
            ref Unsafe.As<char, TStruct>(ref Unsafe.AsRef(in result.GetPinnableReference())), bytes.Length);

        for (var i = 0; i < bytes.Length; i++) resultSpan[i].Write(ToCharsBuffer(bytes[i], casing));

        return result;
    }

    // By Executor-Cheng https://github.com/KonataDev/Lagrange.Core/pull/344#pullrequestreview-2027515322
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ToCharsBuffer(byte value, uint casing = 0)
    {
        var difference = BitConverter.IsLittleEndian
            ? ((uint)value >> 4) + ((value & 0x0Fu) << 16) - 0x890089u
            : ((value & 0xF0u) << 12) + (value & 0x0Fu) - 0x890089u;
        var packedResult = ((((uint)-(int)difference & 0x700070u) >> 4) + difference + 0xB900B9u) | casing;
        return packedResult;
    }

    public static byte[] UnHex(this string hex)
    {
        if (hex.Length % 2 != 0) throw new ArgumentException("Invalid hex string");

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length; i += 2) bytes[i / 2] = byte.Parse(hex.Substring(i, 2), NumberStyles.HexNumber);
        return bytes;
    }

    private interface IHexByteStruct
    {
        void Write(uint hexChar);
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Size = 6)]
    private struct WithSpaceHexByteStruct : IHexByteStruct
    {
        [FieldOffset(0)]
        public uint twoChar;

        [FieldOffset(4)]
        public char space;

        public void Write(uint hexChar)
        {
            twoChar = hexChar;
            space = ' ';
        }
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Size = 4)]
    private struct NoSpaceHexByteStruct : IHexByteStruct
    {
        [FieldOffset(0)]
        public uint twoChar;

        public void Write(uint hexChar)
        {
            twoChar = hexChar;
        }
    }
}