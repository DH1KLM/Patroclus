using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Patroclus
{
    public static class StringConverters
    {
        //DH1KLM: Keep formatting local and independent of Avalonia internal utility types.
        public static readonly IValueConverter StringFormat =
            new FuncValueParameterConverter<object, object, string>(
                (value, format) => string.Format(
                    CultureInfo.InvariantCulture,
                    Convert.ToString(format, CultureInfo.InvariantCulture) ?? "{0}",
                    value));
    }

    public sealed class FuncValueParameterConverter<TIn, TParam, TOut> : IValueConverter
    {
        private readonly Func<TIn, TParam, TOut> _convert;

        public FuncValueParameterConverter(Func<TIn, TParam, TOut> convert)
        {
            _convert = convert ?? throw new ArgumentNullException(nameof(convert));
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TIn typedValue || (value == null && default(TIn) == null))
            {
                return _convert(
                    value is TIn v ? v : default!,
                    parameter is TParam p ? p : default!);
            }

            return AvaloniaProperty.UnsetValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}