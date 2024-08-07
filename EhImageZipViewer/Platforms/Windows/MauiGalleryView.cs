using EhImageZipViewer.Controls;
using Microsoft.UI.Xaml.Controls;

namespace EhImageZipViewer.WinUI;

public class MauiGalleryView : Microsoft.UI.Xaml.Controls.ListView
{
    private readonly GalleryView _vitrualView;

    public MauiGalleryView(GalleryView vitrualView)
    {
        _vitrualView = vitrualView;
        ItemTemplate = (Microsoft.UI.Xaml.DataTemplate)MauiWinUIApplication.Current.Resources["GalleryViewItemTemplate"];
        ItemContainerStyle = (Microsoft.UI.Xaml.Style)MauiWinUIApplication.Current.Resources["GalleryViewItemStyle"];

        SelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode.None;
        IsItemClickEnabled = true;
    }

    public void UpdateImages() => ItemsSource = _vitrualView.Images;
}
