namespace EhImageZipViewer;

public abstract class PlatformFileResult
{
    public abstract ValueTask<Stream> OpenReadAsync();
    public abstract string FileName { get; }
}
