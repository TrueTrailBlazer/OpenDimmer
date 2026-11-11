using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using OpenDimmer.Models;
using OpenDimmer.Services;
using DrawingColor = System.Drawing.Color;
using Forms = System.Windows.Forms;
using MediaColor = System.Windows.Media.Color;

namespace OpenDimmer.UI
{
    public sealed class MainWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly SettingsService _settingsService;
        private readonly OverlayManager _overlayManager;
        private Forms.NotifyIcon _notifyIcon;

        private StackPanel _screensPanel;
        private CheckBox _chkMaster;
        private Slider _sldMaster;
        private TextBox _txtMaster;
        private bool _suppressEvents;

        private sealed class ScreenStripControls
        {
            public int ScreenId;
            public CheckBox CheckBox;
            public Slider Slider;
            public TextBox TextBox;
        }

        private readonly List<ScreenStripControls> _screenStrips = new List<ScreenStripControls>();

        public MainWindow(AppSettings settings, SettingsService settingsService, OverlayManager overlayManager)
        {
            _settings = settings;
            _settingsService = settingsService;
            _overlayManager = overlayManager;

            Title = "OpenDimmer";
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = new SolidColorBrush(MediaColor.FromRgb(28, 34, 42));
            BorderBrush = new SolidColorBrush(MediaColor.FromRgb(15, 19, 24));
            BorderThickness = new Thickness(1);
            Height = 290;
            Topmost = _settings.NonDimmableWindow;
            ShowInTaskbar = _settings.ShowInTaskbar;

            BuildUserInterface();
            BuildSystemTrayIcon();
            RestoreOrResetPosition();

            _overlayManager.DisplaysReconfigured += (s, e) =>
            {
                Dispatcher.Invoke(RebuildScreenStrips);
            };

            MouseLeftButtonDown += (s, e) =>
            {
                if (_settings.MovableWindow && e.ButtonState == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            };

            if (_settings.StartMinimised)
            {
                Visibility = Visibility.Hidden;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            _overlayManager.SetControlPanelHandle(hwnd);
            _overlayManager.EnforceAllTopmost();
        }

        private void BuildUserInterface()
        {
            var root = new Grid { Margin = new Thickness(8) };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new DockPanel { LastChildFill = false };
            var titleText = new TextBlock
            {
                Text = "OpenDimmer",
                Foreground = new SolidColorBrush(MediaColor.FromRgb(200, 210, 220)),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            DockPanel.SetDock(titleText, Dock.Left);
            header.Children.Add(titleText);

            var btnClose = CreateTitleButton("✕", "Close / Hide to tray");
            btnClose.Click += OnBtnCloseClick;
            DockPanel.SetDock(btnClose, Dock.Right);
            header.Children.Add(btnClose);

            var btnMin = CreateTitleButton("—", "Minimise");
            btnMin.Click += OnBtnMinimiseClick;
            DockPanel.SetDock(btnMin, Dock.Right);
            header.Children.Add(btnMin);

            Grid.SetRow(header, 0);
            root.Children.Add(header);

            _screensPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 6, 0, 0)
            };

            Grid.SetRow(_screensPanel, 1);
            root.Children.Add(_screensPanel);
            Content = root;

            RebuildScreenStrips();
        }

        private Button CreateTitleButton(string caption, string tooltip)
        {
            return new Button
            {
                Content = caption,
                ToolTip = tooltip,
                Width = 26,
                Height = 22,
                Margin = new Thickness(2, 0, 0, 0),
                Background = new SolidColorBrush(MediaColor.FromRgb(40, 48, 58)),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(210, 220, 230)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }

        private void RebuildScreenStrips()
        {
            _suppressEvents = true;
            _screensPanel.Children.Clear();
            _screenStrips.Clear();

            var masterCard = BuildStripCard("M", _settings.Master.On, _settings.Master.Dim,
                out _chkMaster, out _sldMaster, out _txtMaster);

            _chkMaster.Click += (s, e) => OnMasterToggled();
            _sldMaster.ValueChanged += (s, e) => OnMasterValueChanged((int)Math.Round(e.NewValue));
            _screensPanel.Children.Add(masterCard);

            int activeCount = Forms.Screen.AllScreens.Length;
            for (int i = 0; i < activeCount; i++)
            {
                int screenId = i + 1;
                var cfg = _settings.Screens.FirstOrDefault(x => x.Id == screenId);
                if (cfg == null) continue;

                CheckBox chk;
                Slider sld;
                TextBox txt;
                var card = BuildStripCard(screenId.ToString(), cfg.On, cfg.Dim, out chk, out sld, out txt);

                var strip = new ScreenStripControls
                {
                    ScreenId = screenId,
                    CheckBox = chk,
                    Slider = sld,
                    TextBox = txt
                };
                _screenStrips.Add(strip);

                int capturedId = screenId;
                chk.Click += (s, e) => OnScreenToggled(capturedId);
                sld.ValueChanged += (s, e) => OnScreenValueChanged(capturedId, (int)Math.Round(e.NewValue));

                _screensPanel.Children.Add(card);
            }

            Width = Math.Max(220, (activeCount + 1) * 74 + 28);
            _suppressEvents = false;
        }

        private Border BuildStripCard(
            string label,
            bool isOn,
            int dimValue,
            out CheckBox checkBox,
            out Slider slider,
            out TextBox textBox)
        {
            var card = new Border
            {
                Width = 66,
                Margin = new Thickness(3),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(MediaColor.FromRgb(35, 43, 53)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(22, 27, 34)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 6, 4, 6)
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            checkBox = new CheckBox
            {
                Content = label,
                IsChecked = isOn,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            slider = new Slider
            {
                Orientation = Orientation.Vertical,
                Height = 148,
                Minimum = 0,
                Maximum = 90,
                SmallChange = 1,
                LargeChange = 5,
                TickFrequency = 10,
                TickPlacement = TickPlacement.Both,
                Value = dimValue,
                IsEnabled = isOn,
                Opacity = isOn ? 1.0 : 0.4,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            textBox = new TextBox
            {
                Width = 38,
                Height = 22,
                Margin = new Thickness(0, 8, 0, 0),
                Text = dimValue.ToString(),
                IsEnabled = isOn,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(MediaColor.FromRgb(210, 215, 222)),
                FontWeight = FontWeights.Medium
            };

            Slider localSlider = slider;
            CheckBox localCheck = checkBox;
            card.PreviewMouseWheel += (s, e) =>
            {
                if (localCheck.IsChecked == true)
                {
                    int delta = e.Delta > 0 ? 2 : -2;
                    localSlider.Value = Math.Max(0, Math.Min(90, localSlider.Value + delta));
                    e.Handled = true;
                }
            };

            stack.Children.Add(checkBox);
            stack.Children.Add(slider);
            stack.Children.Add(textBox);
            card.Child = stack;

            return card;
        }

        private void OnMasterToggled()
        {
            if (_suppressEvents) return;
            bool on = _chkMaster.IsChecked == true;
            _settings.Master.On = on;
            _sldMaster.IsEnabled = on;
            _sldMaster.Opacity = on ? 1.0 : 0.4;
            _txtMaster.IsEnabled = on;

            _suppressEvents = true;
            foreach (var strip in _screenStrips)
            {
                strip.CheckBox.IsChecked = on;
                strip.Slider.IsEnabled = on;
                strip.Slider.Opacity = on ? 1.0 : 0.4;
                strip.TextBox.IsEnabled = on;

                var cfg = _settings.Screens.FirstOrDefault(x => x.Id == strip.ScreenId);
                if (cfg != null) cfg.On = on;
            }
            _suppressEvents = false;

            _overlayManager.RefreshAllOverlays();
            _settingsService.Save(_settings);
        }

        private void OnMasterValueChanged(int newMasterValue)
        {
            if (_suppressEvents) return;
            int delta = newMasterValue - _settings.Master.Dim;
            _settings.Master.Dim = newMasterValue;
            _txtMaster.Text = newMasterValue.ToString();

            _suppressEvents = true;
            foreach (var strip in _screenStrips)
            {
                var cfg = _settings.Screens.FirstOrDefault(x => x.Id == strip.ScreenId);
                if (cfg == null) continue;

                int updated = _settings.Master.Proportional
                    ? Math.Max(0, Math.Min(90, cfg.Dim + delta))
                    : newMasterValue;

                cfg.Dim = updated;
                strip.Slider.Value = updated;
                strip.TextBox.Text = updated.ToString();
            }
            _suppressEvents = false;

            _overlayManager.RefreshAllOverlays();
            _settingsService.Save(_settings);
        }

        private void OnScreenToggled(int screenId)
        {
            if (_suppressEvents) return;
            var strip = _screenStrips.FirstOrDefault(x => x.ScreenId == screenId);
            var cfg = _settings.Screens.FirstOrDefault(x => x.Id == screenId);
            if (strip == null || cfg == null) return;

            bool on = strip.CheckBox.IsChecked == true;
            cfg.On = on;
            strip.Slider.IsEnabled = on;
            strip.Slider.Opacity = on ? 1.0 : 0.4;
            strip.TextBox.IsEnabled = on;

            _overlayManager.RefreshOverlay(screenId);
            _settingsService.Save(_settings);
        }

        private void OnScreenValueChanged(int screenId, int newValue)
        {
            if (_suppressEvents) return;
            var strip = _screenStrips.FirstOrDefault(x => x.ScreenId == screenId);
            var cfg = _settings.Screens.FirstOrDefault(x => x.Id == screenId);
            if (strip == null || cfg == null) return;

            cfg.Dim = newValue;
            strip.TextBox.Text = newValue.ToString();

            _overlayManager.RefreshOverlay(screenId);
            _settingsService.Save(_settings);
        }

        private void BuildSystemTrayIcon()
        {
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open OpenDimmer", null, (s, e) => ShowAndActivateWindow());
            menu.Items.Add("Reset Position", null, (s, e) => ResetPositionToCorner());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => Application.Current.Shutdown());

            _notifyIcon = new Forms.NotifyIcon
            {
                Text = "OpenDimmer",
                Icon = SystemIcons.Application,
                ContextMenuStrip = menu,
                Visible = true
            };

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == Forms.MouseButtons.Left)
                {
                    if (IsVisible && WindowState != WindowState.Minimized)
                        Hide();
                    else
                        ShowAndActivateWindow();
                }
            };
        }

        public void ShowAndActivateWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _overlayManager.EnforceAllTopmost();
        }

        private void RestoreOrResetPosition()
        {
            if (_settings.Position.Left.HasValue && _settings.Position.Top.HasValue)
            {
                Left = _settings.Position.Left.Value;
                Top = _settings.Position.Top.Value;
            }
            else
            {
                ResetPositionToCorner();
            }
        }

        private void ResetPositionToCorner()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 16;
            Top = workArea.Bottom - Height - 16;
        }

        private void OnBtnMinimiseClick(object sender, RoutedEventArgs e)
        {
            if (_settings.ShowInTaskbar)
                WindowState = WindowState.Minimized;
            else
                Hide();
        }

        private void OnBtnCloseClick(object sender, RoutedEventArgs e)
        {
            SaveWindowPosition();
            if (_settings.CloseHidesToTray)
            {
                Hide();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void SaveWindowPosition()
        {
            _settings.Position.Left = Left;
            _settings.Position.Top = Top;
            _settingsService.Save(_settings);
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveWindowPosition();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            _overlayManager.Dispose();
            base.OnClosed(e);
        }
    }
}
