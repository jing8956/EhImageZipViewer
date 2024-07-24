namespace EhImageZipViewer.Controls;

public class GalleryView : View
{
    public static readonly BindableProperty ImagesProperty =
        BindableProperty.Create(nameof(Images), typeof(IEnumerable<ImageSource>), typeof(GalleryView), null);

    public IEnumerable<ImageSource> Images
    {
        get { return (IEnumerable<ImageSource>)GetValue(ImagesProperty); }
        set { SetValue(ImagesProperty, value); }
    }
}
