using Microsoft.Extensions.Logging;

namespace Jing8956.Maui.SvgImageSource;

public partial class SvgImageSourceService : ImageSourceService, IImageSourceService<ISvgImageSource>
{
    public SvgImageSourceService() : this(null)
    {

    }

    public SvgImageSourceService(ILogger<SvgImageSourceService>? logger = null)
        : base(logger)
    {

    }
}
