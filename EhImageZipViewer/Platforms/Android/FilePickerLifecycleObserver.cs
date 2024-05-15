using AndroidX.Activity.Result;
using AndroidX.Lifecycle;

namespace EhImageZipViewer;

public class FilePickerLifecycleObserver(ActivityResultRegistry registry) : Java.Lang.Object, IDefaultLifecycleObserver
{
    public ActivityResultLauncher Launcher { get; private set; } = default!;

    public void OnCreate(ILifecycleOwner owner)
    {
        Launcher = registry.Register(nameof(PlatformFilePicker), owner,
            new FilePickerContract(),
            new ActivityResultCallback<Android.Net.Uri>(OnResult));
    }
    public void OnStart(ILifecycleOwner owner) { }
    public void OnPause(ILifecycleOwner owner) { }
    public void OnResume(ILifecycleOwner owner) { }
    public void OnStop(ILifecycleOwner owner) { }
    public void OnDestroy(ILifecycleOwner owner) { }

    private static readonly SemaphoreSlim _semaphore = new(1);
    private static TaskCompletionSource<Android.Net.Uri?>? _callbackSource;
    public static async Task<Android.Net.Uri?> Launch(string mimeType)
    {
        var activity = (MainActivity)ActivityStateManager.Default.GetCurrentActivity()!;

        await _semaphore.WaitAsync();
        Android.Net.Uri? result;
        try
        {
            _callbackSource = new TaskCompletionSource<Android.Net.Uri?>();
            activity.FilePickerLifecycleObserver.Launcher.Launch(mimeType);
            result = await _callbackSource.Task;
        }
        finally
        {
            _callbackSource = null;
            _semaphore.Release();
        }

        return result;
    }
    private static void OnResult(Android.Net.Uri? result) => _callbackSource?.SetResult(result);
}
