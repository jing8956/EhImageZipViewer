namespace EhImageZipViewer;

public static class PlatformFilePicker
{
    public async static Task<PlatformFileResult?> PickAsync()
    {
        var uri = await FilePickerLifecycleObserver.Launch("application/zip");
        if (uri == null) return null;
        return new AndroidFileResult(uri);
    }
}
