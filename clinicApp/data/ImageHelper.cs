using System.IO;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace clinicApp.data
{
    internal static class ImageHelper
    {
        private const int DefaultWebPQuality = 75;

        /// <summary>
        /// Loads any supported image format from disk and encodes it as lossy WebP bytes.
        /// </summary>
        public static byte[] ConvertToWebP(string filePath, int quality = DefaultWebPQuality)
        {
            using var image = Image.Load(filePath);
            using var ms = new MemoryStream();
            image.Save(ms, new WebpEncoder
            {
                Quality = quality,
                FileFormat = WebpFileFormatType.Lossy
            });
            return ms.ToArray();
        }

        /// <summary>
        /// Decodes image bytes (e.g. WebP) and returns a WPF BitmapImage for display.
        /// </summary>
        public static BitmapImage ToBitmapImage(byte[] imageBytes)
        {
            using var image = Image.Load(imageBytes);
            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            ms.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
