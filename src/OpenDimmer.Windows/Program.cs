using System;
using System.IO;
using System.Threading;
using System.Windows;
using OpenDimmer.Services;
using OpenDimmer.UI;

namespace OpenDimmer
{
    internal static class Program
    {
        private const string MutexName = "Global\\OpenDimmer_SingleInstance_Mutex";
        private const string WakeEventName = "Global\\OpenDimmer_Wake_Event";

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    try
                    {
                        using (var wakeEvent = EventWaitHandle.OpenExisting(WakeEventName))
                        {
                            wakeEvent.Set();
                        }
                    }
                    catch
                    {
                        // Existing instance will remain active
                    }
                    return;
                }

                var app = new Application
                {
                    ShutdownMode = ShutdownMode.OnMainWindowClose
                };

                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenDimmer.json");
                var settingsService = new SettingsService(settingsPath);
                var settings = settingsService.LoadOrInitialize();
                var overlayManager = new OverlayManager(settings, settingsService);
                var mainWindow = new MainWindow(settings, settingsService, overlayManager);

                using (var wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WakeEventName))
                {
                    ThreadPool.RegisterWaitForSingleObject(
                        wakeEvent,
                        (state, timedOut) =>
                        {
                            app.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                mainWindow.ShowAndActivateWindow();
                            }));
                        },
                        null,
                        -1,
                        false);

                    app.Run(mainWindow);
                }
            }
        }
    }
}
