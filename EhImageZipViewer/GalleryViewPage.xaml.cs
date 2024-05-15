using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace EhImageZipViewer;

public partial class GalleryViewPage : ContentPage
{
    private const string _tempDirectoryName = "24ef1515-3b32-4671-9983-e0dc15c03781";
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), _tempDirectoryName);
    private readonly PlatformFileResult _selectedFile;

    public GalleryViewPage(PlatformFileResult selectedFile)
	{
		InitializeComponent();
        _selectedFile = selectedFile;
    }

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ObservableCollection<GalleryPage> _pages = [];
    private Task _loadTask = null!;

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        if(Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }

        Directory.CreateDirectory(_tempDirectory);

        _loadTask = LoadPages(_cancellationTokenSource.Token);
        MainView.ItemsSource = _pages;
    }
    private async void ContentPage_Unloaded(object sender, EventArgs e)
    {
        try
        {
            await _cancellationTokenSource.CancelAsync();
            await _loadTask;
        }
        catch (OperationCanceledException)
        {

        }

        Directory.Delete(_tempDirectory, true);
        _pages.Clear();
    }

    private async Task LoadPages(CancellationToken cancellationToken)
    {
        using var selectedFileStream = await _selectedFile.OpenReadAsync();
        using var selectedFileArchive = new ZipArchive(selectedFileStream, ZipArchiveMode.Read, true);
        var totalCount = selectedFileArchive.Entries.Count;

        var entries = selectedFileArchive.Entries.OrderBy(e => ParsePageNumber(e.Name));
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            using var entryStream = entry.Open();
            var tempFilePath = Path.Combine(_tempDirectory, $"cache_{entry.FullName}");
            using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write,
                FileShare.None, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);

            await entryStream.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);
            
            _pages.Add(new GalleryPage(tempFilePath));
        }
    }

    private static string ParsePageNumber(string name)
        => PageNumberRegex().Replace(name, match => match.Value.PadLeft(4, '0'));

    [GeneratedRegex(@"\d+")]
    private static partial Regex PageNumberRegex();
}

public class GalleryPage(string file)
{
    public string File => file;
    public ImageSource ImageSource => new CustomFileImageSource(file);
}

public class CustomFileImageSource(string file) : ImageSource, IFileImageSource
{
    public string File => file;
    public override bool IsEmpty => false;
}
