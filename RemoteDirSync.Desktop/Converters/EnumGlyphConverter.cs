using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDirSync.Desktop.Converters
{
  public class EnumGlyphConverter : IValueConverter
  {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      if (value is null || parameter is null)
        return null;

      var enumName = value.ToString();
      if (string.IsNullOrEmpty(enumName))
        return null;

      var map = ParseMapping(parameter.ToString() ?? string.Empty);
      if (!map.TryGetValue(enumName, out var glyph) &&
          !map.TryGetValue("*", out glyph))
        return null;

      return glyph;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
      => throw new NotSupportedException();

    private static Dictionary<string, string> ParseMapping(string mapping)
    {
      var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      foreach (var pair in mapping.Split(':', StringSplitOptions.RemoveEmptyEntries))
      {
        var parts = pair.Split(new[] { '=' }, 2);
        if (parts.Length != 2)
          continue;

        var key = parts[0].Trim();
        var value = parts[1].Trim();

        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
        {
          result[key] = value;
        }
      }

      return result;
    }
  }
}
