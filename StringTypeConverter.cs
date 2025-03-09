using BasePointGenerator.Extensions;
using System.Globalization;
using System.Windows.Data;

namespace BasePointGenerator
{
    public class StringTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var typeValue = value?.ToString().ToUpper().Replace("?", "");

            if (typeValue.Contains("NULLABLE"))
                typeValue = typeValue.SubstringsBetween("NULLABLE<", ">")[0];

            return typeValue.ToUpper() == "STRING" ||
                typeValue.ToUpper() == "DECIMAL" ||
                typeValue.ToUpper() == "FLOAT";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
