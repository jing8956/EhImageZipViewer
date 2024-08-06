using System.Buffers;
using System.Text;
using EhImageZipViewer.Extensions;

namespace EhImageZipViewer.Compression;

internal struct ZipLocalFileHeader
{
    private const uint SignatureConstant = 0x04034b50;
    private const ushort Zip64TagConstant = 1;

    public ushort VersionNeededToExtract;
    public ushort GeneralPurposeBitFlag;
    public ushort CompressionMethod;
    public uint LastModified;
    public uint Crc32;
    public long CompressedSize;
    public long UncompressedSize;
    public ushort FilenameLength;
    public ushort ExtraFieldLength;

    public string Filename;

    public static bool TryRead(ref ReadOnlySequence<byte> buffer, out ZipLocalFileHeader header)
    {
        header = default;

        var reader = new SequenceReader<byte>(buffer);
        if (!reader.TryReadLittleEndian(out uint signature)) return false;
        if (signature != SignatureConstant) return false;

        if (!reader.TryReadLittleEndian(out header.VersionNeededToExtract)) return false;
        if (!reader.TryReadLittleEndian(out header.GeneralPurposeBitFlag)) return false;
        if (!reader.TryReadLittleEndian(out header.CompressionMethod)) return false;
        if (!reader.TryReadLittleEndian(out header.LastModified)) return false;
        if (!reader.TryReadLittleEndian(out header.Crc32)) return false;
        if (!reader.TryReadLittleEndian(out uint compressedSizeSmall)) return false;
        header.CompressedSize = compressedSizeSmall;
        if (!reader.TryReadLittleEndian(out uint uncompressedSizeSmall)) return false;
        header.UncompressedSize = uncompressedSizeSmall;
        if (!reader.TryReadLittleEndian(out header.FilenameLength)) return false;
        if (!reader.TryReadLittleEndian(out header.ExtraFieldLength)) return false;

        if (!reader.TryReadExact(header.FilenameLength, out ReadOnlySequence<byte> filenameSequence)) return false;
        var filenameBytes = filenameSequence.IsSingleSegment ? filenameSequence.FirstSpan : filenameSequence.ToArray();
        header.Filename = Encoding.UTF8.GetString(filenameBytes);

        if (reader.UnreadSequence.Length < header.ExtraFieldLength) return false;
        TryGetZip64BlockFromGenericExtraField(reader.UnreadSequence, ref header);
        reader.Advance(header.ExtraFieldLength);

        buffer = reader.UnreadSequence;
        return true;
    }

    private static void TryGetZip64BlockFromGenericExtraField(in ReadOnlySequence<byte> unreadSeq, ref ZipLocalFileHeader header)
    {
        var uncompressedSizeInZip64 = header.UncompressedSize == uint.MaxValue;
        var compressedSizeInZip64 = header.CompressedSize == uint.MaxValue;

        if (uncompressedSizeInZip64 || compressedSizeInZip64)
        {
            var subSeq = unreadSeq.Slice(0, header.ExtraFieldLength);
            var extraReader = new SequenceReader<byte>(subSeq);

            while (true)
            {
                if (!extraReader.TryReadLittleEndian(out ushort tag)) return;
                if (tag == Zip64TagConstant) break;
                if (!extraReader.TryReadLittleEndian(out ushort offset)) return;
                extraReader.Advance(offset);
            }

            if (!extraReader.TryReadLittleEndian(out ushort dataSize)) return;
            if (dataSize < sizeof(long)) return;

            if (extraReader.UnreadSequence.Length < dataSize) return;
            var zip64Reader = new SequenceReader<byte>(extraReader.UnreadSequence.Slice(0, dataSize));
            var readAllFields = dataSize >= sizeof(long) + sizeof(long);

            if (uncompressedSizeInZip64)
            {
                if (!zip64Reader.TryReadLittleEndian(out header.UncompressedSize)) return;
            }
            else if (readAllFields)
            {
                extraReader.Advance(sizeof(long));
            }

            if (compressedSizeInZip64)
            {
                if (!zip64Reader.TryReadLittleEndian(out header.CompressedSize)) return;
            }
            else if (readAllFields)
            {
                extraReader.Advance(sizeof(long));
            }

            if (header.UncompressedSize < 0) throw new InvalidDataException(SR.FieldTooBigUncompressedSize);
            if (header.CompressedSize < 0) throw new InvalidDataException(SR.FieldTooBigCompressedSize);
        }
    }
}
