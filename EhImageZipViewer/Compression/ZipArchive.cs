using System.Buffers;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using EhImageZipViewer.Extensions;

namespace EhImageZipViewer.Compression;

public static class ZipArchive
{
    public static async IAsyncEnumerable<ZipArchiveItem> EnumerateAllAsync(Stream stream, IProgress<int> progress,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0));
        var writingTask = FillPipeAsync(stream, pipe.Writer, progress, cancellationToken);
        var reader = pipe.Reader;
        try
        {
            ReadResult result;
            var header = (ZipLocalFileHeader)default;
            do
            {
                const int minimumSizeForHeader = 30;
                var minimumSize = minimumSizeForHeader + (int)header.CompressedSize;

                result = await reader.ReadAtLeastAsync(minimumSize, cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                var subBuffer = buffer;
                while (ZipLocalFileHeader.TryRead(ref subBuffer, out header))
                {
                    var compressedSize = header.CompressedSize;
                    if (subBuffer.Length < compressedSize) break;
                    var dataBlock = subBuffer.Slice(0, compressedSize);

                    var compressedStreamToRead = new ReadOnlySequenceStream(dataBlock);

                    yield return new ZipArchiveItem()
                    {
                        FileName = header.Filename,
                        Content = ((ZipCompressionMethod)header.CompressionMethod) switch
                        {
                            ZipCompressionMethod.Deflate => new DeflateStream(compressedStreamToRead, CompressionMode.Decompress),
                            ZipCompressionMethod.Stored => compressedStreamToRead,
                            _ => throw new NotSupportedException(SR.CompressionMethodNotSupport)
                        }
                    };

                    buffer = subBuffer.Slice(compressedSize);
                    subBuffer = buffer;
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

    private static async Task FillPipeAsync(Stream stream, PipeWriter writer,
        IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        const int minimumBufferSize = 1024 * 1024 * 2;
        while (true)
        {
            var memory = writer.GetMemory(minimumBufferSize);
            try
            {
                var bytesRead = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0) break;
                writer.Advance(bytesRead);
                progress.Report(bytesRead);
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
