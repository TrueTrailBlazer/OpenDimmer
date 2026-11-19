using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using OpenDimmer.Linux.Services;
using OpenDimmer.Linux.UI;
using OpenDimmer.Models;
using OpenDimmer.Services;

namespace OpenDimmer.Linux
{
    public sealed class App : Application
    {
        private readonly bool _smokeTest;
        private X11DisplayService? _displayService;
        private SingleInstanceGuard? _singleInstance;
        private TrayIcon? _trayIcon;
        private MainWindow? _mainWindow;

        public App(bool smokeTest = false)
        {
            _smokeTest = smokeTest;
        }

        public override void Initialize()
        {
            RequestedThemeVariant = ThemeVariant.Dark;
            Styles.Add(new FluentTheme());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = _smokeTest
                    ? ShutdownMode.OnMainWindowClose
                    : ShutdownMode.OnExplicitShutdown;

                _displayService = new X11DisplayService();
                var (primaryPath, fallbackPath) = LinuxSettingsPaths.GetConfigPaths();
                var store = new SettingsStore(primaryPath, fallbackPath, new LinuxJsonCodec());
                var connectedNames = _displayService.Monitors.Select(m => m.Descriptor.DeviceName).ToList();
                AppSettings settings = store.LoadOrInitialize(connectedNames);

                _mainWindow = new MainWindow(
                    settings,
                    store,
                    _displayService,
                    () => ShutdownCleanly(desktop),
                    applyGamma: !_smokeTest);

                desktop.MainWindow = _mainWindow;

                if (_smokeTest)
                {
                    _mainWindow.Opened += (_, _) =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            _mainWindow.PrepareForShutdown();
                            _mainWindow.Close();
                        }, DispatcherPriority.Background);
                    };
                }
                else
                {
                    _singleInstance = new SingleInstanceGuard(() =>
                    {
                        Dispatcher.UIThread.Post(() => _mainWindow.ShowAndActivate());
                    });

                    SetupTrayIcon(desktop, _mainWindow);
                }

                desktop.Exit += (_, _) => CleanupResources();
                AppDomain.CurrentDomain.ProcessExit += (_, _) => CleanupResources();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Dispatcher.UIThread.Post(() => ShutdownCleanly(desktop));
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, MainWindow mainWindow)
        {
            try
            {
                WindowIcon? icon = LoadEmbeddedIcon();
                var menu = new NativeMenu();

                var openItem = new NativeMenuItem("Open OpenDimmer");
                openItem.Click += (_, _) => mainWindow.ShowAndActivate();
                menu.Items.Add(openItem);

                var toggleItem = new NativeMenuItem("Toggle Master");
                toggleItem.Click += (_, _) => mainWindow.ToggleMasterFromTray();
                menu.Items.Add(toggleItem);

                var nightItem = new NativeMenuItem("Night Sync (4500K)");
                nightItem.Click += (_, _) => mainWindow.TriggerNightSyncFromTray();
                menu.Items.Add(nightItem);

                var reloadItem = new NativeMenuItem("Reload Displays");
                reloadItem.Click += (_, _) => mainWindow.ReloadDisplays();
                menu.Items.Add(reloadItem);

                var resetItem = new NativeMenuItem("Reset window position");
                resetItem.Click += (_, _) =>
                {
                    mainWindow.ShowAndActivate();
                    mainWindow.ResetPositionToCorner();
                };
                menu.Items.Add(resetItem);

                menu.Items.Add(new NativeMenuItemSeparator());

                var closeItem = new NativeMenuItem("Close");
                closeItem.Click += (_, _) => ShutdownCleanly(desktop);
                menu.Items.Add(closeItem);

                _trayIcon = new TrayIcon
                {
                    ToolTipText = "OpenDimmer",
                    Icon = icon,
                    Menu = menu,
                    IsVisible = true
                };
                _trayIcon.Clicked += (_, _) =>
                {
                    if (mainWindow.IsVisible && mainWindow.WindowState != WindowState.Minimized)
                    {
                        mainWindow.Hide();
                    }
                    else
                    {
                        mainWindow.ShowAndActivate();
                    }
                };

                var icons = new TrayIcons { _trayIcon };
                TrayIcon.SetIcons(this, icons);
            }
            catch
            {
                // Tray icon is optional if desktop lacks StatusNotifierItem
            }
        }

        private static WindowIcon? LoadEmbeddedIcon()
        {
            using Stream? stream = typeof(App).Assembly.GetManifestResourceStream("OpenDimmer.icon.png");
            return stream != null ? new WindowIcon(stream) : null;
        }

        private void ShutdownCleanly(IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindow?.PrepareForShutdown();
            CleanupResources();
            desktop.Shutdown();
        }

        private void CleanupResources()
        {
            try { _trayIcon?.Dispose(); } catch { }
            _trayIcon = null;

            try { _singleInstance?.Dispose(); } catch { }
            _singleInstance = null;

            try { _displayService?.Dispose(); } catch { }
            _displayService = null;
        }
    }
}
