using Microsoft.Maui.Controls;

namespace EhImageZipViewer;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnCounterClicked(object sender, EventArgs e)
    {
#if !WINDOWS
        var pickResult = await FilePicker.PickAsync(pickOptions);
#else
        var pickResult = await CustomFilePicker.PickAsync();
#endif
        if(pickResult != null)
        {
            await Navigation.PushAsync(new GalleryViewPage(pickResult));
        }
    }

    private static readonly PickOptions pickOptions;
    static MainPage()
    {
        var fileTypes = new Dictionary<DevicePlatform, IEnumerable<string>>()
        {
            { DevicePlatform.WinUI, new[] { ".zip" } },
            { DevicePlatform.Android, new[] { "application/zip" } }
        };
        pickOptions = new PickOptions()
        {
            PickerTitle = "Select Image Zip file",
            FileTypes = new FilePickerFileType(fileTypes)
        };
    }
}
