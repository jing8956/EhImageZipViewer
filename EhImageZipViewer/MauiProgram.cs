using EhImageZipViewer.Controls;
using EhImageZipViewer.Handlers;
using Jing8956.Maui.SvgImageSource;
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
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<GalleryView, GalleryViewHandler>();
            });

        builder.AddSvgImageSource();

        builder.Services.AddTransient(_ => Preferences.Default);
        builder.Services.AddTransient<ISettingsService, SettingsSrvice>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
