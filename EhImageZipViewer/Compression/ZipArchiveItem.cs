namespace EhImageZipViewer.Compression;

public readonly struct ZipArchiveItem
{
    public required string FileName { get; init; }
    public required Stream Content { get; init; }
}
