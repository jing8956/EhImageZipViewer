using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Windows.Storage;
using WImageSource = Microsoft.UI.Xaml.Media.ImageSource;
using WSvgImageSource = Microsoft.UI.Xaml.Media.Imaging.SvgImageSource;

namespace Jing8956.Maui.SvgImageSource;

public partial class SvgImageSourceService
{
    public SvgImageSourceService(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        ILogger<SvgImageSourceService> logger) : this(logger)
    {
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
    }

    public override Task<IImageSourceServiceResult<WImageSource>?> GetImageSourceAsync(
        IImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default)
        => GetImageSourceAsync((ISvgImageSource)imageSource, scale, cancellationToken);

    public async Task<IImageSourceServiceResult<WImageSource>?> GetImageSourceAsync(
        ISvgImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default)
    {
        if (imageSource.IsEmpty) return null;

        var filename = imageSource.Path;
        if (string.IsNullOrEmpty(filename)) return null;

        var color = imageSource.Color;

        try
        {
            // 首次安装启动会被取消，导致 svg 显示失败。
            var image = await GetLocal(filename, color, CancellationToken.None)
                ?? await GetAppPackage(filename, color, CancellationToken.None)
                ?? throw new InvalidOperationException("Unable to load image file.");

            var result = new ImageSourceServiceResult(image);
            return result;
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "Unable to load image file '{File}'.", filename);
            throw;
        }
    }

    private async ValueTask<WSvgImageSource?> GetAppPackage(string filename, Color? color, CancellationToken cancellationToken = default)
    {
        var uriSource = new Uri($"ms-appx:///{filename}");
        if (color == null || color == Colors.Black) return new WSvgImageSource(uriSource);

        try
        {
            var originFile = await StorageFile.GetFileFromApplicationUriAsync(uriSource);
            return await ChangeDefaultColor(originFile, color, cancellationToken);
        }
#if DEBUG
        catch (Exception e)
        {
            Logger?.LogWarning(e, "Get SVG '{FileName}' from App Package failed.", filename);
#else
        catch
        {

#endif
        }

        return null;
    }
    private async ValueTask<WSvgImageSource?> GetLocal(string filename, Color? color, CancellationToken cancellationToken = default)
    {
        if (Path.IsPathRooted(filename))
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(filename);
                using var stream = await file.OpenReadAsync();

                if (color == null || color == Colors.Black)
                {
                    var result = new WSvgImageSource();
                    await result.SetSourceAsync(stream);

                    return result;
                }
                else
                {
                    return await ChangeDefaultColor(file, color, cancellationToken);
                }
            }
#if DEBUG
            catch (Exception e)
            {
                Logger?.LogWarning(e, "Get SVG '{FileName}' from Local failed.", filename);
#else
            catch
            {
#endif
            }
        }

        return null;
    }

    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager = new();
    private readonly XmlWriterSettings _xmlWriterSettings = new()
    {
        Async = true,
        Indent = false,
        OmitXmlDeclaration = true,
        CloseOutput = false,
    };

    private async ValueTask<WSvgImageSource> ChangeDefaultColor(StorageFile svgFile, Color color, CancellationToken cancellationToken = default)
    {
        var hexColor = color.ToHex();

        using var inStream = await svgFile.OpenStreamForReadAsync();
        var doc = await XDocument.LoadAsync(inStream, LoadOptions.None, cancellationToken);

        ChangeSvgDefaultColor(doc.Root, hexColor);

        var bufferStream = new RecyclableMemoryStream(_recyclableMemoryStreamManager);
        using var writer = XmlWriter.Create(bufferStream, _xmlWriterSettings);
        await doc.SaveAsync(writer, cancellationToken);
        await writer.FlushAsync();

        bufferStream.Position = 0;

        var result = new WSvgImageSource();
        await result.SetSourceAsync(bufferStream.AsRandomAccessStream());
        return result;
    }

    private static void ChangeSvgDefaultColor(XElement? element, string fill)
    {
        if (element == null) return;

        var styleAttr = element.Attribute("style");
        if (styleAttr != null)
        {
            var style = styleAttr.Value.AsSpan();
            var newStyle = new StringBuilder(style.Length);

            Span<Range> ranges = stackalloc Range[2];
            while(style.Length > 0)
            {
                if(style.Split(ranges, ';', StringSplitOptions.RemoveEmptyEntries) == 1)
                {
                    ranges[1] = new Range(0, 0);
                }

                var item = style[ranges[0]];
                if (item.StartsWith("fill:"))
                {
                    var value = item[5..];
                    var attr = element.Attribute("fill");

                    if (attr == null && value.Length > 0)
                    {
                        element.SetAttributeValue("fill", new string(value));
                    }
                }
                else if (item.StartsWith("stroke:"))
                {
                    var value = item[7..];
                    var attr = element.Attribute("stroke");

                    if (attr == null && value.Length > 0)
                    {
                        element.SetAttributeValue("stroke", new string(value));
                    }
                }
                else
                {
                    if (newStyle.Length > 0) newStyle.Append(';');
                    newStyle.Append(item);
                }

                style = style[ranges[1]];
            }

            styleAttr.Value = newStyle.ToString();
        }

        var fillAttr = element.Attribute("fill");
        var strokeAttr = element.Attribute("stroke");

        if(fillAttr == null && strokeAttr == null)
        {
            element.SetAttributeValue("fill", fill);
        }
        else
        {
            if (fillAttr != null && (fillAttr.Value == "#000" || fillAttr.Value == "#000000"))
            {
                fillAttr.Value = fill;
            }
            if (strokeAttr != null && (strokeAttr.Value == "#000" || strokeAttr.Value == "#000000"))
            {
                strokeAttr.Value = fill;
            }
        }

        foreach (var innElement in element.Elements()) ChangeSvgDefaultColor(innElement, fill);
    }

}