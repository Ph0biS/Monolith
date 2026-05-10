using System.Globalization;

namespace PROJECT.Converters
{
    // Цвет текста: фиолетовый если открыто, серый если закрыто
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUnlocked)
            {
                return isUnlocked ? Color.FromArgb("#A78BFA") : Colors.Gray;
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    // Инверсия bool: возвращает True, если ачивка ЗАКРЫТА (чтобы показать замок)
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUnlocked)
            {
                return !isUnlocked;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}