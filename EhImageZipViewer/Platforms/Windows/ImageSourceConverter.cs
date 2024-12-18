namespace EhImageZipViewer.WinUI;

public partial class ImageSourceConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value switch
        {
            IFileImageSource fileSource => fileSource.File,
            IUriImageSource uriSource => uriSource.Uri,
            IStreamImageSource streamSource => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
