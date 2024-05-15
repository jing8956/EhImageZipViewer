using Android.App;
using Android.Content;
using AndroidX.Activity.Result.Contract;

namespace EhImageZipViewer;

public class FilePickerContract : ActivityResultContract
{
    public override Intent CreateIntent(Context context, Java.Lang.Object? input)
    {
        var intent = new Intent(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        // intent.PutExtra(Intent.ExtraAllowMultiple, false);
        intent.SetType((string?)input);
        intent.AddFlags(ActivityFlags.ExcludeFromRecents);
        return intent;
    }

    public override Java.Lang.Object? ParseResult(int resultCode, Intent? intent)
    {
        if (resultCode != (int)Result.Ok) return null;
        return intent?.Data;
    }
}
