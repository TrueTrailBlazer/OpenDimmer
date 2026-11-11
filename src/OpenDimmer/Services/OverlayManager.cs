using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Threading;
using Microsoft.Win32;
using OpenDimmer.Models;
using OpenDimmer.UI;

namespace OpenDimmer.Services
{
    public sealed class OverlayManager : IDisposable
    {
        private readonly List<OverlayWindow> _overlays = new List<OverlayWindow>();
        private readonly AppSettings _settings;
        private readonly SettingsService _settingsService;
        private readonly DispatcherTimer _topmostWatchdog;
        private NativeMethods.WinEventDelegate _winEventDelegate;
        private IntPtr _foregroundHook;
        private IntPtr _objectShowHook;
        private IntPtr _controlPanelHwnd;

        public IReadOnlyList<OverlayWindow> Overlays { get { return _overlays; } }
        public event EventHandler DisplaysReconfigured;

        public OverlayManager(AppSettings settings, SettingsService settingsService)
        {
            _settings = settings;
            _settingsService = settingsService;

            BuildOverlaysForCurrentScreens();
            InstallWinEventHooks();

            _topmostWatchdog = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(750)
            };
            _topmostWatchdog.Tick += (s, e) => EnforceAllTopmost();
            _topmostWatchdog.Start();

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        public void SetControlPanelHandle(IntPtr hwnd)
        {
            _controlPanelHwnd = hwnd;
        }

        public void BuildOverlaysForCurrentScreens()
        {
            foreach (var old in _overlays)
            {
                try { old.Close(); } catch { }
            }
            _overlays.Clear();

            _settingsService.SyncScreensNonDestructively(_settings);
            var screens = Screen.AllScreens;

            for (int i = 0; i < screens.Length; i++)
            {
                int id = i + 1;
                var screen = screens[i];
                var cfg = _settings.Screens.FirstOrDefault(s =>
                    string.Equals(s.DeviceName, screen.DeviceName, StringComparison.OrdinalIgnoreCase))
                    ?? _settings.Screens.First(s => s.Id == id);

                var overlay = new OverlayWindow(id, screen.DeviceName, screen.Bounds);
                overlay.Show();
                overlay.UpdateBounds(screen.Bounds);
                overlay.ApplyDimming(cfg.On, cfg.Dim, cfg.Tint);
                overlay.SetIndicators(_settings.ShowIds, _settings.ShowBoundaries);
                _overlays.Add(overlay);
            }
        }

        public void RefreshOverlay(int screenId)
        {
            var cfg = _settings.Screens.FirstOrDefault(s => s.Id == screenId);
            var overlay = _overlays.FirstOrDefault(o => o.ScreenId == screenId);
            if (cfg != null && overlay != null)
            {
                overlay.ApplyDimming(cfg.On, cfg.Dim, cfg.Tint);
                PromoteControlPanelIfNeeded();
            }
        }

        public void RefreshAllOverlays()
        {
            foreach (var overlay in _overlays)
            {
                var cfg = _settings.Screens.FirstOrDefault(s => s.Id == overlay.ScreenId);
                if (cfg != null)
                {
                    overlay.ApplyDimming(cfg.On, cfg.Dim, cfg.Tint);
                }
                overlay.SetIndicators(_settings.ShowIds, _settings.ShowBoundaries);
            }
            PromoteControlPanelIfNeeded();
        }

        public void EnforceAllTopmost()
        {
            foreach (var overlay in _overlays)
            {
                overlay.EnsureTopmost();
            }
            PromoteControlPanelIfNeeded();
        }

        private void PromoteControlPanelIfNeeded()
        {
            if (_settings.NonDimmableWindow && _controlPanelHwnd != IntPtr.Zero)
            {
                NativeMethods.SetWindowPos(
                    _controlPanelHwnd,
                    NativeMethods.HWND_TOPMOST,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
            }
        }

        private void InstallWinEventHooks()
        {
            _winEventDelegate = OnWinEvent;
            uint flags = NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS;

            _foregroundHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _winEventDelegate,
                0, 0, flags);

            _objectShowHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_OBJECT_SHOW,
                NativeMethods.EVENT_OBJECT_SHOW,
                IntPtr.Zero,
                _winEventDelegate,
                0, 0, flags);
        }

        private void OnWinEvent(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            if (idObject != 0 || hwnd == IntPtr.Zero)
                return;

            EnforceAllTopmost();
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            BuildOverlaysForCurrentScreens();
            var handler = DisplaysReconfigured;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                BuildOverlaysForCurrentScreens();
                var handler = DisplaysReconfigured;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            _topmostWatchdog.Stop();
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;

            if (_foregroundHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_foregroundHook);
                _foregroundHook = IntPtr.Zero;
            }

            if (_objectShowHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_objectShowHook);
                _objectShowHook = IntPtr.Zero;
            }

            foreach (var overlay in _overlays)
            {
                try { overlay.Close(); } catch { }
            }
            _overlays.Clear();
        }
    }
}
