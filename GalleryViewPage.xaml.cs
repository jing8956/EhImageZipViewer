using System.Collections.ObjectModel;
using System.IO.Compression;

namespace EhImageZipViewer;

public partial class GalleryViewPage : ContentPage
{
	private readonly FileResult selectedFile;
    public GalleryViewPage(FileResult selectedFile)
	{
		InitializeComponent();
        this.selectedFile = selectedFile;
    }

    private int loadPosition;
    private readonly ObservableCollection<GalleryPage> pages = [];
    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        await LoadPages(3);
        MainView.ItemsSource = pages;
    }
    private void ContentPage_Unloaded(object sender, EventArgs e)
    {
        pages.Clear();
        GC.Collect();
    }

    private async Task LoadPages(int count)
    {
#if !WINDOWS
        using var selectedFileStream = await selectedFile.OpenReadAsync();
#else
        using var selectedFileStream = selectedFile.OpenRead();
#endif
        using var selectedFileArchive = new ZipArchive(selectedFileStream, ZipArchiveMode.Read, true);
        var totalCount = selectedFileArchive.Entries.Count;

        for (int i = 0; i < count; i++)
        {
            if (loadPosition < totalCount)
            {
                var entry = selectedFileArchive.Entries[loadPosition++];
                
                using var entryStream = entry.Open();
                using var memoryStream = new MemoryStream((int)entry.Length);

                await entryStream.CopyToAsync(memoryStream);
                pages.Add(new GalleryPage(memoryStream.GetBuffer()));
            }
            else
            {
                break;
            }
        }

        selectedFileStream.Seek(0, SeekOrigin.Begin);
    }

    private readonly object isBusy = new();
    private async void MainView_RemainingItemsThresholdReached(object sender, EventArgs e)
    {
        if(Monitor.TryEnter(isBusy))
        {
            await LoadPages(1);
            Monitor.Exit(isBusy);
        }
    }
}

public class GalleryPage(byte[] data)
{
    public ImageSource ImageSource => ImageSource.FromStream(GetStreamAsync);

    public Task<Stream> GetStreamAsync(CancellationToken _) => Task.FromResult<Stream>(new MemoryStream(data));
}
