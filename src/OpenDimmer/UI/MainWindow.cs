using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using OpenDimmer.Models;
using OpenDimmer.Services;
using DrawingColor = System.Drawing.Color;
using DrawingPen = System.Drawing.Pen;
using Forms = System.Windows.Forms;
using MediaColor = System.Windows.Media.Color;

namespace OpenDimmer.UI
{
    internal sealed class DarkMenuColorTable : Forms.ProfessionalColorTable
    {
        public override DrawingColor MenuBorder { get { return DrawingColor.FromArgb(24, 28, 34); } }
        public override DrawingColor MenuItemBorder { get { return DrawingColor.FromArgb(58, 122, 254); } }
        public override DrawingColor MenuItemSelected { get { return DrawingColor.FromArgb(52, 62, 76); } }
        public override DrawingColor ToolStripDropDownBackground { get { return DrawingColor.FromArgb(32, 38, 46); } }
        public override DrawingColor ImageMarginGradientBegin { get { return DrawingColor.FromArgb(32, 38, 46); } }
        public override DrawingColor ImageMarginGradientMiddle { get { return DrawingColor.FromArgb(32, 38, 46); } }
        public override DrawingColor ImageMarginGradientEnd { get { return DrawingColor.FromArgb(32, 38, 46); } }
        public override DrawingColor SeparatorDark { get { return DrawingColor.FromArgb(55, 64, 76); } }
        public override DrawingColor SeparatorLight { get { return DrawingColor.FromArgb(32, 38, 46); } }
    }

    internal sealed class DarkTrayMenuRenderer : Forms.ToolStripProfessionalRenderer
    {
        public DarkTrayMenuRenderer() : base(new DarkMenuColorTable()) { }

        protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected
                ? DrawingColor.White
                : DrawingColor.FromArgb(215, 224, 234);
            base.OnRenderItemText(e);
        }
    }

    public sealed class MainWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly SettingsService _settingsService;
        private readonly OverlayManager _overlayManager;
        private Forms.NotifyIcon _notifyIcon;
        private IntPtr _trayIconHandle;

        private StackPanel _screensPanel;
        private Border _optionsView;
        private Border _aboutView;
        private Border _debugView;
        private TextBox _txtDebug;
        private Button _tabBtnScreens;
        private Button _tabBtnOptions;
        private Button _tabBtnAbout;
        private Button _tabBtnDebug;

        private CheckBox _chkMaster;
        private Slider _sldMaster;
        private TextBox _txtMaster;
        private bool _suppressEvents;

        private static readonly string[] TintCodes = new[] { "000000", "2B1300", "330D00", "1F190D" };
        private static readonly string[] TintNames = new[] { "Neutral Black", "Warm Amber (Night Light)", "Deep Ember", "Soft Sepia" };

        private sealed class ScreenStripControls
        {
            public int ScreenId;
            public CheckBox CheckBox;
            public Slider Slider;
            public TextBox TextBox;
            public Button TintButton;
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
            Height = 326;
            Topmost = _settings.NonDimmableWindow;
            ShowInTaskbar = _settings.ShowInTaskbar;

            AutoRunService.SetAutoRun(_settings.AutoRun);

            BuildUserInterface();
            BuildSystemTrayIcon();
            RestoreOrResetPosition();

            _overlayManager.DisplaysReconfigured += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    RebuildScreenStrips();
                    PopulateDebugLog();
                });
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
            PopulateDebugLog();
            NativeMethods.TrimMemoryWorkingSet();
        }

        private void BuildUserInterface()
        {
            var root = new Grid { Margin = new Thickness(8) };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Row 0: Title Bar
            var header = new DockPanel { LastChildFill = false };
            var titleText = new TextBlock
            {
                Text = "OpenDimmer",
                Foreground = new SolidColorBrush(MediaColor.FromRgb(215, 224, 234)),
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

            // Row 1: Tab Navigation Bar
            var navBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(2, 2, 2, 4)
            };

            _tabBtnScreens = CreateTabButton("Screens", true);
            _tabBtnOptions = CreateTabButton("Options", false);
            _tabBtnAbout = CreateTabButton("About", false);
            _tabBtnDebug = CreateTabButton("Debug", false);

            _tabBtnScreens.Click += (s, e) => SwitchView("screens");
            _tabBtnOptions.Click += (s, e) => SwitchView("options");
            _tabBtnAbout.Click += (s, e) => SwitchView("about");
            _tabBtnDebug.Click += (s, e) =>
            {
                PopulateDebugLog();
                SwitchView("debug");
            };

            navBar.Children.Add(_tabBtnScreens);
            navBar.Children.Add(_tabBtnOptions);
            navBar.Children.Add(_tabBtnAbout);
            navBar.Children.Add(_tabBtnDebug);

            Grid.SetRow(navBar, 1);
            root.Children.Add(navBar);

            // Row 2: Content Area
            var contentGrid = new Grid();

            _screensPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 0)
            };

            _optionsView = BuildOptionsView();
            _aboutView = BuildAboutView();
            _debugView = BuildDebugView();

            contentGrid.Children.Add(_screensPanel);
            contentGrid.Children.Add(_optionsView);
            contentGrid.Children.Add(_aboutView);
            contentGrid.Children.Add(_debugView);

            Grid.SetRow(contentGrid, 2);
            root.Children.Add(contentGrid);
            Content = root;

            RebuildScreenStrips();
            SwitchView("screens");
        }

        private void SwitchView(string view)
        {
            bool isScreens = view == "screens";
            bool isOptions = view == "options";
            bool isAbout = view == "about";
            bool isDebug = view == "debug";

            _screensPanel.Visibility = isScreens ? Visibility.Visible : Visibility.Collapsed;
            _optionsView.Visibility = isOptions ? Visibility.Visible : Visibility.Collapsed;
            _aboutView.Visibility = isAbout ? Visibility.Visible : Visibility.Collapsed;
            _debugView.Visibility = isDebug ? Visibility.Visible : Visibility.Collapsed;

            StyleTabButton(_tabBtnScreens, isScreens);
            StyleTabButton(_tabBtnOptions, isOptions);
            StyleTabButton(_tabBtnAbout, isAbout);
            StyleTabButton(_tabBtnDebug, isDebug);

            int activeCount = Forms.Screen.AllScreens.Length;
            int stripsWidth = Math.Max(246, (activeCount + 1) * 74 + 24);
            Width = isScreens ? stripsWidth : Math.Max(296, stripsWidth);
        }

        private Button CreateTabButton(string text, bool active)
        {
            var btn = new Button
            {
                Content = text,
                Height = 22,
                Padding = new Thickness(8, 0, 8, 0),
                Margin = new Thickness(0, 0, 4, 0),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 11.5
            };
            StyleTabButton(btn, active);
            return btn;
        }

        private void StyleTabButton(Button btn, bool active)
        {
            btn.Background = new SolidColorBrush(active
                ? MediaColor.FromRgb(58, 122, 254)
                : MediaColor.FromRgb(38, 46, 56));
            btn.Foreground = new SolidColorBrush(active
                ? Colors.White
                : MediaColor.FromRgb(185, 195, 208));
            btn.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
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

            Button masterDummyTint;
            var masterCard = BuildStripCard("M", "Master Control", _settings.Master.On, _settings.Master.Dim, null,
                out _chkMaster, out _sldMaster, out _txtMaster, out masterDummyTint);

            _chkMaster.Click += (s, e) => OnMasterToggled();
            _sldMaster.ValueChanged += (s, e) => OnMasterValueChanged((int)Math.Round(e.NewValue));
            BindTextBoxToSlider(_txtMaster, _sldMaster);
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
                Button btnTint;
                var card = BuildStripCard(screenId.ToString(), "Screen #" + screenId + " (" + cfg.DeviceName + ")", cfg.On, cfg.Dim, cfg.Tint,
                    out chk, out sld, out txt, out btnTint);

                var strip = new ScreenStripControls
                {
                    ScreenId = screenId,
                    CheckBox = chk,
                    Slider = sld,
                    TextBox = txt,
                    TintButton = btnTint
                };
                _screenStrips.Add(strip);

                int capturedId = screenId;
                chk.Click += (s, e) => OnScreenToggled(capturedId);
                sld.ValueChanged += (s, e) => OnScreenValueChanged(capturedId, (int)Math.Round(e.NewValue));
                BindTextBoxToSlider(txt, sld);

                if (btnTint != null)
                {
                    btnTint.Click += (s, e) => OnScreenTintCycled(capturedId);
                }

                _screensPanel.Children.Add(card);
            }

            if (_screensPanel.Visibility == Visibility.Visible)
            {
                Width = Math.Max(246, (activeCount + 1) * 74 + 24);
            }
            _suppressEvents = false;
        }

        private void BindTextBoxToSlider(TextBox textBox, Slider slider)
        {
            textBox.TextChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                int parsed;
                if (int.TryParse(textBox.Text.Trim(), out parsed))
                {
                    int clamped = Math.Max(0, Math.Min(90, parsed));
                    if ((int)Math.Round(slider.Value) != clamped)
                    {
                        slider.Value = clamped;
                    }
                }
            };

            textBox.LostFocus += (s, e) =>
            {
                textBox.Text = ((int)Math.Round(slider.Value)).ToString();
            };
        }

        private Border BuildStripCard(
            string label,
            string tooltip,
            bool isOn,
            int dimValue,
            string tintHex,
            out CheckBox checkBox,
            out Slider slider,
            out TextBox textBox,
            out Button tintButton)
        {
            var card = new Border
            {
                Width = 66,
                Margin = new Thickness(3, 1, 3, 1),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(MediaColor.FromRgb(35, 43, 53)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(22, 27, 34)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 5, 4, 5)
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            checkBox = new CheckBox
            {
                Content = label,
                ToolTip = tooltip,
                IsChecked = isOn,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };

            slider = new Slider
            {
                Orientation = Orientation.Vertical,
                Height = 136,
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
                Height = 20,
                Margin = new Thickness(0, 6, 0, 0),
                Text = dimValue.ToString(),
                IsEnabled = isOn,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(MediaColor.FromRgb(210, 215, 222)),
                FontWeight = FontWeights.Medium,
                FontSize = 11.5
            };

            stack.Children.Add(checkBox);
            stack.Children.Add(slider);
            stack.Children.Add(textBox);

            if (tintHex != null)
            {
                tintButton = new Button
                {
                    Width = 38,
                    Height = 14,
                    Margin = new Thickness(0, 5, 0, 0),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(MediaColor.FromRgb(90, 102, 118)),
                    Cursor = Cursors.Hand
                };
                UpdateTintButtonVisual(tintButton, tintHex);
                stack.Children.Add(tintButton);
            }
            else
            {
                tintButton = null;
                var masterTag = new TextBlock
                {
                    Text = "MASTER",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(MediaColor.FromRgb(140, 152, 168)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 6, 0, 0)
                };
                stack.Children.Add(masterTag);
            }

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

            card.Child = stack;
            return card;
        }

        private void UpdateTintButtonVisual(Button btn, string hexTint)
        {
            string code = string.IsNullOrEmpty(hexTint) ? "000000" : hexTint.ToUpperInvariant();
            int idx = Array.IndexOf(TintCodes, code);
            string name = idx >= 0 ? TintNames[idx] : ("Custom #" + code);

            MediaColor displayColor = MediaColor.FromRgb(30, 30, 30);
            if (code == "2B1300") displayColor = MediaColor.FromRgb(214, 122, 34);
            else if (code == "330D00") displayColor = MediaColor.FromRgb(196, 68, 28);
            else if (code == "1F190D") displayColor = MediaColor.FromRgb(168, 142, 82);

            btn.Background = new SolidColorBrush(displayColor);
            btn.ToolTip = "Tint: " + name + " (click to cycle)";
        }

        private void OnScreenTintCycled(int screenId)
        {
            var strip = _screenStrips.FirstOrDefault(x => x.ScreenId == screenId);
            var cfg = _settings.Screens.FirstOrDefault(x => x.Id == screenId);
            if (strip == null || cfg == null || strip.TintButton == null) return;

            string current = string.IsNullOrEmpty(cfg.Tint) ? "000000" : cfg.Tint.ToUpperInvariant();
            int idx = Array.IndexOf(TintCodes, current);
            int nextIdx = (idx + 1) % TintCodes.Length;

            cfg.Tint = TintCodes[nextIdx];
            UpdateTintButtonVisual(strip.TintButton, cfg.Tint);
            _overlayManager.RefreshOverlay(screenId);
            _settingsService.Save(_settings);
        }

        private Border BuildOptionsView()
        {
            var container = new Border
            {
                Background = new SolidColorBrush(MediaColor.FromRgb(35, 43, 53)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(22, 27, 34)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8, 10, 8),
                Visibility = Visibility.Collapsed
            };

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            stack.Children.Add(CreateOptionCheckBox(
                "Show screen identity (1, 2...)",
                _settings.ShowIds,
                val => { _settings.ShowIds = val; _overlayManager.RefreshAllOverlays(); _settingsService.Save(_settings); }));

            stack.Children.Add(CreateOptionCheckBox(
                "Show screen boundaries",
                _settings.ShowBoundaries,
                val => { _settings.ShowBoundaries = val; _overlayManager.RefreshAllOverlays(); _settingsService.Save(_settings); }));

            stack.Children.Add(CreateOptionCheckBox(
                "Proportional Master slider",
                _settings.Master.Proportional,
                val => { _settings.Master.Proportional = val; _settingsService.Save(_settings); }));

            stack.Children.Add(CreateOptionCheckBox(
                "Dim Start Menu & Cursor (Gamma)",
                _settings.UseHardwareGamma,
                val => { _settings.UseHardwareGamma = val; _overlayManager.RefreshAllOverlays(); _settingsService.Save(_settings); }));

            stack.Children.Add(CreateOptionCheckBox(
                "Fix Auto-Hide Taskbar edge",
                _settings.AvoidTaskbarAutoHideLock,
                val => { _settings.AvoidTaskbarAutoHideLock = val; _overlayManager.RefreshAllOverlays(); _settingsService.Save(_settings); }));

            stack.Children.Add(CreateOptionCheckBox(
                "Non-dimmable control panel",
                _settings.NonDimmableWindow,
                val => { _settings.NonDimmableWindow = val; Topmost = val; _overlayManager.EnforceAllTopmost(); _settingsService.Save(_settings); }));

            stack.Children.Add(CreateOptionCheckBox(
                "Movable control window",
                _settings.MovableWindow,
                val => { _settings.MovableWindow = val; _settingsService.Save(_settings); }));

            stack.Children.Add(CreateOptionCheckBox(
                "Close button (✕) hides to tray",
                _settings.CloseHidesToTray,
                val => { _settings.CloseHidesToTray = val; _settingsService.Save(_settings); }));

            stack.Children.Add(CreateOptionCheckBox(
                "Show in taskbar",
                _settings.ShowInTaskbar,
                val => { _settings.ShowInTaskbar = val; ShowInTaskbar = val; _settingsService.Save(_settings); }));

            stack.Children.Add(CreateOptionCheckBox(
                "Start minimised to tray",
                _settings.StartMinimised,
                val => { _settings.StartMinimised = val; _settingsService.Save(_settings); }));

            stack.Children.Add(CreateOptionCheckBox(
                "Start with Windows",
                _settings.AutoRun,
                val => { _settings.AutoRun = val; AutoRunService.SetAutoRun(val); _settingsService.Save(_settings); }));

            var btnResetPos = new Button
            {
                Content = "Reset Window Position",
                Height = 24,
                Margin = new Thickness(0, 6, 0, 2),
                Background = new SolidColorBrush(MediaColor.FromRgb(50, 60, 74)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnResetPos.Click += (s, e) => ResetPositionToCorner();
            stack.Children.Add(btnResetPos);

            scroll.Content = stack;
            container.Child = scroll;
            return container;
        }

        private CheckBox CreateOptionCheckBox(string label, bool initialValue, Action<bool> onChanged)
        {
            var chk = new CheckBox
            {
                Content = label,
                IsChecked = initialValue,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(220, 228, 236)),
                Margin = new Thickness(0, 3, 0, 3),
                FontSize = 11.5
            };
            chk.Click += (s, e) => onChanged(chk.IsChecked == true);
            return chk;
        }

        private Border BuildAboutView()
        {
            var container = new Border
            {
                Background = new SolidColorBrush(MediaColor.FromRgb(35, 43, 53)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(22, 27, 34)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 10, 12, 10),
                Visibility = Visibility.Collapsed
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            stack.Children.Add(new TextBlock
            {
                Text = "OpenDimmer v1.0.0",
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 6)
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Open-source multi-monitor brightness & warm-tint controller with persistent Z-Order guardian, sleep/dock recovery, and hybrid hardware gamma.",
                Foreground = new SolidColorBrush(MediaColor.FromRgb(190, 202, 215)),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11.5,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var btnRepo = new Button
            {
                Content = "GitHub: TrueTrailBlazer/OpenDimmer",
                Height = 26,
                Background = new SolidColorBrush(MediaColor.FromRgb(58, 122, 254)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnRepo.Click += (s, e) =>
            {
                try { Process.Start("https://github.com/TrueTrailBlazer/OpenDimmer"); } catch { }
            };
            stack.Children.Add(btnRepo);

            container.Child = stack;
            return container;
        }

        private Border BuildDebugView()
        {
            var container = new Border
            {
                Background = new SolidColorBrush(MediaColor.FromRgb(35, 43, 53)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(22, 27, 34)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Visibility = Visibility.Collapsed
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });

            _txtDebug = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(MediaColor.FromRgb(22, 27, 34)),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(195, 208, 222)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(45, 54, 66)),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 10.5,
                Padding = new Thickness(4)
            };

            var btnCopyDebug = new Button
            {
                Content = "Copy Debug Log",
                Height = 22,
                Margin = new Thickness(0, 5, 0, 0),
                Background = new SolidColorBrush(MediaColor.FromRgb(50, 60, 74)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnCopyDebug.Click += (s, e) =>
            {
                try
                {
                    PopulateDebugLog();
                    Clipboard.SetText(_txtDebug.Text ?? string.Empty);
                    btnCopyDebug.Content = "Copied to Clipboard!";
                }
                catch { }
            };

            Grid.SetRow(_txtDebug, 0);
            Grid.SetRow(btnCopyDebug, 1);
            grid.Children.Add(_txtDebug);
            grid.Children.Add(btnCopyDebug);

            container.Child = grid;
            return container;
        }

        private void PopulateDebugLog()
        {
            if (_txtDebug == null) return;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== OpenDimmer Diagnostic Log ===");
                sb.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine(string.Format("OS: [{0}]", Environment.OSVersion));
                sb.AppendLine(string.Format("64-bit: [OS: {0}] [Process: {1}]",
                    Environment.Is64BitOperatingSystem, Environment.Is64BitProcess));
                sb.AppendLine();

                var screens = Forms.Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    var s = screens[i];
                    sb.AppendLine(string.Format("DVC #{0}: [{1}] [BPP: {2}] [Primary: {3}]",
                        i + 1, s.DeviceName, s.BitsPerPixel, s.Primary));
                    sb.AppendLine(string.Format("BOU #{0}: {1}", i + 1, s.Bounds));
                }

                sb.AppendLine();
                sb.AppendLine("=== OpenDimmer.json ===");
                if (File.Exists(_settingsService.FilePath))
                {
                    sb.AppendLine(File.ReadAllText(_settingsService.FilePath, Encoding.UTF8));
                }

                _txtDebug.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                _txtDebug.Text = "Debug log error: " + ex.Message;
            }
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

            // Sync Master checkbox state with individual screens (same as original Dimmer)
            _suppressEvents = true;
            bool anyOn = _screenStrips.Any(x => x.CheckBox.IsChecked == true);
            _chkMaster.IsChecked = anyOn;
            _settings.Master.On = anyOn;
            _sldMaster.IsEnabled = anyOn;
            _sldMaster.Opacity = anyOn ? 1.0 : 0.4;
            _txtMaster.IsEnabled = anyOn;
            _suppressEvents = false;

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

        private Icon CreateBrightnessIcon()
        {
            using (var bmp = new Bitmap(16, 16))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(DrawingColor.Transparent);

                using (var brush = new SolidBrush(DrawingColor.FromArgb(255, 245, 180, 45)))
                {
                    g.FillEllipse(brush, 4, 4, 8, 8);
                }

                using (var pen = new DrawingPen(DrawingColor.FromArgb(255, 245, 180, 45), 1.5f))
                {
                    g.DrawLine(pen, 8, 1, 8, 2);
                    g.DrawLine(pen, 8, 14, 8, 15);
                    g.DrawLine(pen, 1, 8, 2, 8);
                    g.DrawLine(pen, 14, 8, 15, 8);
                }

                _trayIconHandle = bmp.GetHicon();
                return System.Drawing.Icon.FromHandle(_trayIconHandle);
            }
        }

        private void BuildSystemTrayIcon()
        {
            var menu = new Forms.ContextMenuStrip
            {
                Renderer = new DarkTrayMenuRenderer(),
                ShowImageMargin = false,
                ShowCheckMargin = false,
                BackColor = DrawingColor.FromArgb(32, 38, 46),
                ForeColor = DrawingColor.FromArgb(215, 224, 234)
            };

            menu.Items.Add("Open", null, (s, e) => ShowAndActivateWindow());
            menu.Items.Add("Toggle Master", null, (s, e) =>
            {
                _chkMaster.IsChecked = !(_chkMaster.IsChecked == true);
                OnMasterToggled();
            });
            menu.Items.Add("Reset window position", null, (s, e) => ResetPositionToCorner());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Close", null, (s, e) => Application.Current.Shutdown());

            _notifyIcon = new Forms.NotifyIcon
            {
                Text = "OpenDimmer",
                Icon = CreateBrightnessIcon(),
                ContextMenuStrip = menu,
                Visible = true
            };

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == Forms.MouseButtons.Left)
                {
                    if (IsVisible && WindowState != WindowState.Minimized)
                    {
                        Hide();
                        NativeMethods.TrimMemoryWorkingSet();
                    }
                    else
                    {
                        ShowAndActivateWindow();
                    }
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
            SaveWindowPosition();
        }

        private void OnBtnMinimiseClick(object sender, RoutedEventArgs e)
        {
            if (_settings.ShowInTaskbar)
            {
                WindowState = WindowState.Minimized;
            }
            else
            {
                Hide();
                NativeMethods.TrimMemoryWorkingSet();
            }
        }

        private void OnBtnCloseClick(object sender, RoutedEventArgs e)
        {
            SaveWindowPosition();
            if (_settings.CloseHidesToTray)
            {
                Hide();
                NativeMethods.TrimMemoryWorkingSet();
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
            if (_trayIconHandle != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(_trayIconHandle);
                _trayIconHandle = IntPtr.Zero;
            }
            _overlayManager.Dispose();
            base.OnClosed(e);
        }
    }
}
