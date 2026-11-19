using System;
using System.Linq;
using Avalonia;
using OpenDimmer.Linux.Services;
using OpenDimmer.Models;
using OpenDimmer.Services;

namespace OpenDimmer.Linux
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Contains("--diagnostics", StringComparer.OrdinalIgnoreCase))
            {
                return RunDiagnostics();
            }

            bool smokeTest = args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase);

            if (!smokeTest)
            {
                // Pre-check single instance before creating Avalonia lifetime
                using var probe = new SingleInstanceGuard(() => { });
                if (!probe.IsPrimaryInstance)
                {
                    return 0;
                }
                // Dispose probe immediately so App owns the real IPC listener
            }

            AppBuilder builder = AppBuilder.Configure(() => new App(smokeTest))
                .UsePlatformDetect()
                .LogToTrace();

            return builder.StartWithClassicDesktopLifetime(args);
        }

        private static int RunDiagnostics()
        {
            using var displayService = new X11DisplayService();
            var (primaryPath, fallbackPath) = LinuxSettingsPaths.GetConfigPaths();
            var store = new SettingsStore(primaryPath, fallbackPath, new LinuxJsonCodec());
            var connectedNames = displayService.Monitors.Select(m => m.Descriptor.DeviceName).ToList();
            AppSettings settings = store.LoadOrInitialize(connectedNames);

            Console.WriteLine(displayService.BuildDiagnosticsReport(settings, store.ActivePath));
            return 0;
        }
    }
}
