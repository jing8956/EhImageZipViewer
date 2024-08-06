namespace EhImageZipViewer;

public static class PlatformFilePicker
{
    public async static Task<PlatformFileResult?> PickAsync()
    {
        // "application/x-zip-compressed" Google Could Drive
        var uri = await FilePickerLifecycleObserver.Launch("application/zip", "application/x-zip-compressed");
        if (uri == null) return null;
        return new AndroidFileResult(uri);
    }
}
