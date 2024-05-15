namespace EhImageZipViewer;

public class Win32FileResult(string filePath) : PlatformFileResult
{
    public override ValueTask<Stream> OpenReadAsync() => ValueTask.FromResult(OpenRead());
#pragma warning disable CA1859 // 尽可能使用具体类型以提高性能
    private Stream OpenRead() => new FileStream(filePath, FileMode.Open, FileAccess.Read);
#pragma warning restore CA1859 // 尽可能使用具体类型以提高性能
}

