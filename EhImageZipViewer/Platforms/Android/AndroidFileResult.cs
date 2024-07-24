using Android.Provider;

namespace EhImageZipViewer;

public class AndroidFileResult(Android.Net.Uri uri) : PlatformFileResult
{
    public override ValueTask<Stream> OpenReadAsync() => ValueTask.FromResult(OpenRead());

    public Stream OpenRead()
    {
        var activity = (MainActivity)ActivityStateManager.Default.GetCurrentActivity()!;
        return activity.ContentResolver!.OpenInputStream(uri)!;
    }

    public override string FileName
    {
        get
        {
            var activity = (MainActivity)ActivityStateManager.Default.GetCurrentActivity()!;
            string? result = null;
            if (string.Equals(uri.Scheme, "content"))
            {
                var cursor = activity.ContentResolver!.Query(uri, null, null, null, null);
                try
                {
                    if (cursor != null && cursor.MoveToFirst())
                    {
                        result = cursor.GetString(cursor.GetColumnIndex(IOpenableColumns.DisplayName));
                    }
                }
                finally
                {
                    cursor?.Close();
                }
            }
    
            if (result == null)
            {
                result = uri.Path!;
                int cut = result.LastIndexOf('/');
                if (cut != -1)
                {
                    result = result[(cut + 1)..];
                }
            }
    
            return result;
        }
    }
}