using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using OpenDimmer.Models;

namespace OpenDimmer.Services
{
    public sealed class SettingsService
    {
        private readonly string _filePath;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public SettingsService(string filePath)
        {
            _filePath = filePath;
        }

        public AppSettings LoadOrInitialize()
        {
            AppSettings settings = null;
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath, Encoding.UTF8);
                    settings = _serializer.Deserialize<AppSettings>(json);
                }
                catch
                {
                    settings = null;
                }
            }

            if (settings == null)
            {
                settings = new AppSettings();
            }

            SyncScreensNonDestructively(settings);
            return settings;
        }

        public void SyncScreensNonDestructively(AppSettings settings)
        {
            var physicalScreens = Screen.AllScreens;
            for (int i = 0; i < physicalScreens.Length; i++)
            {
                int id = i + 1;
                string devName = physicalScreens[i].DeviceName ?? ("DISPLAY" + id);

                var existing = settings.Screens.FirstOrDefault(s =>
                    string.Equals(s.DeviceName, devName, StringComparison.OrdinalIgnoreCase))
                    ?? settings.Screens.FirstOrDefault(s => s.Id == id);

                if (existing == null)
                {
                    settings.Screens.Add(new ScreenSettings
                    {
                        Id = id,
                        DeviceName = devName,
                        On = false,
                        Dim = 30,
                        Tint = "000000"
                    });
                }
                else
                {
                    existing.Id = id;
                    existing.DeviceName = devName;
                }
            }
        }

        public void Save(AppSettings settings)
        {
            try
            {
                settings.LastRun = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string json = _serializer.Serialize(settings);
                File.WriteAllText(_filePath, json, Encoding.UTF8);
            }
            catch
            {
                // Non-fatal persistence failure
            }
        }
    }
}
