using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Jing8956.Maui.SvgImageSource;

[TypeConverter(typeof(SvgImageSourceConverter))]
public partial class SvgImageSource : ImageSource, ISvgImageSource
{
    [MemberNotNullWhen(false, nameof(Path))]
    public override bool IsEmpty => string.IsNullOrEmpty(Path);

    public static readonly BindableProperty PathProperty =
        BindableProperty.Create(nameof(Path), typeof(string), typeof(SvgImageSource));
    public string? Path
    {
        get => (string?)GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public static readonly BindableProperty FillProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(SvgImageSource));
    public Color? Color
    {
        get => (Color?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public override Task<bool> Cancel() => Task.FromResult(false);
    public override string ToString()
    {
        var fill = Color;
        string? rgb = null;
        if (fill != null)
        {
            fill.ToRgb(out var r, out var g, out var b);
            rgb = $"(#{r:X2}{g:X2}){b:X2})";
        }

        return $"Svg{rgb}: {Path}";
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyName == PathProperty.PropertyName
            || propertyName == FillProperty.PropertyName)
            OnSourceChanged();

        base.OnPropertyChanged(propertyName);
    }
}
