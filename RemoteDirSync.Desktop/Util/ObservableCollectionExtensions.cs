using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDirSync.Desktop.Util
{
  public static class ObservableCollectionExtensions
  {
    public static void Sort<T, TKey1, TKey2>(this ObservableCollection<T> collection,
                                         Func<T, TKey1> keySelector1,
                                         Func<T, TKey2>? keySelector2 = null)
    {
      if (collection == null) throw new ArgumentNullException(nameof(collection));
      if (keySelector1 == null) throw new ArgumentNullException(nameof(keySelector1));

      var sorted = collection
        .OrderBy(keySelector1);
      if (keySelector2 != null) {
        sorted = sorted.ThenBy(keySelector2);
      }
      var result = sorted.ToList();

      collection.Clear();
      foreach (var item in result)
        collection.Add(item);
    }
  }
}
