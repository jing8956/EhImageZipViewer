namespace EhImageZipViewer;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnCounterClicked(object sender, EventArgs e)
    {
#if WINDOWS || ANDROID
        var pickResult = await PlatformFilePicker.PickAsync();
#else
        var fileResult = await FilePicker.PickAsync(pickOptions);
        var pickResult = fileResult != null ? new GenerateFileResult(fileResult) : null;
#endif
        if (pickResult != null)
        {
            await Navigation.PushAsync(new GalleryViewPage(pickResult));
        }
    }
    private class GenerateFileResult(FileResult fileResult) : PlatformFileResult
    {
        public override async ValueTask<Stream> OpenReadAsync()
        {
            return await fileResult.OpenReadAsync();
        }

        public override string FileName => fileResult.FileName;
        public override long FileLength => new FileInfo(fileResult.FullPath).Length;
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
