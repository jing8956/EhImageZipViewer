using System.Diagnostics.CodeAnalysis;

namespace Jing8956.Maui.SvgImageSource;

public interface ISvgImageSource : IImageSource
{
    string? Path { get; }
    Color? Color { get; }
}
