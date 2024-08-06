using Android.App;
using Android.Content;
using AndroidX.Activity.Result.Contract;
using static Android.Icu.Text.TimeZoneFormat;

namespace EhImageZipViewer;

public class FilePickerContract : ActivityResultContract
{
    public override Intent CreateIntent(Context context, Java.Lang.Object? input)
    {
        var intent = new Intent(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        // intent.PutExtra(Intent.ExtraAllowMultiple, false);
        intent.PutExtra(Intent.ExtraMimeTypes, (string[])input!);
        intent.SetType("*/*");
        intent.AddFlags(ActivityFlags.ExcludeFromRecents);
        return intent;
    }

    public override Java.Lang.Object? ParseResult(int resultCode, Intent? intent)
    {
        if (resultCode != (int)Result.Ok) return null;
        return intent?.Data;
    }
}
