using System.Buffers;
using System.Text;
using EhImageZipViewer.Extensions;

namespace EhImageZipViewer.Compression;

public struct ZipCentralDirectoryFileHeader
{
    private const uint SignatureConstant = 0x02014B50;
    private const ushort Zip64TagConstant = 1;

    public byte VersionMadeByCompatibility;
    public byte VersionMadeBySpecification;
    public ushort VersionNeededToExtract;
    public ushort GeneralPurposeBitFlag;
    public ushort CompressionMethod;
    public uint LastModified; // convert this on the fly
    public uint Crc32;
    public long CompressedSize;
    public long UncompressedSize;
    public ushort FilenameLength;
    public ushort ExtraFieldLength;
    public ushort FileCommentLength;
    public uint DiskNumberStart;
    public ushort InternalFileAttributes;
    public uint ExternalFileAttributes;
    public long RelativeOffsetOfLocalHeader;

    public string Filename;
    public string FileComment;
    // public List<ZipGenericExtraField>? ExtraFields;

    public static bool TryRead(ref ReadOnlySequence<byte> buffer, out ZipCentralDirectoryFileHeader header)
    {
        header = default;

        var reader = new SequenceReader<byte>(buffer);
        if (!reader.TryReadLittleEndian(out uint signature)) return false;
        if(signature != SignatureConstant) return false;

        if (!reader.TryRead(out header.VersionMadeBySpecification)) return false;
        if (!reader.TryRead(out header.VersionMadeByCompatibility)) return false;
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
        if (!reader.TryReadLittleEndian(out header.FileCommentLength)) return false;
        if (!reader.TryReadLittleEndian(out ushort diskNumberStartSmall)) return false;
        header.DiskNumberStart = diskNumberStartSmall;
        if (!reader.TryReadLittleEndian(out header.InternalFileAttributes)) return false;
        if (!reader.TryReadLittleEndian(out header.ExternalFileAttributes)) return false;
        if (!reader.TryReadLittleEndian(out uint relativeOffsetOfLocalHeaderSmall)) return false;
        header.RelativeOffsetOfLocalHeader = relativeOffsetOfLocalHeaderSmall;

        if (!reader.TryReadExact(header.FilenameLength, out ReadOnlySequence<byte> filenameSequence)) return false;
        var filenameBytes = filenameSequence.IsSingleSegment ? filenameSequence.FirstSpan : filenameSequence.ToArray();
        header.Filename = Encoding.UTF8.GetString(filenameBytes);

        if (reader.UnreadSequence.Length < header.ExtraFieldLength) return false;
        TryGetZip64BlockFromGenericExtraField(reader.UnreadSequence, ref header);
        reader.Advance(header.ExtraFieldLength);

        if (!reader.TryReadExact(header.FileCommentLength, out ReadOnlySequence<byte> fileCommentSequence)) return false;
        var fileCommentBytes = fileCommentSequence.IsSingleSegment ? fileCommentSequence.FirstSpan : fileCommentSequence.ToArray();
        header.FileComment = Encoding.UTF8.GetString(fileCommentBytes);

        buffer = reader.UnreadSequence;
        return true;
    }

    private static void TryGetZip64BlockFromGenericExtraField(in ReadOnlySequence<byte> unreadSeq, ref ZipCentralDirectoryFileHeader header)
    {
        var uncompressedSizeInZip64 = header.UncompressedSize == uint.MaxValue;
        var compressedSizeInZip64 = header.CompressedSize == uint.MaxValue;
        var relativeOffsetInZip64 = header.RelativeOffsetOfLocalHeader == uint.MaxValue;
        var diskNumberStartInZip64 = header.DiskNumberStart == ushort.MaxValue;

        if (uncompressedSizeInZip64 || compressedSizeInZip64 || relativeOffsetInZip64 || diskNumberStartInZip64)
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
            var readAllFields = dataSize >= sizeof(long) + sizeof(long) + sizeof(long) + sizeof(int);

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

            if (relativeOffsetInZip64)
            {
                if (!zip64Reader.TryReadLittleEndian(out header.RelativeOffsetOfLocalHeader)) return;
            }
            else if (readAllFields)
            {
                extraReader.Advance(sizeof(long));
            }

            if (diskNumberStartInZip64)
            {
                if (!zip64Reader.TryReadLittleEndian(out header.DiskNumberStart)) return;
            }
            else if (readAllFields)
            {
                extraReader.Advance(sizeof(uint));
            }

            if (header.UncompressedSize < 0) throw new InvalidDataException(SR.FieldTooBigUncompressedSize);
            if (header.CompressedSize < 0) throw new InvalidDataException(SR.FieldTooBigCompressedSize);
            if (header.RelativeOffsetOfLocalHeader < 0) throw new InvalidDataException(SR.FieldTooBigLocalHeaderOffset);
        }
    }
}
