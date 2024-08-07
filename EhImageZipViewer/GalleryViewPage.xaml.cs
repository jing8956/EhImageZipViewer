using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EhImageZipViewer.Compression;

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

        _pages.Clear();
        Directory.Delete(_tempDirectory, true);
    }


    private async Task LoadPages(CancellationToken cancellationToken)
    {
        var fileName = _selectedFile.FileName;
        var fileNameHash = MD5.HashData(Encoding.UTF8.GetBytes(fileName));
        var fileNameHashString = string.Concat(fileNameHash.Select(b => b.ToString("x2")));

        using var selectedFileStream = await _selectedFile.OpenReadAsync();

        var addedFiles = new List<string>();

        var progress = new ReciveBytesProgress();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var metricsTask = CollectMetrics(progress, cts.Token);

        try
        {
            await foreach (var entry in ZipArchive.EnumerateAllAsync(selectedFileStream, progress, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entryFileName = Path.GetFileName(entry.FileName);
                var tempFilePath = Path.Combine(_tempDirectory, $"{fileNameHashString}_{entryFileName}.tmp");
                using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write,
                    FileShare.None, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);

                await entry.Content.CopyToAsync(fileStream, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);

                var searchFileName = ParsePageNumber(entryFileName);
                var item = ImageSource.FromFile(tempFilePath);

                var index = addedFiles.FindLastIndex(n => string.CompareOrdinal(n, searchFileName) < 0);
                index++;

                addedFiles.Insert(index, searchFileName);
                _pages.Insert(index, item);
            }
        }
        finally
        {
            topLabel.Text = "";
            await cts.CancelAsync().ConfigureAwait(false);
            await metricsTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private class ReciveBytesProgress : IProgress<int>
    {
        private long _value;
        public long Value => Interlocked.Read(ref _value);

        public void Report(int value) => Interlocked.Add(ref _value, value);
    }

    private async Task CollectMetrics(ReciveBytesProgress progress, CancellationToken cancellationToken = default)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var lastValue = progress.Value;
        while(await timer.WaitForNextTickAsync(cancellationToken))
        {
            var currValue = progress.Value;
            var bytesRead = currValue - lastValue;
            var currValueMB = double.Round(currValue / 1048576.0, 2);
            var bytesReadMB = double.Round(bytesRead / 1048576.0, 2);
            topLabel.Text = $"{currValueMB} MB, {bytesReadMB} MB/s, {bytesReadMB * 8} Mbps";

            lastValue = currValue;
        }
    }

    private static string ParsePageNumber(string name)
        => PageNumberRegex().Replace(name, match => match.Value.PadLeft(4, '0'));

    [GeneratedRegex(@"\d+")]
    private static partial Regex PageNumberRegex();
}


