using EhImageZipViewer.Controls;
using Microsoft.Maui.Handlers;
using UIKit;

namespace EhImageZipViewer.Handlers;

public partial class GalleryViewHandler : ViewHandler<GalleryView, UIView>
{
    protected override UIView CreatePlatformView()
    {
        throw new NotImplementedException();
    }

    public static void MapImages(GalleryViewHandler handler, GalleryView view)
    {

    }
}
