using Android.Content;
using Bumptech.Glide;
using Bumptech.Glide.Load;
using Bumptech.Glide.Module;
using Bumptech.Glide.Request;

namespace EhImageZipViewer.Platforms.Android;

public class MauiAppGlideModule : AppGlideModule
{
    public override void ApplyOptions(Context context, GlideBuilder builder)
    {
        var defaultRequestOptions = new RequestOptions();
        defaultRequestOptions.Format(DecodeFormat.PreferRgb565);

        builder.SetDefaultRequestOptions(defaultRequestOptions);
    }
}
