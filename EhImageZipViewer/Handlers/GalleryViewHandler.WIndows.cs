using System.Collections.ObjectModel;
using EhImageZipViewer.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EhImageZipViewer.Handlers;

public partial class GalleryViewHandler : ViewHandler<GalleryView, TextBlock>
{
    protected override TextBlock CreatePlatformView()
    {
        return new TextBlock();
    }

    public static void MapImages(GalleryViewHandler handler, GalleryView view)
    {
        if(view.Images != null && view.Images is ObservableCollection<ImageSource> source)
        {
            source.CollectionChanged += (sender, args) =>
            {
                handler.PlatformView.DispatcherQueue.TryEnqueue(() =>
                {
                    handler.PlatformView.Text = $"{source.Count}";
                });
            };
        }
    }
}
