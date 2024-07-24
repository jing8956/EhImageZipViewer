using System;
using System.Threading;

namespace EhImageZipViewer;

public class BackgroundLoadMemoryStream : Stream
{
    private readonly Stream _stream;
    private readonly long _length;
    private readonly bool _leaveOpen;
    private readonly int _bufferSize;
    private readonly Task _loadTask;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<TaskCompletionSource<byte[]>> _buffers;

    private long _position;

    public BackgroundLoadMemoryStream(Stream stream, long? length = null, bool leaveOpen = false, CancellationToken cancellationToken = default)
    {
        _stream = stream;
        _length = length ?? stream.Length;
        _leaveOpen = leaveOpen;

        _bufferSize = 1024 * 80;
        var buffersCapacity = (int)(_length / _bufferSize) + 1;

        _buffers = new(capacity: buffersCapacity);
        for (int i = 0; i < buffersCapacity; i++)
        {
            _buffers.Add(new TaskCompletionSource<byte[]>());
        }

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loadTask = Task.Run(() => loadStream(_cancellationTokenSource.Token), CancellationToken.None);
    }

    private async Task loadStream(CancellationToken cancellationToken)
    {
        try
        {
            await loadBlock(_buffers.Count - 1);
            await loadBlock(_buffers.Count - 2);

            for (int i = 0; i < _buffers.Count - 2; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await loadBlock(i);
            }
        }
        catch (Exception ex)
        {
            foreach (var item in _buffers)
            {
                item.TrySetException(ex);
            }

            throw;
        }
    }

    private async ValueTask loadBlock(int index)
    {
        var startPosition = index * _bufferSize;
        _stream.Position = startPosition;

        var buffer = new byte[_bufferSize];
        await _stream.ReadAtLeastAsync(buffer, _bufferSize, false);

        _buffers[index].SetResult(buffer);
    }

    public override bool CanRead => !_disposed;
    public override bool CanSeek => !_disposed;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position
    {
        get => _position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _length);
            _position = value;
        }
    }

    public override void Flush() => throw new NotImplementedException();
    public override Task FlushAsync(CancellationToken cancellationToken) => _loadTask;

    public override int Read(byte[] buffer, int offset, int count)
    {
        var toMemory = buffer.AsMemory(offset, count);
        var targetPosition = _position + toMemory.Length;
        if (targetPosition > _length) throw new ArgumentException("Out range.");

        var totalReaded = 0;
        var bytesRemaining = toMemory.Length;
        var (blockIndex, blockPosition) = ((int, int))long.DivRem(_position, _bufferSize);

        while (bytesRemaining > 0)
        {
            var currReaded = int.Min(bytesRemaining, _bufferSize - blockPosition);
            var currToMemory = toMemory.Slice(totalReaded, currReaded);
            var fromMemory = _buffers[blockIndex].Task.Result.AsMemory().Slice(blockPosition, currReaded);

            fromMemory.CopyTo(currToMemory);
            bytesRemaining -= currReaded;
            totalReaded += currReaded;

            blockIndex++;
            blockPosition = 0;
        }

        _position += totalReaded;
        return totalReaded;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin: break;
            case SeekOrigin.Current: offset = _position + offset; break;
            case SeekOrigin.End: offset = _length + offset; break;
            default: throw new ArgumentException($"Invalid SeekOrigin '{origin}'", nameof(origin));
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, _length);

        _position = offset;
        return _position;
    }

    public override void SetLength(long value) => throw new NotImplementedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    #region IDisposeable && IAsyncDisposeable
    private bool _disposed = false;

    public sealed override async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(false);
        GC.SuppressFinalize(this);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive);
    }

    protected override void Dispose(bool disposing)
    {
        if(disposing && !_leaveOpen)
        {
            _stream.Dispose();

            _cancellationTokenSource.Cancel();
            _loadTask.Wait(TimeSpan.FromSeconds(10));
            _buffers.Clear();

            _cancellationTokenSource.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_leaveOpen)
        {
            await _stream.DisposeAsync();
            
            await _cancellationTokenSource.CancelAsync();
            await _loadTask.WaitAsync(TimeSpan.FromSeconds(10));
            _buffers.Clear();

            _cancellationTokenSource.Dispose();
        }
    }

    #endregion
}
