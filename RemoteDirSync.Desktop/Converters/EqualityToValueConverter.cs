using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDirSync.Desktop.Converters
{
  public class EqualityToValueConverter : IValueConverter
  {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      if(parameter is string strParam)
      {
        var parts = strParam.Split(":");        
        if(parts.Length != 3)
        {
          return value;
        }

        if(value is int intVal)
        {
          if(intVal == int.Parse(parts[0]))
          {
            return parts[1];
          }
          else
          {
            return parts[2];
          }
        }

        if(value is string strVal)
        {
          if(strVal == parts[0])
          {
            return parts[1];
          }
          else
          {
            return parts[2];
          }
        }
      }
      return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
