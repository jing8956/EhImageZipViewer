using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

namespace EhImageZipViewer.Compression;

internal struct ZipEndOfCentralDirectoryBlock
{
    public const uint SignatureConstant = 0x06054B50;
    public const int SignatureSize = sizeof(uint);

    public const int SizeOfBlockWithoutSignature = 18;

    public const int ZipFileCommentMaxLength = ushort.MaxValue;

    public uint NumberOfThisDisk;
    public ushort NumberOfTheDiskWithTheStartOfTheCentralDirectory;
    public ushort NumberOfEntriesInTheCentralDirectoryOnThisDisk;
    public long NumberOfEntriesInTheCentralDirectory;
    public uint SizeOfCentralDirectory;
    public long OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
    public string ArchiveComment;

    public static int TryRead(ReadOnlySpan<byte> buffer, out ZipEndOfCentralDirectoryBlock result)
    {
        result = default;
        ReadOnlySpan<byte> signatureSpan = [0x50, 0x4B, 0x05, 0x06];
        var eocdStart = buffer[..^SizeOfBlockWithoutSignature].LastIndexOf(signatureSpan);

        if(eocdStart != -1)
        {
            buffer = buffer[eocdStart..];

            var signature = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            Debug.Assert(signature == SignatureConstant);
            buffer = buffer[sizeof(uint)..];

            result.NumberOfThisDisk = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            buffer = buffer[sizeof(ushort)..];

            result.NumberOfTheDiskWithTheStartOfTheCentralDirectory = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            buffer = buffer[sizeof(ushort)..];

            result.NumberOfEntriesInTheCentralDirectoryOnThisDisk = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            buffer = buffer[sizeof(ushort)..];

            result.NumberOfEntriesInTheCentralDirectory = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            buffer = buffer[sizeof(ushort)..];

            result.SizeOfCentralDirectory = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            buffer = buffer[sizeof(uint)..];

            result.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            buffer = buffer[sizeof(uint)..];

            var commentLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            buffer = buffer[sizeof(ushort)..];

            result.ArchiveComment = System.Text.Encoding.UTF8.GetString(buffer[..commentLength]);
        }

        return eocdStart;
    }
}

internal struct Zip64EndOfCentralDirectoryLocator
{
    public const uint SignatureConstant = 0x07064B50;
    public const int SignatureSize = sizeof(uint);
    public const int SizeOfBlockWithoutSignature = 16;

    public uint NumberOfDiskWithZip64EOCD;
    public ulong OffsetOfZip64EOCD;
    public uint TotalNumberOfDisks;

    public static bool TryRead(ReadOnlySpan<byte> buffer, out Zip64EndOfCentralDirectoryLocator result)
    {
        result = default;

        var signature = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        if (signature != SignatureConstant) return false;
        buffer = buffer[sizeof(uint)..];

        result.NumberOfDiskWithZip64EOCD = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        buffer = buffer[sizeof(uint)..];

        result.OffsetOfZip64EOCD = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        buffer = buffer[sizeof(ulong)..];

        result.TotalNumberOfDisks = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        return true;
    }
}

internal struct Zip64EndOfCentralDirectoryRecord
{
    public const uint SignatureConstant = 0x06064B50;
    public const int SignatureSize = sizeof(uint);
    public const ulong NormalSize = 0x2C; // the size of the data excluding the size/signature fields if no extra data included
    public const int SizeOfBlockWithoutSignature = 0x2C + sizeof(ulong);

    public ulong SizeOfThisRecord;
    public ushort VersionMadeBy;
    public ushort VersionNeededToExtract;
    public uint NumberOfThisDisk;
    public uint NumberOfDiskWithStartOfCD;
    public ulong NumberOfEntriesOnThisDisk;
    public ulong NumberOfEntriesTotal;
    public ulong SizeOfCentralDirectory;
    public ulong OffsetOfCentralDirectory;

    public static bool TryRead(ReadOnlySpan<byte> buffer, out Zip64EndOfCentralDirectoryRecord result)
    {
        result = default;

        var signature = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        if (signature != SignatureConstant) return false;
        buffer = buffer[sizeof(uint)..];

        result.SizeOfThisRecord  = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        buffer = buffer[sizeof(ulong)..];

        result.VersionMadeBy = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        buffer = buffer[sizeof(ushort)..];

        result.VersionNeededToExtract = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        buffer = buffer[sizeof(ushort)..];

        result.NumberOfThisDisk = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        buffer = buffer[sizeof(uint)..];

        result.NumberOfDiskWithStartOfCD = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        buffer = buffer[sizeof(uint)..];

        result.NumberOfEntriesOnThisDisk = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        buffer = buffer[sizeof(ulong)..];

        result.NumberOfEntriesTotal = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        buffer = buffer[sizeof(ulong)..];

        result.SizeOfCentralDirectory = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        buffer = buffer[sizeof(ulong)..];

        result.OffsetOfCentralDirectory = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        return true;
    }
}
