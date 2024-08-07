using Android.Provider;

namespace EhImageZipViewer;

public class AndroidFileResult : PlatformFileResult
{
    private readonly Android.Net.Uri _uri;

    public AndroidFileResult(Android.Net.Uri uri)
    {
        _uri = uri;

        if (string.Equals(_uri.Scheme, "content"))
        {
            var cursor = MainActivity.CurrentActivity.ContentResolver!.Query(_uri, null, null, null, null)!;

            var nameIndex = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
            var sizeIndex = cursor.GetColumnIndex(IOpenableColumns.Size);
            cursor.MoveToFirst();

            FileName = cursor.GetString(nameIndex)!;
            FileLength = cursor.GetLong(sizeIndex);

            cursor.Close();
        }
        else
        {
            var file = new Java.IO.File(_uri.ToString()!);

            FileName = file.Name;
            FileLength = file.Length();
        }
    }

    public override ValueTask<Stream> OpenReadAsync() => ValueTask.FromResult(OpenRead());

    public Stream OpenRead() => MainActivity.CurrentActivity.ContentResolver!.OpenInputStream(_uri)!;

    public override string FileName { get; }
    public override long FileLength { get; }

    public string ContentType => MainActivity.CurrentActivity.ContentResolver!.GetType(_uri)!;
}