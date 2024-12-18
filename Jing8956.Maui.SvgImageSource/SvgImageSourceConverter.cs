using System.ComponentModel;
using System.Globalization;

namespace Jing8956.Maui.SvgImageSource;

public class SvgImageSourceConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string);
    
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        => destinationType == typeof(string);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        var strValue = value?.ToString();
        if (strValue == null)
        {
            throw new InvalidOperationException(string.Format("Cannot convert \"{0}\" into {1}", strValue, typeof(SvgImageSource)));
        }

        return new SvgImageSource() { Path = strValue };
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is SvgImageSource sis) return sis.ToString(); 

        throw new NotSupportedException();
    }
}
