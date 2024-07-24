using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using EhImageZipViewer.Extensions;
using static EhImageZipViewer.Compression.ZipArchiveReadContext;

namespace EhImageZipViewer.Compression;

public class ZipArchive : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly ZipCentralDirectoryFileHeader[] _entries;

    internal ZipArchive(Stream stream, ZipCentralDirectoryFileHeader[] entries)
    {
        _stream = stream;
        _entries = entries;
    }

    #region LoadAsync
    public static async ValueTask<ZipArchive> LoadAsync(Stream stream)
    {
        var eocd = await ReadEndOfCentralDirectory(stream).ConfigureAwait(false);
        var entries = await ReadCentralDirectory(stream, eocd).ConfigureAwait(false);
        return new ZipArchive(stream, entries);
    }

    private static async ValueTask<ZipEndOfCentralDirectoryBlock> ReadEndOfCentralDirectory(Stream stream)
    {
        const int bufferSize = 1024 * 80;

        var newPosition = stream.Length - bufferSize;
        if (newPosition < 0) newPosition = 0;
        stream.Position = newPosition;

        using var memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
        var buffer = memoryOwner.Memory[..bufferSize];

        var bytesRead = await stream.ReadAtLeastAsync(buffer, bufferSize, throwOnEndOfStream: false);
        if (bytesRead < bufferSize) throw new IOException(SR.UnexpectedEndOfStream);

        var eocdStart = ZipEndOfCentralDirectoryBlock.TryRead(buffer.Span, out var eocd);
        if (eocdStart == -1) throw new InvalidDataException(SR.EOCDNotFound);

        if (eocd.NumberOfThisDisk != eocd.NumberOfTheDiskWithTheStartOfTheCentralDirectory) throw new InvalidDataException(SR.SplitSpanned);
        if (eocd.NumberOfEntriesInTheCentralDirectory != eocd.NumberOfEntriesInTheCentralDirectoryOnThisDisk) throw new InvalidDataException(SR.SplitSpanned);

        if (eocd.NumberOfThisDisk == ushort.MaxValue ||
            eocd.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber == uint.MaxValue ||
            eocd.NumberOfEntriesInTheCentralDirectory == ushort.MaxValue)
        {
            // Read Zip64 End of Central Directory Locator
            const int zip64eocdLocatorBlockSize = Zip64EndOfCentralDirectoryLocator.SignatureSize + Zip64EndOfCentralDirectoryLocator.SizeOfBlockWithoutSignature;

            Memory<byte> subBuffer;
            if (eocdStart >= zip64eocdLocatorBlockSize)
            {
                var zip64LocatorStart = eocdStart - zip64eocdLocatorBlockSize;
                subBuffer = buffer[zip64LocatorStart..];
            }
            else
            {
                newPosition -= zip64eocdLocatorBlockSize - eocdStart;
                stream.Seek(newPosition, SeekOrigin.Begin);
                bytesRead = await stream.ReadAtLeastAsync(buffer, zip64eocdLocatorBlockSize, throwOnEndOfStream: false);
                if (bytesRead < zip64eocdLocatorBlockSize) throw new IOException(SR.UnexpectedEndOfStream);
                subBuffer = buffer;
            }

            bool zip64eocdLocatorProper = Zip64EndOfCentralDirectoryLocator.TryRead(subBuffer.Span, out var locator);
            if (!zip64eocdLocatorProper) return eocd;
            if (locator.OffsetOfZip64EOCD > long.MaxValue) throw new InvalidDataException(SR.FieldTooBigOffsetToZip64EOCD);

            // Read Zip64 End of Central Directory Record
            const int zip64EocdBlockSize = Zip64EndOfCentralDirectoryRecord.SignatureSize + Zip64EndOfCentralDirectoryRecord.SizeOfBlockWithoutSignature;
            var offsetOfZip64EOCD = (long)locator.OffsetOfZip64EOCD;
            if (newPosition <= offsetOfZip64EOCD)
            {
                var zip64Start = (int)(offsetOfZip64EOCD - newPosition);
                subBuffer = buffer[zip64Start..];
            }
            else
            {
                stream.Seek(offsetOfZip64EOCD, SeekOrigin.Begin);
                bytesRead = await stream.ReadAtLeastAsync(buffer, zip64EocdBlockSize, throwOnEndOfStream: false);
                if (bytesRead < zip64EocdBlockSize) throw new IOException(SR.UnexpectedEndOfStream);
                subBuffer = buffer;
            }

            if (!Zip64EndOfCentralDirectoryRecord.TryRead(subBuffer.Span, out var record)) throw new InvalidDataException(SR.Zip64EOCDNotWhereExpected);

            if (record.NumberOfEntriesTotal > long.MaxValue) throw new InvalidDataException(SR.FieldTooBigNumEntries);
            if (record.OffsetOfCentralDirectory > long.MaxValue) throw new InvalidDataException(SR.FieldTooBigOffsetToCD);
            if (record.NumberOfEntriesTotal != record.NumberOfEntriesOnThisDisk) throw new InvalidDataException(SR.SplitSpanned);

            eocd.NumberOfThisDisk = record.NumberOfThisDisk;
            eocd.NumberOfEntriesInTheCentralDirectory = (long)record.NumberOfEntriesTotal;
            eocd.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = (long)record.OffsetOfCentralDirectory;
        }

        return eocd;
    }

    private static async ValueTask<ZipCentralDirectoryFileHeader[]> ReadCentralDirectory(Stream stream, ZipEndOfCentralDirectoryBlock eocd)
    {
        stream.Seek(eocd.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber, SeekOrigin.Begin);

        var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));

        var targetCount = (int)eocd.NumberOfEntriesInTheCentralDirectory;
        var result = new ZipCentralDirectoryFileHeader[targetCount];
        var currPosition = 0;
        try
        {
            while(currPosition < targetCount)
            {
                var readResult = await reader.ReadAsync();
                var buffer = readResult.Buffer;

                try
                {
                    while (ZipCentralDirectoryFileHeader.TryRead(ref buffer, out result[currPosition]))
                    {
                        currPosition++;
                        if (currPosition == targetCount) break;
                    }
                    
                    if (readResult.IsCompleted)
                    {
                        if (currPosition != targetCount) throw new InvalidDataException(SR.IncompleteMessage);
                        break;
                    }
                }
                finally
                {
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }

        return result;
    }
    #endregion

    public IReadOnlyList<ZipCentralDirectoryFileHeader> Entries => _entries;

    public async IAsyncEnumerable<ZipArchiveReadAllReadAllItem> EnumerateAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_entries.Length == 0) yield break;
        var headers = new Queue<ZipCentralDirectoryFileHeader>(_entries.OrderBy(e => e.RelativeOffsetOfLocalHeader));
        var header = headers.Dequeue();

        var position = header.RelativeOffsetOfLocalHeader;
        _stream.Seek(position, SeekOrigin.Begin);

        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0));
        var writingTask = FillPipeAsync(_stream, pipe.Writer, cancellationToken);
        var reader = pipe.Reader;
        try
        {
            ReadResult result;
            do
            {
                const int minimumSizeForHeader = 30;
                var minimumSize = minimumSizeForHeader + (int)header.CompressedSize;
                result = await reader.ReadAtLeastAsync(minimumSize, cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;
                
                while(TrySkipHeaderBlock(buffer, out var offset, out ReadOnlySequence<byte> next))
                {
                    var compressedSize = header.CompressedSize;
                    if (next.Length < compressedSize) break;
                    var dataBlock = next.Slice(0, compressedSize);

                    if (position == header.RelativeOffsetOfLocalHeader)
                    {
                        var compressedStreamToRead = new ReadOnlySequenceStream(dataBlock);

                        yield return new ZipArchiveReadAllReadAllItem()
                        {
                            FileName = header.Filename,
                            Length = header.UncompressedSize,
                            Content = ((CompressionMethod)header.CompressionMethod) switch
                            {
                                CompressionMethod.Deflate => new DeflateStream(compressedStreamToRead, CompressionMode.Decompress),
                                CompressionMethod.Stored => compressedStreamToRead,
                                _ => throw new NotSupportedException(SR.CompressionMethodNotSupport)
                            }
                        };

                        if (!headers.TryDequeue(out header)) yield break;
                    }
                    else
                    {
                        throw new InvalidDataException("");
                    }

                    position += offset + compressedSize;
                    buffer = buffer.Slice(offset + compressedSize);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
            } while (!result.IsCompleted);
        }
        finally
        {
            await reader.CompleteAsync().ConfigureAwait(false);
            await writingTask.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static bool TrySkipHeaderBlock(in ReadOnlySequence<byte> buffer, out int offset, out ReadOnlySequence<byte> block)
    {
        offset = default;
        block = default;
        
        var reader = new SequenceReader<byte>(buffer);
        if (!reader.TryReadLittleEndian(out uint signature)) return false;
        const uint SignatureConstant = 0x04034B50;
        if (signature != SignatureConstant) return false;

        const int offsetToFilenameLength = 22;
        reader.Advance(offsetToFilenameLength);

        if (!reader.TryReadLittleEndian(out ushort filenameLength)) return false;
        if (!reader.TryReadLittleEndian(out ushort extraFieldLength)) return false;
        reader.Advance(filenameLength + extraFieldLength);

        offset = 30 + filenameLength + extraFieldLength;
        block = reader.UnreadSequence;
        return true;
    }

    private static async Task FillPipeAsync(Stream stream, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        const int minimumBufferSize = 1024 * 1024 * 4;

        while (true)
        {
            var memory = writer.GetMemory(minimumBufferSize);
            try
            {
                var bytesRead = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0) break;
                writer.Advance(bytesRead);
            }
            catch (Exception ex)
            {
                await writer.CompleteAsync(ex).ConfigureAwait(false);
                break;
            }

            var result = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsCompleted) break;
        }

        await writer.CompleteAsync().ConfigureAwait(false);
    }


    #region IAsyncDisposable
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream.Dispose();
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await _stream.DisposeAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }
    #endregion

    private class ReadOnlySequenceStream : Stream
    {
        private ReadOnlySequence<byte> _buffer;

        internal ReadOnlySequenceStream(ReadOnlySequence<byte> buffer) => _buffer = buffer;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;


        public override long Length => _buffer.Length;
        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan().Slice(offset, count));
        public override int Read(Span<byte> buffer)
        {
            var bytesRead = (int)long.Min(buffer.Length, _buffer.Length);
            _buffer.Slice(0, bytesRead).CopyTo(buffer);
            _buffer = _buffer.Slice(bytesRead);
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override void Flush() => throw new NotImplementedException();
    }
}

public readonly struct ZipArchiveReadAllReadAllItem
{
    public required string FileName { get; init; }
    public required long Length { get; init; }
    public required Stream Content { get; init; }
}
