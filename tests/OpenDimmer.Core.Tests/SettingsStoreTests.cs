using System;
using System.IO;
using System.Text.Json;
using OpenDimmer.Models;
using OpenDimmer.Services;
using Xunit;

namespace OpenDimmer.Core.Tests
{
    internal sealed class TestJsonCodec : IJsonCodec
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public string Serialize(AppSettings settings) => JsonSerializer.Serialize(settings, Options);

        public AppSettings Deserialize(string json) =>
            JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
    }

    public class SettingsStoreTests : IDisposable
    {
        private readonly string _tempDir;

        public SettingsStoreTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "OpenDimmer_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, recursive: true);
                }
            }
            catch
            {
                // Ignore temp cleanup races
            }
        }

        [Fact]
        public void SyncScreensNonDestructively_PreservesDisconnectedMonitorSettingsAcrossHotplug()
        {
            string primary = Path.Combine(_tempDir, "OpenDimmer.json");
            var store = new SettingsStore(primary, null, new TestJsonCodec());

            var settings = store.LoadOrInitialize(new[] { "eDP-1", "HDMI-1" });
            settings.Screens[0].Dim = 25;
            settings.Screens[1].Dim = 70;
            settings.Screens[1].Tint = "2B1300";
            settings.Screens[1].On = true;
            store.Save(settings);

            // Unplug HDMI-1: only eDP-1 connected
            store.SyncScreensNonDestructively(settings, new[] { "eDP-1" });
            Assert.Equal(2, settings.Screens.Count);
            Assert.Equal("eDP-1", settings.Screens[0].DeviceName);
            Assert.Equal("HDMI-1", settings.Screens[1].DeviceName);
            Assert.Equal(70, settings.Screens[1].Dim);

            // Replug HDMI-1 as first display (order flipped!)
            store.SyncScreensNonDestructively(settings, new[] { "HDMI-1", "eDP-1" });
            Assert.Equal("HDMI-1", settings.Screens[0].DeviceName);
            Assert.Equal(1, settings.Screens[0].Id);
            Assert.Equal(70, settings.Screens[0].Dim);
            Assert.Equal("2B1300", settings.Screens[0].Tint);
            Assert.Equal("eDP-1", settings.Screens[1].DeviceName);
            Assert.Equal(2, settings.Screens[1].Id);
            Assert.Equal(25, settings.Screens[1].Dim);
        }

        [Fact]
        public void Save_FallsBackToFallbackPathWhenPrimaryPathUnwritable()
        {
            // Point primaryPath inside an existing file so CreateDirectory/WriteAllText fails cleanly
            string blockerFile = Path.Combine(_tempDir, "readonly_blocker");
            File.WriteAllText(blockerFile, "locked");
            string badPrimary = Path.Combine(blockerFile, "OpenDimmer.json");
            string fallback = Path.Combine(_tempDir, "appdata", "OpenDimmer.json");

            var store = new SettingsStore(badPrimary, fallback, new TestJsonCodec());
            var settings = store.LoadOrInitialize(new[] { "HDMI-1" });
            settings.Screens[0].Dim = 65;
            store.Save(settings);

            Assert.Equal(fallback, store.ActivePath);
            Assert.True(File.Exists(fallback));

            var reloaded = store.LoadOrInitialize(new[] { "HDMI-1" });
            Assert.Equal(65, reloaded.Screens[0].Dim);
        }
    }
}
