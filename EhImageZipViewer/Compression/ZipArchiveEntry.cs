using System.Buffers.Binary;
using System.IO.Compression;
using System.IO.Pipelines;

namespace EhImageZipViewer.Compression;

public class ZipArchiveEntry
{
    private readonly Stream _stream;
    private readonly ZipCentralDirectoryFileHeader _fileHeader;

    internal ZipArchiveEntry(Stream stream, ZipCentralDirectoryFileHeader fileHeader)
    {
        _stream = stream;
        _fileHeader = fileHeader;
    }

    public string FileName => _fileHeader.Filename;
    public long Length => _fileHeader.UncompressedSize;

    public ZipArchiveReadContext OpenRead() => new(_stream, _fileHeader);
}

public readonly struct ZipArchiveReadContext : IAsyncDisposable
{
    public const uint SignatureConstant = 0x04034B50;

    private readonly Stream _stream;
    private readonly long _offsetOfLocalHeader;
    private readonly long _compressedSize;

    private readonly Pipe _pipe;
    private readonly Task _writingTask;

    private readonly Stream _uncompressedStream;

    internal ZipArchiveReadContext(Stream stream, in ZipCentralDirectoryFileHeader fileHeader)
    {
        _stream = stream;
        _offsetOfLocalHeader = fileHeader.RelativeOffsetOfLocalHeader;
        _compressedSize = fileHeader.CompressedSize;

        _pipe = new Pipe();
        _writingTask = Task.Run(FillPipeAsync);

        var compressedStreamToRead = _pipe.Reader.AsStream();
        _uncompressedStream = ((CompressionMethod)fileHeader.CompressionMethod) switch
        {
            CompressionMethod.Deflate => new DeflateStream(compressedStreamToRead, CompressionMode.Decompress),
            CompressionMethod.Stored => compressedStreamToRead,
            _ => throw new NotSupportedException(SR.CompressionMethodNotSupport)
        };
    }

    private async Task FillPipeAsync()
    {
        var writer = _pipe.Writer;
        _stream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);

        try
        {
            const int offsetToFilenameLength = sizeof(uint) + 22;
            const int minimumBufferSize = offsetToFilenameLength + sizeof(ushort) + sizeof(ushort);

            var memory = writer.GetMemory(minimumBufferSize);
            await _stream.ReadExactlyAsync(memory[..minimumBufferSize]);

            var signature = BinaryPrimitives.ReadUInt32LittleEndian(memory.Span);
            if (signature != SignatureConstant) throw new InvalidDataException(SR.LocalFileHeaderCorrupt);

            var subMemory = memory[offsetToFilenameLength..];
            var filenameLength = BinaryPrimitives.ReadUInt16LittleEndian(subMemory.Span);
            var extraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(subMemory.Span[sizeof(ushort)..]);

            _stream.Seek(filenameLength + extraFieldLength, SeekOrigin.Current);
            writer.Advance(0);
        }
        catch (Exception ex)
        {
            await writer.CompleteAsync(ex);
            return;
        }

        var remainingLength = _compressedSize;
        while (remainingLength > 0L)
        {
            var memory = writer.GetMemory(1024 * 1024 * 16);

            try
            {
                long bytesRead = await _stream.ReadAsync(memory);
                if (bytesRead == 0L) throw new InvalidDataException(SR.LocalFileCorrupt);

                bytesRead = long.Min(bytesRead, remainingLength);
                remainingLength -= bytesRead;

                writer.Advance((int)bytesRead);
            }
            catch (Exception ex)
            {
                await writer.CompleteAsync(ex);
                return;
            }

            var result = await writer.FlushAsync();
            if (result.IsCompleted) break;
        }

        await writer.CompleteAsync();
    }

    public Stream UncompressedStream => _uncompressedStream;

    public async ValueTask DisposeAsync()
    {
        await _uncompressedStream.DisposeAsync();
        await _writingTask;
    }

    internal enum CompressionMethod : ushort
    {
        Stored = 0x0,
        Deflate = 0x8,
        Deflate64 = 0x9,
        BZip2 = 0xC,
        LZMA = 0xE
    }
}
