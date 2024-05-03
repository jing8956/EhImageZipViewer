using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EhImageZipViewer;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
#if ANDROID
            .ConfigureImageSources(services =>
            {
                services.RemoveAll<IImageSourceService<IStreamImageSource>>();
                services.RemoveAll<IImageSourceService<StreamImageSource>>();
                services.AddService<StreamImageSource>(svcs => new CustomStreamImageSourceService(svcs.GetRequiredService<ILogger<CustomStreamImageSourceService>>()));
            })
#endif
            ;

#if DEBUG
		builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

#if ANDROID

    public class CustomStreamImageSourceService(ILogger<MauiProgram.CustomStreamImageSourceService> logger) : StreamImageSourceService(logger)
    {
        public override async Task<IImageSourceServiceResult?> LoadDrawableAsync(
            IImageSource imageSource, Android.Widget.ImageView imageView, CancellationToken cancellationToken = default)
        {
            var result = await base.GetDrawableAsync(imageSource, imageView.Context!, cancellationToken);
            imageView.SetImageDrawable(result?.Value);
            return result;
        }
    }

#endif

}
