using System;
using System.IO;
using System.Text.Json;
using OpenDimmer.Models;
using OpenDimmer.Services;

namespace OpenDimmer.Linux.Services
{
    public sealed class LinuxJsonCodec : IJsonCodec
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public string Serialize(AppSettings settings) =>
            JsonSerializer.Serialize(settings, Options);

        public AppSettings Deserialize(string json) =>
            JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
    }

    public static class LinuxSettingsPaths
    {
        public static (string primary, string fallback) GetConfigPaths()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(appData))
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                appData = Path.Combine(home, ".config");
            }

            string primary = Path.Combine(appData, "OpenDimmer", "OpenDimmer.json");
            string fallback = Path.Combine(Path.GetTempPath(), "OpenDimmer.json");
            return (primary, fallback);
        }
    }
}
