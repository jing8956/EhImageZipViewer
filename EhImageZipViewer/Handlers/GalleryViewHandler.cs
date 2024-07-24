using EhImageZipViewer.Controls;
using Microsoft.Maui.Handlers;

namespace EhImageZipViewer.Handlers;

public partial class GalleryViewHandler
{
    public static IPropertyMapper<GalleryView, GalleryViewHandler> PropertyMapper =
        new PropertyMapper<GalleryView, GalleryViewHandler>(ViewMapper)
        {
            [nameof(GalleryView.Images)] = MapImages
        };

    public GalleryViewHandler() : base(PropertyMapper)
    {
        
    }
}
