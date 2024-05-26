// DurationToMinutesConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace MoonPlayer.Converters
{
    public class DurationToMinutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int seconds)
            {
                int minutes = seconds / 60;
                int remainingSeconds = seconds % 60;
                return $"{minutes:00}:{remainingSeconds:00}";
            }
            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();  // Not needed for one-way bindings
        }
    }
}
