namespace Jing8956.Maui.SvgImageSource;

internal static class Extensions
{
    public static string ToHexString(this Span<byte> bytes) => ToHexString((ReadOnlySpan<byte>)bytes);
    public static string ToHexString(this ReadOnlySpan<byte> bytes) =>
        string.Create(bytes.Length * 2, bytes, (hexChars, bytes) =>
        {
            ReadOnlySpan<char> hexAlphabet = "0123456789ABCDEF";

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                hexChars[i * 2] = hexAlphabet[b >> 4];
                hexChars[i * 2 + 1] = hexAlphabet[b & 0x0F];
            }
        });
}
