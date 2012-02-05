using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkypeAutoRecorder
{
    /// <summary>
    /// Helpers for main code.
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// Converts <see cref="Icon"/> to <see cref="ImageSource"/>.
        /// </summary>
        /// <param name="icon">The <see cref="Icon"/>.</param>
        /// <returns>The <see cref="ImageSource"/>.</returns>
        public static ImageSource IconToImageSource(Icon icon)
        {
            var stream = new MemoryStream();
            icon.Save(stream);
            stream.Position = 0;
            return BitmapFrame.Create(stream);
        }
    }
}
