using System;
using System.Globalization;
using System.IO;
using Microsoft.Maui.Controls;

namespace WindowManager.Converters
{
    /// <summary>
    /// Converts a <c>byte[]</c> containing raw image data (BMP, JPEG, PNG, …) to a
    /// MAUI <see cref="ImageSource"/> suitable for binding to an <see cref="Image"/> control.
    /// Returns <c>null</c> when the value is <c>null</c> or an empty array, which causes
    /// the bound <see cref="Image"/> to display nothing.
    /// </summary>
    public class ByteArrayToImageSourceConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is byte[] bytes && bytes.Length > 0)
            {
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }

            return null;
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException($"{nameof(ByteArrayToImageSourceConverter)} does not support ConvertBack.");
    }
}
