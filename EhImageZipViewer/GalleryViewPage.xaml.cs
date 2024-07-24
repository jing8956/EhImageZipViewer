using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
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
    private readonly ObservableCollection<ImageSource> _pages = [];
    private Task _loadTask = null!;

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        if(Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }

        Directory.CreateDirectory(_tempDirectory);

        _loadTask = LoadPages(_cancellationTokenSource.Token);
        MainView.Images = _pages;
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
        var fileName = _selectedFile.FileName;
        var fileNameHash = MD5.HashData(Encoding.UTF8.GetBytes(fileName));
        var fileNameHashString = string.Concat(fileNameHash.Select(b => b.ToString("x2")));

        using var selectedFileStream = await _selectedFile.OpenReadAsync();
        using var archive = await Compression.ZipArchive.LoadAsync(selectedFileStream);

        var totalCount = archive.Entries.Count;
        var sortedFileName = archive.Entries.Select(h => h.Filename).OrderBy(ParsePageNumber).ToList();
        await foreach (var entry in archive.EnumerateAllAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var tempFilePath = Path.Combine(_tempDirectory, $"{fileNameHashString}_{entry.FileName}.tmp");
            using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write,
                FileShare.None, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);

            await entry.Content.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);

            var index = sortedFileName.IndexOf(entry.FileName);
            var item = ImageSource.FromFile(tempFilePath);
            _pages.Insert(index, item);
        }
    }

    private static string ParsePageNumber(string name)
        => PageNumberRegex().Replace(name, match => match.Value.PadLeft(4, '0'));

    [GeneratedRegex(@"\d+")]
    private static partial Regex PageNumberRegex();
}


