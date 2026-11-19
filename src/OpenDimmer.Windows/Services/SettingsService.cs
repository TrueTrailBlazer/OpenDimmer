using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using OpenDimmer.Models;

namespace OpenDimmer.Services
{
    internal sealed class WindowsJsonCodec : IJsonCodec
    {
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public string Serialize(AppSettings settings)
        {
            return _serializer.Serialize(settings);
        }

        public AppSettings Deserialize(string json)
        {
            return _serializer.Deserialize<AppSettings>(json);
        }
    }

    public sealed class SettingsService
    {
        private readonly SettingsStore _store;

        public string FilePath { get { return _store.ActivePath; } }

        public SettingsService(string filePath)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string fallbackPath = string.IsNullOrEmpty(appData)
                ? null
                : Path.Combine(Path.Combine(appData, "OpenDimmer"), "OpenDimmer.json");
            _store = new SettingsStore(filePath, fallbackPath, new WindowsJsonCodec());
        }

        public AppSettings LoadOrInitialize()
        {
            return _store.LoadOrInitialize(GetConnectedDeviceNames());
        }

        public void SyncScreensNonDestructively(AppSettings settings)
        {
            _store.SyncScreensNonDestructively(settings, GetConnectedDeviceNames());
        }

        public void Save(AppSettings settings)
        {
            _store.Save(settings);
        }

        public static string PrettifyJson(string json)
        {
            return JsonFormatter.PrettifyJson(json);
        }

        private static IList<string> GetConnectedDeviceNames()
        {
            var screens = Screen.AllScreens;
            var names = new List<string>(screens.Length);
            for (int i = 0; i < screens.Length; i++)
            {
                string devName = screens[i].DeviceName;
                names.Add(string.IsNullOrEmpty(devName) ? ("DISPLAY" + (i + 1)) : devName);
            }
            return names;
        }
    }
}
