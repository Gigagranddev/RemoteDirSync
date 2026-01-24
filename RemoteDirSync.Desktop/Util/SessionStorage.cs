using RemoteDirSync.Desktop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RemoteDirSync.Desktop.Util
{
  public class SessionStorage
  {
    private const string _fileName = "sessions.json";

    private static string GetAppDataDirectory()
    {
      var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      var appDir = Path.Combine(baseDir, "RemoteDirSync");
      Directory.CreateDirectory(appDir);
      return appDir;
    }

    public async Task SaveAsync(List<Session> sessions)
    {
      var path = Path.Combine(GetAppDataDirectory(), _fileName);

      var options = new JsonSerializerOptions { WriteIndented = true };
      var json = JsonSerializer.Serialize(sessions, options);

      await File.WriteAllTextAsync(path, json);
    }

    public async Task<List<Session>> LoadAsync()
    {
      var path = Path.Combine(GetAppDataDirectory(), _fileName);
      if (!File.Exists(path))
        return new List<Session>();

      var json = await File.ReadAllTextAsync(path);
      var sessions = JsonSerializer.Deserialize<List<Session>>(json)
                     ?? new List<Session>();

      return sessions;
    }
  }
}
