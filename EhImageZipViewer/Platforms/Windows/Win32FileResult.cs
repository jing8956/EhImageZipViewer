namespace EhImageZipViewer;

public class Win32FileResult(string filePath) : PlatformFileResult
{
    private static readonly FileStreamOptions _openOptions = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        BufferSize = 1,
        Options = FileOptions.SequentialScan | FileOptions.Asynchronous
    };

    private readonly FileInfo _fileInfo = new(filePath);

    public override ValueTask<Stream> OpenReadAsync() => ValueTask.FromResult(OpenRead());
#pragma warning disable CA1859 // 尽可能使用具体类型以提高性能
    private Stream OpenRead() => _fileInfo.Open(_openOptions);
#pragma warning restore CA1859 // 尽可能使用具体类型以提高性能

    public override string FileName => _fileInfo.Name;
    public override long FileLength => _fileInfo.Length;
    public string FilePath => _fileInfo.FullName;
}

