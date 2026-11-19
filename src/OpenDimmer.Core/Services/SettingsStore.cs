using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenDimmer.Models;

namespace OpenDimmer.Services
{
    public interface IJsonCodec
    {
        string Serialize(AppSettings settings);
        AppSettings Deserialize(string json);
    }

    public sealed class SettingsStore
    {
        private readonly string _primaryPath;
        private readonly string _fallbackPath;
        private readonly IJsonCodec _codec;
        private string _activePath;

        public string ActivePath { get { return _activePath; } }
        public string PrimaryPath { get { return _primaryPath; } }
        public string FallbackPath { get { return _fallbackPath; } }

        public SettingsStore(string primaryPath, string fallbackPath, IJsonCodec codec)
        {
            if (string.IsNullOrEmpty(primaryPath)) throw new ArgumentNullException("primaryPath");
            if (codec == null) throw new ArgumentNullException("codec");

            _primaryPath = primaryPath;
            _fallbackPath = fallbackPath;
            _codec = codec;
            _activePath = primaryPath;
        }

        public AppSettings LoadOrInitialize(IList<string> connectedDeviceNames)
        {
            AppSettings settings = TryLoadFromPath(_primaryPath);
            if (settings != null)
            {
                _activePath = _primaryPath;
            }
            else if (!string.IsNullOrEmpty(_fallbackPath))
            {
                settings = TryLoadFromPath(_fallbackPath);
                if (settings != null)
                {
                    _activePath = _fallbackPath;
                }
            }

            if (settings == null)
            {
                settings = new AppSettings();
            }

            if (settings.Position == null) settings.Position = new WindowPosition();
            if (settings.Master == null) settings.Master = new MasterSettings();
            if (settings.Screens == null) settings.Screens = new List<ScreenSettings>();
            if (settings.MaxDimPercent <= 0) settings.MaxDimPercent = 90;

            SyncScreensNonDestructively(settings, connectedDeviceNames);
            Save(settings);
            return settings;
        }

        private AppSettings TryLoadFromPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                return _codec.Deserialize(json);
            }
            catch
            {
                return null;
            }
        }

        public void SyncScreensNonDestructively(AppSettings settings, IList<string> connectedDeviceNames)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            if (settings.Screens == null) settings.Screens = new List<ScreenSettings>();
            if (connectedDeviceNames == null) return;

            var connectedEntries = new List<ScreenSettings>(connectedDeviceNames.Count);
            var matchedEntries = new HashSet<ScreenSettings>();

            for (int i = 0; i < connectedDeviceNames.Count; i++)
            {
                int id = i + 1;
                string devName = string.IsNullOrEmpty(connectedDeviceNames[i])
                    ? ("DISPLAY" + id)
                    : connectedDeviceNames[i];

                ScreenSettings existing = null;
                for (int j = 0; j < settings.Screens.Count; j++)
                {
                    ScreenSettings candidate = settings.Screens[j];
                    if (string.Equals(candidate.DeviceName, devName, StringComparison.OrdinalIgnoreCase))
                    {
                        existing = candidate;
                        break;
                    }
                }

                // Legacy migration only: adopt an entry with empty DeviceName matching Id
                if (existing == null)
                {
                    for (int j = 0; j < settings.Screens.Count; j++)
                    {
                        ScreenSettings candidate = settings.Screens[j];
                        if (string.IsNullOrEmpty(candidate.DeviceName) && candidate.Id == id)
                        {
                            existing = candidate;
                            existing.DeviceName = devName;
                            break;
                        }
                    }
                }

                if (existing == null)
                {
                    existing = new ScreenSettings
                    {
                        Id = id,
                        DeviceName = devName,
                        On = settings.Master != null && settings.Master.On,
                        Dim = settings.Master != null ? settings.Master.Dim : 30,
                        Tint = "000000",
                        TintIntensity = 60
                    };
                }
                else
                {
                    existing.Id = id;
                    existing.DeviceName = devName;
                    if (existing.TintIntensity <= 0) existing.TintIntensity = 60;
                    if (string.IsNullOrEmpty(existing.Tint)) existing.Tint = "000000";
                }

                connectedEntries.Add(existing);
                matchedEntries.Add(existing);
            }

            // Preserve disconnected screens cleanly at higher IDs so their Dim/Tint never get wiped
            int nextDisconnectedId = connectedEntries.Count + 1;
            var disconnectedEntries = new List<ScreenSettings>();
            for (int j = 0; j < settings.Screens.Count; j++)
            {
                ScreenSettings item = settings.Screens[j];
                if (!matchedEntries.Contains(item))
                {
                    item.Id = nextDisconnectedId++;
                    disconnectedEntries.Add(item);
                }
            }

            settings.Screens.Clear();
            settings.Screens.AddRange(connectedEntries);
            settings.Screens.AddRange(disconnectedEntries);
        }

        public void Save(AppSettings settings)
        {
            if (settings == null) return;

            settings.LastRun = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string rawJson = _codec.Serialize(settings);
            string prettyJson = JsonFormatter.PrettifyJson(rawJson);

            if (TryWriteFile(_activePath, prettyJson))
            {
                return;
            }

            if (!string.IsNullOrEmpty(_fallbackPath)
                && !string.Equals(_activePath, _fallbackPath, StringComparison.Ordinal))
            {
                if (TryWriteFile(_fallbackPath, prettyJson))
                {
                    _activePath = _fallbackPath;
                }
            }
        }

        private static bool TryWriteFile(string path, string content)
        {
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(path, content, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
