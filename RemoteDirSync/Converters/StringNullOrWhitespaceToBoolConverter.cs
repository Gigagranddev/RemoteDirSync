using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDirSync.Converters
{
  public class StringNullOrWhitespaceToBoolConverter : IValueConverter
  {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      if (value is string str)
      {
        return !string.IsNullOrWhiteSpace(str);
      }
      return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
