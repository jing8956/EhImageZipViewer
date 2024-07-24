using EhImageZipViewer.Controls;
using EhImageZipViewer.Platforms.Android;
using Microsoft.Maui.Handlers;
using global::Android.Views;
using AndroidX.RecyclerView.Widget;

namespace EhImageZipViewer.Handlers;

public partial class GalleryViewHandler : ViewHandler<GalleryView, MauiGalleryView>
{
    protected override MauiGalleryView CreatePlatformView()
    {
        var result = new MauiGalleryView(Context, VirtualView)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        result.SetLayoutManager(new LinearLayoutManager(Context));

        return result;
    }

    protected override void ConnectHandler(MauiGalleryView platformView)
    {
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MauiGalleryView platformView)
    {
        platformView.Dispose();
        base.DisconnectHandler(platformView);
    }

    public static void MapImages(GalleryViewHandler handler, GalleryView view)
    {
        handler.PlatformView.UpdateImages();
    }
}
