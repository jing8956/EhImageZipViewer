using System.Buffers.Binary;
using System.IO.Compression;
using System.IO.Pipelines;

namespace EhImageZipViewer.Compression;

internal enum ZipCompressionMethod : ushort
{
    Stored = 0x0,
    Deflate = 0x8,
    Deflate64 = 0x9,
    BZip2 = 0xC,
    LZMA = 0xE
}
