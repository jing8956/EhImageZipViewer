using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EhImageZipViewer.Extensions;

internal static class SequenceReaderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool TryRead<T>(ref this SequenceReader<byte> reader, out T value) where T: unmanaged
    {
        ReadOnlySpan<byte> span = reader.UnreadSpan;
        if (span.Length < sizeof(T)) return TryReadMultisegment(ref reader, out value);

        value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
        reader.Advance(sizeof(T));
        return true;
    }
    private static unsafe bool TryReadMultisegment<T>(ref SequenceReader<byte> reader, out T value) where T : unmanaged
    {
        Debug.Assert(reader.UnreadSpan.Length < sizeof(T));

        // Not enough data in the current segment, try to peek for the data we need.
        T buffer = default;
        var tempSpan = new Span<byte>(&buffer, sizeof(T));

        if (!reader.TryCopyTo(tempSpan))
        {
            value = default;
            return false;
        }

        value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(tempSpan));
        reader.Advance(sizeof(T));
        return true;
    }

    private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out uint value)
    {
        if (reader.TryRead(out value))
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            return true;
        }

        return false;
    }
    /// <summary>
    /// Reads an <see cref="uint"/> as little endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="uint"/>.</returns>
    public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out uint value)
    {
        if (BitConverter.IsLittleEndian)
        {
            return reader.TryRead(out value);
        }

        return TryReadReverseEndianness(ref reader, out value);
    }


    private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out ushort value)
    {
        if (reader.TryRead(out value))
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            return true;
        }

        return false;
    }
    /// <summary>
    /// Reads an <see cref="ushort"/> as little endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="ushort"/>.</returns>
    public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out ushort value)
    {
        if (BitConverter.IsLittleEndian)
        {
            return reader.TryRead(out value);
        }

        return TryReadReverseEndianness(ref reader, out value);
    }
}
