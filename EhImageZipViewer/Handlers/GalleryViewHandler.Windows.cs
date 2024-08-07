using System.Collections.ObjectModel;
using EhImageZipViewer.Controls;
using EhImageZipViewer.WinUI;
using Microsoft.Maui.Handlers;

namespace EhImageZipViewer.Handlers;

public partial class GalleryViewHandler : ViewHandler<GalleryView, MauiGalleryView>
{
    protected override MauiGalleryView CreatePlatformView() => new(VirtualView);

    public static void MapImages(GalleryViewHandler handler, GalleryView view)
    {
        handler.PlatformView.UpdateImages();
    }
}
