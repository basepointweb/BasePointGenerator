using BasePointGenerator.Extensions;
using System.Globalization;
using System.Windows.Data;

namespace BasePointGenerator
{
    public class DecimalTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return false;

            var typeValue = value?.ToString().ToUpper().Replace("?", "");

            if (typeValue.Contains("NULLABLE"))
                typeValue = typeValue.SubstringsBetween("NULLABLE<", ">")[0];

            return
                typeValue.ToUpper() == "DECIMAL" ||
                typeValue.ToUpper() == "FLOAT";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}