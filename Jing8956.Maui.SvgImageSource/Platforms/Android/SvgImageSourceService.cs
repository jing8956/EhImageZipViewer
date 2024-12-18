using System.Buffers;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;
using AndroidX.Core.Graphics.Drawable;
using Java.Interop;
using Microsoft.Extensions.Logging;
using Color = Microsoft.Maui.Graphics.Color;

namespace Jing8956.Maui.SvgImageSource;

public partial class SvgImageSourceService
{
    public override Task<IImageSourceServiceResult<Drawable>?> GetDrawableAsync(
        IImageSource imageSource, Context context, CancellationToken cancellationToken = default)
        => GetDrawableAsync((ISvgImageSource)imageSource, context, cancellationToken);

    public async Task<IImageSourceServiceResult<Drawable>?> GetDrawableAsync(
        ISvgImageSource imageSource, Context context, CancellationToken cancellationToken = default)
    {
        if (imageSource.IsEmpty) return null;

        var filename = imageSource.Path;
        if (string.IsNullOrEmpty(filename)) return null;

        var color = imageSource.Color;

        try
        {
            var image = await GetResources(filename, color, context, cancellationToken)
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

    private static readonly SearchValues<char> _validChars
        = SearchValues.Create("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_");
    private ValueTask<Drawable?> GetResources(string filename, Color? color,
        Context context, CancellationToken cancellationToken = default)
    {
        try
        {
            var name = filename.AsSpan()[..^4]; // remove extension ".svg"

            var resourceName = string.Create(name.Length, name, static (span, name) =>
            {
                name.CopyTo(span);

                for (int i = 0; i < span.Length; i++)
                {
                    ref char c = ref span[i];
                    if (!_validChars.Contains(c)) c = '_';
                }
            });
            var packageName = context.ApplicationContext!.PackageName;
            var id = context.Resources!.GetIdentifier(resourceName, "drawable", packageName);

            var result = context.Resources.GetDrawable(id, null);
            if(result != null && color != null)
            {
                var contextB = context.GetJniTypeName();
                result = result.Mutate();
                color.ToRgb(out var r, out var g, out var b);
                // var colorValue = (0xFF << 24) | (0xFF << 16) | (0x00 << 8) | 0x00;
                result.SetTint(Android.Graphics.Color.Red);
                result.SetTintMode(PorterDuff.Mode.SrcIn);
            }
            return ValueTask.FromResult(result);
        }
#if DEBUG
        catch (Exception e)
        {
            Logger?.LogWarning(e, "Get SVG '{FileName}' from Resources failed.", filename);
#else
        catch
        {
#endif
        }

        return ValueTask.FromResult<Drawable?>(null);
    }
}
