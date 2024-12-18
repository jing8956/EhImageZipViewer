#if WINDOWS
using Microsoft.Extensions.DependencyInjection.Extensions;
#endif

namespace Jing8956.Maui.SvgImageSource;

public static class MauiHandlersCollectionExtensions
{
    public static MauiAppBuilder AddSvgImageSource(this MauiAppBuilder builder)
    {
#if WINDOWS
        builder.Services.TryAddSingleton<Microsoft.IO.RecyclableMemoryStreamManager>();
#endif

        builder.Services.AddSingleton<SvgImageSourceService>();
        builder.ConfigureImageSources(services =>
        {
            services.AddService(svcs =>
            {
                var service = svcs.GetRequiredService<SvgImageSourceService>();         
                return service;
            });
        });

        return builder;
    }
}
