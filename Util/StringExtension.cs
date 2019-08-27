using System.IO;
using System.IO.Compression;
using System.Text;

namespace JsMinBenchmark.Util
{
    public static class StringExtension
    {
        public static long Utf8Length(this string text)
        {
            return Encoding.UTF8.GetBytes(text).LongLength;
        }

        public static long GZipLength(this string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            using(var resultStream = new MemoryStream())
            using(var gZipStream = new GZipStream(resultStream, CompressionMode.Compress, false))
            {
                gZipStream.Write(bytes, 0, bytes.Length);
                gZipStream.Flush();
                return resultStream.Length;
            }
        }
    }
}