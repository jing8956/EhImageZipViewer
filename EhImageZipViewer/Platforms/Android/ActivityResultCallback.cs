using AndroidX.Activity.Result;

namespace EhImageZipViewer;

public class ActivityResultCallback<TResult> : Java.Lang.Object, IActivityResultCallback
    where TResult : Java.Lang.Object
{
    readonly Action<TResult?> _callback;
    public ActivityResultCallback(Action<TResult?> callback) => _callback = callback;
    public ActivityResultCallback(TaskCompletionSource<TResult?> tcs) => _callback = tcs.SetResult;

    public void OnActivityResult(Java.Lang.Object? result) => _callback((TResult?)result);
}
