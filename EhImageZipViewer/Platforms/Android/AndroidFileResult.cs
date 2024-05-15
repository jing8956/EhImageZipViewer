namespace EhImageZipViewer;

public class AndroidFileResult(Android.Net.Uri uri) : PlatformFileResult
{
    public override ValueTask<Stream> OpenReadAsync() => ValueTask.FromResult(OpenRead());

    public Stream OpenRead()
    {
        var activity = (MainActivity)ActivityStateManager.Default.GetCurrentActivity()!;
        return activity.ContentResolver!.OpenInputStream(uri)!;
    }
}