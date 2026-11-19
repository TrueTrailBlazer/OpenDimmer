using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OpenDimmer.Linux.Services;
using OpenDimmer.Models;
using OpenDimmer.Services;
using AvPath = Avalonia.Controls.Shapes.Path;

namespace OpenDimmer.Linux.UI
{
    public sealed class MainWindow : Window
    {
        private static readonly Color ColCanvas = Color.Parse("#181B20");
        private static readonly Color ColSurfaceCard = Color.Parse("#22272E");
        private static readonly Color ColSurfaceSubCard = Color.Parse("#16191E");
        private static readonly Color ColSurfaceMaster = Color.Parse("#1F2937");
        private static readonly Color ColSurfaceInput = Color.Parse("#14171C");
        private static readonly Color ColSurfaceTrack = Color.Parse("#111418");
        private static readonly Color ColSurfaceHover = Color.Parse("#2D333B");
        private static readonly Color ColPrimary = Color.Parse("#4D8EFF");
        private static readonly Color ColPrimaryStrong = Color.Parse("#3B82F6");
        private static readonly Color ColSecondary = Color.Parse("#FFB95F");
        private static readonly Color ColTextPrimary = Color.Parse("#F3F4F6");
        private static readonly Color ColTextSecondary = Color.Parse("#9CA3AF");
        private static readonly Color ColTextMuted = Color.Parse("#6B7280");
        private static readonly Color ColBorderSubtle = Color.Parse("#2D333B");
        private static readonly Color ColDanger = Color.Parse("#E11D48");
        private static readonly Color ColSwitchOff = Color.Parse("#36393F");

        private const string SvgBrightnessMedium =
            "M12 2L14.8 4.8H19.2V9.2L22 12L19.2 14.8V19.2H14.8L12 22L9.2 19.2H4.8V14.8L2 12L4.8 9.2V4.8H9.2L12 2ZM12 6.5A5.5 5.5 0 1 0 12 17.5A5.5 5.5 0 1 0 12 6.5ZM12 8.2V15.8A3.8 3.8 0 0 1 12 8.2Z";
        private const string SvgLink =
            "M3.9 12c0-1.71 1.39-3.1 3.1-3.1h4V7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h4v-1.9H7c-1.71 0-3.1-1.39-3.1-3.1zM8 13h8v-2H8v2zm9-6h-4v1.9h4c1.71 0 3.1 1.39 3.1 3.1s-1.39 3.1-3.1 3.1h-4V17h4c2.76 0 5-2.24 5-5s-2.24-5-5-5z";
        private const string SvgNightlight =
            "M12 3a9 9 0 1 0 9 9c0-.46-.04-.92-.1-1.36a5.389 5.389 0 0 1-4.4 2.26 5.403 5.403 0 0 1-3.14-9.8c-.44-.06-.9-.1-1.36-.1z";
        private const string SvgMonitor =
            "M21 2H3c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h7v2H8v2h8v-2h-2v-2h7c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H3V4h18v12z";
        private const string SvgLightMode =
            "M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zM2 13h2c.55 0 1-.45 1-1s-.45-1-1-1H2c-.55 0-1 .45-1 1s.45 1 1 1zm18 0h2c.55 0 1-.45 1-1s-.45-1-1-1h-2c-.55 0-1 .45-1 1s.45 1 1 1zM11 2v2c0 .55.45 1 1 1s1-.45 1-1V2c0-.55-.45-1-1-1s-1 .45-1 1zm0 18v2c0 .55.45 1 1 1s1-.45 1-1v-2c0-.55-.45-1-1-1s-1 .45-1 1zM5.99 4.58a.996.996 0 0 0-1.41 0 .996.996 0 0 0 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0s.39-1.03 0-1.41L5.99 4.58zm12.37 12.37a.996.996 0 0 0-1.41 0 .996.996 0 0 0 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0a.996.996 0 0 0 0-1.41l-1.06-1.06zm1.06-10.96a.996.996 0 0 0 0-1.41.996.996 0 0 0-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06zM7.05 18.36a.996.996 0 0 0 0-1.41.996.996 0 0 0-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06z";
        private const string SvgWbIncandescent =
            "M3.55 19.09l1.41 1.41 1.79-1.8-1.41-1.41-1.79 1.8zM11 23h2v-2.95h-2V23zM4 11.05H1v2h3v-2zm11-4.19V2.05H9v4.81C7.21 7.9 6 9.83 6 12.05c0 3.31 2.69 6 6 6s6-2.69 6-6c0-2.22-1.21-4.15-3-5.19zm5 4.19v2h3v-2h-3zm-2.76 7.66l1.79 1.8 1.41-1.41-1.8-1.79-1.4 1.4z";
        private const string SvgSliders =
            "M3 17v2h6v-2H3zM3 5v2h10V5H3zm10 16v-2h8v-2h-8v-2h-2v6h2zM7 9v2H3v2h4v2h2V9H7zm14 4v-2H11v2h10zm-6-4h2V7h4V5h-4V3h-2v6z";
        private const string SvgChevronDown =
            "M7 10l5 5 5-5z";

        private sealed class ChannelStripVisual
        {
            public bool IsMaster;
            public int ScreenId;
            public string DeviceName = string.Empty;
            public bool IsOn;
            public int Value;
            public Border CardBorder = null!;
            public Border BadgeBorder = null!;
            public TextBlock BadgeText = null!;
            public TextBlock StatusLine = null!;
            public Border SliderTrackArea = null!;
            public Border SliderTrackBg = null!;
            public Border SliderFill = null!;
            public Border SliderThumb = null!;
            public Border ThumbInnerDot = null!;
            public TextBox ValueBox = null!;
            public Ellipse StatusDot = null!;
            public TextBlock BottomLabel = null!;
            public Border StatusPill = null!;
            public Button HeaderActionBtn = null!;
        }

        private readonly AppSettings _settings;
        private readonly SettingsStore _settingsStore;
        private readonly X11DisplayService _displayService;
        private readonly Action _shutdownCallback;
        private readonly bool _applyGamma;

        private readonly List<ChannelStripVisual> _monitorStrips = new List<ChannelStripVisual>();
        private readonly List<Button> _presetButtons = new List<Button>();

        private ChannelStripVisual? _masterStrip;
        private Button _navScreensBtn = null!;
        private Button _navSettingsBtn = null!;
        private Grid _screensView = null!;
        private ScrollViewer _settingsView = null!;
        private UniformGrid _channelsGrid = null!;
        private StackPanel _tintSettingsHost = null!;
        private TextBlock _configuredCountText = null!;
        private Ellipse _nightSyncDot = null!;
        private TextBlock _footerStatusText = null!;
        private Grid _dropdownOverlayLayer = null!;
        private Border _dropdownMenuCard = null!;

        private bool _suppressEvents;
        private bool _explicitExit;

        public MainWindow(
            AppSettings settings,
            SettingsStore settingsStore,
            X11DisplayService displayService,
            Action shutdownCallback,
            bool applyGamma = true)
        {
            _settings = settings;
            _settingsStore = settingsStore;
            _displayService = displayService;
            _shutdownCallback = shutdownCallback;
            _applyGamma = applyGamma;

            Title = "OpenDimmer";
            Width = 392;
            Height = 478;
            MinWidth = 370;
            MinHeight = 420;
            Background = Brushes.Transparent;
            SystemDecorations = SystemDecorations.BorderOnly;
            CanResize = true;
            ShowInTaskbar = _settings.ShowInTaskbar;
            WindowStartupLocation = WindowStartupLocation.Manual;

            LoadWindowIcon();
            BuildUserInterface();
            RebuildChannelStrips();
            RebuildTintSettingsRows();
            UpdateConfiguredCounter();
            RestoreOrResetPosition();

            if (_applyGamma)
            {
                _displayService.ApplyAll(_settings);
            }

            if (_settings.StartMinimised && _applyGamma)
            {
                Hide();
            }

            Closing += OnWindowClosing;
        }

        private void LoadWindowIcon()
        {
            try
            {
                using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("OpenDimmer.icon.png");
                if (stream != null)
                {
                    Icon = new WindowIcon(stream);
                }
            }
            catch
            {
                // Ignore icon loading fallback
            }
        }

        private void BuildUserInterface()
        {
            var outerShell = new Border
            {
                Background = new SolidColorBrush(ColCanvas),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(ColBorderSubtle),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };

            var rootOverlayHost = new Grid();
            var mainLayout = new Grid { Margin = new Thickness(10, 0, 10, 0) };
            mainLayout.RowDefinitions.Add(new RowDefinition(42, GridUnitType.Pixel));
            mainLayout.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
            mainLayout.RowDefinitions.Add(new RowDefinition(28, GridUnitType.Pixel));

            var header = BuildHeaderBar();
            Grid.SetRow(header, 0);
            mainLayout.Children.Add(header);

            var contentHost = new Grid();
            _screensView = BuildScreensView();
            _settingsView = BuildSettingsView();
            contentHost.Children.Add(_screensView);
            contentHost.Children.Add(_settingsView);
            Grid.SetRow(contentHost, 1);
            mainLayout.Children.Add(contentHost);

            var footer = BuildFooterBar();
            Grid.SetRow(footer, 2);
            mainLayout.Children.Add(footer);

            rootOverlayHost.Children.Add(mainLayout);

            _dropdownOverlayLayer = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(95, 0, 0, 0)),
                IsVisible = false,
                ZIndex = 100
            };
            _dropdownOverlayLayer.PointerPressed += (s, e) =>
            {
                if (Equals(e.Source, _dropdownOverlayLayer))
                {
                    CloseTintPresetDropdown();
                }
            };

            _dropdownMenuCard = new Border
            {
                Width = 228,
                Background = new SolidColorBrush(Color.Parse("#1A1D23")),
                BorderBrush = new SolidColorBrush(ColBorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 8, 10, 8),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 118, 22, 0),
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    OffsetX = 0,
                    OffsetY = 8,
                    Blur = 22,
                    Color = Color.FromArgb(180, 0, 0, 0)
                })
            };
            _dropdownOverlayLayer.Children.Add(_dropdownMenuCard);
            rootOverlayHost.Children.Add(_dropdownOverlayLayer);

            outerShell.Child = rootOverlayHost;
            Content = outerShell;
        }

        private Grid BuildHeaderBar()
        {
            var header = new Grid { Background = new SolidColorBrush(ColCanvas) };
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            header.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            header.PointerPressed += (s, e) =>
            {
                if (_settings.MovableWindow
                    && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
                    && !HasAncestorButton(e.Source as Visual))
                {
                    BeginMoveDrag(e);
                }
            };

            var leftBrand = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            var brandIcon = CreateHeaderBrandIcon();
            brandIcon.Margin = new Thickness(0, 0, 6, 0);
            leftBrand.Children.Add(brandIcon);
            leftBrand.Children.Add(new TextBlock
            {
                Text = "OpenDimmer",
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetColumn(leftBrand, 0);
            header.Children.Add(leftBrand);

            var navPill = new Border
            {
                Background = new SolidColorBrush(ColSurfaceInput),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var navStack = new StackPanel { Orientation = Orientation.Horizontal };
            _navScreensBtn = CreateNavPillButton("Screens", true);
            _navScreensBtn.Click += (s, e) => SwitchView("Screens");
            _navSettingsBtn = CreateNavPillButton("Settings", false);
            _navSettingsBtn.Margin = new Thickness(2, 0, 0, 0);
            _navSettingsBtn.Click += (s, e) => SwitchView("Settings");
            navStack.Children.Add(_navScreensBtn);
            navStack.Children.Add(_navSettingsBtn);
            navPill.Child = navStack;
            Grid.SetColumn(navPill, 1);
            header.Children.Add(navPill);

            var winControls = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            var btnMin = CreateWindowControlButton("\u2014", "Minimize", false);
            btnMin.Click += OnBtnMinimiseClick;
            var btnMax = CreateWindowControlButton("\u25A2", "Maximize / Restore", false);
            btnMax.Click += (s, e) => ToggleMaximizeRestore();
            var btnClose = CreateWindowControlButton("\u2715", "Close / Hide to tray", true);
            btnClose.Click += OnBtnCloseClick;

            winControls.Children.Add(btnMin);
            winControls.Children.Add(btnMax);
            winControls.Children.Add(btnClose);
            Grid.SetColumn(winControls, 2);
            header.Children.Add(winControls);

            return header;
        }

        private static bool HasAncestorButton(Visual? visual)
        {
            Visual? current = visual;
            while (current != null)
            {
                if (current is Button) return true;
                current = current.GetVisualParent();
            }
            return false;
        }

        private static Control CreateHeaderBrandIcon()
        {
            try
            {
                using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("OpenDimmer.icon.png");
                if (stream != null)
                {
                    var bmp = new Bitmap(stream);
                    return new Border
                    {
                        Width = 18,
                        Height = 18,
                        CornerRadius = new CornerRadius(4),
                        ClipToBounds = true,
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new Image
                        {
                            Source = bmp,
                            Width = 18,
                            Height = 18
                        }
                    };
                }
            }
            catch
            {
                // Fallback to vector icon
            }

            return CreateVectorIcon(SvgBrightnessMedium, ColPrimary, 16, 16);
        }

        private static Control CreateVectorIcon(string pathData, Color color, double width, double height)
        {
            var path = new AvPath
            {
                Data = Geometry.Parse(pathData),
                Fill = new SolidColorBrush(color),
                Stretch = Stretch.Uniform
            };

            return new Viewbox
            {
                Width = width,
                Height = height,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = path
            };
        }

        private static Button CreateNavPillButton(string label, bool active)
        {
            var btn = new Button
            {
                Content = label,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Padding = new Thickness(10, 4, 10, 4),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(5),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            StyleNavPillButton(btn, active);
            return btn;
        }

        private static void StyleNavPillButton(Button btn, bool active)
        {
            btn.Background = active ? new SolidColorBrush(ColSurfaceCard) : Brushes.Transparent;
            btn.Foreground = new SolidColorBrush(active ? ColTextPrimary : ColTextSecondary);
        }

        private static Button CreateWindowControlButton(string symbol, string tooltip, bool isClose)
        {
            var btn = new Button
            {
                Content = symbol,
                Width = 24,
                Height = 24,
                FontSize = 11,
                Padding = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(ColTextSecondary),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(5),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            ToolTip.SetTip(btn, tooltip);
            btn.PointerEntered += (s, e) =>
            {
                btn.Background = new SolidColorBrush(isClose ? ColDanger : ColSurfaceHover);
                btn.Foreground = new SolidColorBrush(ColTextPrimary);
            };
            btn.PointerExited += (s, e) =>
            {
                btn.Background = Brushes.Transparent;
                btn.Foreground = new SolidColorBrush(ColTextSecondary);
            };
            return btn;
        }

        private Grid BuildScreensView()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            _channelsGrid = new UniformGrid
            {
                Rows = 1,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(_channelsGrid, 0);
            grid.Children.Add(_channelsGrid);

            var bottomBar = new Border
            {
                Background = new SolidColorBrush(ColSurfaceCard),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6)
            };

            var bottomDock = new DockPanel { LastChildFill = false };

            var nightBtn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(2, 2, 6, 2),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            ToolTip.SetTip(nightBtn, "Toggle warm Night Light (4500K) across all monitors");

            var nightStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            var dotHolder = new Border
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(ColSurfaceInput),
                Margin = new Thickness(0, 0, 6, 0)
            };
            _nightSyncDot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(ColSecondary),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            dotHolder.Child = _nightSyncDot;
            nightStack.Children.Add(dotHolder);
            nightStack.Children.Add(new TextBlock
            {
                Text = "Night Sync",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(ColTextSecondary),
                VerticalAlignment = VerticalAlignment.Center
            });
            nightBtn.Content = nightStack;
            nightBtn.Click += (s, e) => OnNightSyncClicked();
            DockPanel.SetDock(nightBtn, Dock.Left);
            bottomDock.Children.Add(nightBtn);

            var presetsPill = new Border
            {
                Background = new SolidColorBrush(ColSurfaceInput),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(2)
            };
            var presetsStack = new StackPanel { Orientation = Orientation.Horizontal };
            _presetButtons.Clear();

            int[] presets = new[] { 25, 50, 75, 0 };
            foreach (int p in presets)
            {
                int captured = p;
                string label = p == 0 ? "Off" : (p + "%");
                bool isActive = (p == 0 && !_settings.Master.On) || (p > 0 && _settings.Master.On && _settings.Master.Dim == p);
                var pBtn = CreateNavPillButton(label, isActive);
                pBtn.FontSize = 10;
                pBtn.Padding = new Thickness(8, 3, 8, 3);
                ToolTip.SetTip(pBtn, p == 0 ? "Disable dimming (100% brightness)" : ("Set dimming to " + p + "%"));
                pBtn.Click += (s, e) => ApplyQuickPreset(captured);
                _presetButtons.Add(pBtn);
                presetsStack.Children.Add(pBtn);
            }
            presetsPill.Child = presetsStack;
            DockPanel.SetDock(presetsPill, Dock.Right);
            bottomDock.Children.Add(presetsPill);

            bottomBar.Child = bottomDock;
            Grid.SetRow(bottomBar, 1);
            grid.Children.Add(bottomBar);
            return grid;
        }

        private void RebuildChannelStrips()
        {
            _suppressEvents = true;
            _channelsGrid.Children.Clear();
            _monitorStrips.Clear();

            var monitors = _displayService.Monitors;
            _channelsGrid.Columns = monitors.Count + 1;

            _masterStrip = CreateChannelStrip(
                true,
                0,
                string.Empty,
                "Master",
                _settings.Master.On ? "Linked All" : "Unlinked",
                _settings.Master.On,
                _settings.Master.Dim,
                "000000");
            _channelsGrid.Children.Add(_masterStrip.CardBorder);

            for (int i = 0; i < monitors.Count; i++)
            {
                MonitorDescriptor desc = monitors[i].Descriptor;
                ScreenSettings? cfg = _settings.Screens.FirstOrDefault(x =>
                    string.Equals(x.DeviceName, desc.DeviceName, StringComparison.OrdinalIgnoreCase))
                    ?? _settings.Screens.FirstOrDefault(x => x.Id == desc.Index);
                if (cfg == null) continue;

                ChannelStripVisual strip = CreateChannelStrip(
                    false,
                    desc.Index,
                    desc.DeviceName,
                    desc.FriendlyName,
                    desc.Subtitle,
                    cfg.On,
                    cfg.Dim,
                    cfg.Tint);

                _monitorStrips.Add(strip);
                _channelsGrid.Children.Add(strip.CardBorder);
            }

            UpdateNightSyncDotVisual();
            _suppressEvents = false;
        }

        private ChannelStripVisual CreateChannelStrip(
            bool isMaster,
            int screenId,
            string deviceName,
            string title,
            string subtitle,
            bool isOn,
            int dimValue,
            string tintHex)
        {
            int maxCap = _settings.MaxDimPercent > 0 ? _settings.MaxDimPercent : 90;
            var visual = new ChannelStripVisual
            {
                IsMaster = isMaster,
                ScreenId = screenId,
                DeviceName = deviceName,
                IsOn = isOn,
                Value = DimmingMath.ClampDimPercent(dimValue, maxCap)
            };

            var card = new Border
            {
                Background = new SolidColorBrush(isMaster ? ColSurfaceMaster : ColSurfaceCard),
                BorderBrush = isMaster ? new SolidColorBrush(Color.FromArgb(90, 59, 130, 246)) : Brushes.Transparent,
                BorderThickness = new Thickness(isMaster ? 1 : 0),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(3, 0, 3, 0),
                ClipToBounds = true
            };

            var cardLayout = new Grid();
            cardLayout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            cardLayout.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
            cardLayout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var headerStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
            var topRow = new DockPanel
            {
                LastChildFill = false,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var badgeBtn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            ToolTip.SetTip(badgeBtn, isMaster ? "Toggle all monitors On/Off" : "Toggle monitor On/Off");

            var badgeBorder = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(isMaster ? ColPrimary : ColSurfaceInput)
            };
            var badgeText = new TextBlock
            {
                Text = isMaster ? "M" : screenId.ToString(),
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(isMaster ? ColCanvas : ColTextSecondary),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            badgeBorder.Child = badgeText;
            badgeBtn.Content = badgeBorder;
            badgeBtn.Click += (s, e) => ToggleChannelPower(visual);
            DockPanel.SetDock(badgeBtn, Dock.Left);
            topRow.Children.Add(badgeBtn);

            var headerActionBtn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(2),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            headerActionBtn.Click += (s, e) =>
            {
                if (visual.IsMaster)
                {
                    ToggleChannelPower(visual);
                }
                else
                {
                    CycleMonitorTint(visual.ScreenId);
                }
            };
            DockPanel.SetDock(headerActionBtn, Dock.Right);
            topRow.Children.Add(headerActionBtn);
            headerStack.Children.Add(topRow);

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 11.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            ToolTip.SetTip(titleText, title);
            headerStack.Children.Add(titleText);

            var statusLine = new TextBlock
            {
                Text = subtitle,
                FontSize = 9.5,
                Foreground = new SolidColorBrush(isMaster ? ColPrimary : ColTextSecondary),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };
            headerStack.Children.Add(statusLine);
            Grid.SetRow(headerStack, 0);
            cardLayout.Children.Add(headerStack);

            // Center vertical slider area + left scale ticks
            var sliderHost = new Grid
            {
                Margin = new Thickness(0, 8, 0, 8),
                MinHeight = 140
            };

            var ticksStack = new Grid
            {
                Width = 22,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(2, 4, 0, 4),
                IsHitTestVisible = false
            };
            for (int r = 0; r < 5; r++)
            {
                ticksStack.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
            }
            string[] ticks = isMaster
                ? new[] { "100", "75", "50", "25", "0" }
                : new[] { "-", "-", "-", "-", "-" };
            for (int t = 0; t < ticks.Length; t++)
            {
                var tb = new TextBlock
                {
                    Text = ticks[t],
                    FontSize = 9,
                    Foreground = new SolidColorBrush(isMaster ? ColTextMuted : ColBorderSubtle),
                    VerticalAlignment = t == 0 ? VerticalAlignment.Top : (t == 4 ? VerticalAlignment.Bottom : VerticalAlignment.Center),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                Grid.SetRow(tb, t);
                ticksStack.Children.Add(tb);
            }
            sliderHost.Children.Add(ticksStack);

            var trackHitBox = new Border
            {
                Width = 34,
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            var trackCanvas = new Grid
            {
                Margin = new Thickness(0, 8, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 18
            };

            var trackBg = new Border
            {
                Width = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(ColSurfaceTrack),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipToBounds = true
            };

            var fill = new Border
            {
                Width = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(ColPrimary),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            trackBg.Child = fill;
            trackCanvas.Children.Add(trackBg);

            var thumb = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(ColTextPrimary),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    OffsetX = 0,
                    OffsetY = 1,
                    Blur = 4,
                    Color = Color.FromArgb(170, 0, 0, 0)
                })
            };
            var thumbDot = new Border
            {
                Width = 6,
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(isMaster ? ColSurfaceMaster : ColSurfaceCard),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            thumb.Child = thumbDot;
            trackCanvas.Children.Add(thumb);

            trackHitBox.Child = trackCanvas;
            sliderHost.Children.Add(trackHitBox);
            Grid.SetRow(sliderHost, 1);
            cardLayout.Children.Add(sliderHost);

            var footerStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var valPill = new Border
            {
                Width = 46,
                Height = 24,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(ColSurfaceInput),
                Margin = new Thickness(0, 0, 0, 6)
            };
            var valBox = new TextBox
            {
                Text = visual.Value + "%",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(ColTextPrimary),
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(2, 0, 2, 0),
                MinHeight = 20
            };
            valPill.Child = valBox;
            footerStack.Children.Add(valPill);

            var statusPillBtn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Cursor = isMaster ? Cursor.Default : new Cursor(StandardCursorType.Hand)
            };
            var statusPill = new Border
            {
                Background = isMaster ? Brushes.Transparent : new SolidColorBrush(ColSurfaceInput),
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(7, 2, 7, 2)
            };
            var statusPillRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            var statusDot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(ColPrimary),
                Margin = new Thickness(0, 0, 4, 0),
                IsVisible = !isMaster
            };
            if (isMaster)
            {
                var sun = CreateVectorIcon(SvgLightMode, ColPrimary, 12, 12);
                sun.Margin = new Thickness(0, 0, 4, 0);
                statusPillRow.Children.Add(sun);
            }
            else
            {
                statusPillRow.Children.Add(statusDot);
            }

            var bottomLabel = new TextBlock
            {
                Text = isMaster ? "ALL" : "Native",
                FontSize = 9.5,
                FontWeight = FontWeight.Medium,
                Foreground = new SolidColorBrush(ColTextSecondary),
                VerticalAlignment = VerticalAlignment.Center
            };
            statusPillRow.Children.Add(bottomLabel);
            statusPill.Child = statusPillRow;
            statusPillBtn.Content = statusPill;
            if (!isMaster)
            {
                ToolTip.SetTip(statusPillBtn, "Click to cycle color temperature preset");
                statusPillBtn.Click += (s, e) => CycleMonitorTint(visual.ScreenId);
            }
            footerStack.Children.Add(statusPillBtn);

            Grid.SetRow(footerStack, 2);
            cardLayout.Children.Add(footerStack);

            card.Child = cardLayout;

            visual.CardBorder = card;
            visual.BadgeBorder = badgeBorder;
            visual.BadgeText = badgeText;
            visual.StatusLine = statusLine;
            visual.SliderTrackArea = trackHitBox;
            visual.SliderTrackBg = trackBg;
            visual.SliderFill = fill;
            visual.SliderThumb = thumb;
            visual.ThumbInnerDot = thumbDot;
            visual.ValueBox = valBox;
            visual.StatusDot = statusDot;
            visual.BottomLabel = bottomLabel;
            visual.StatusPill = statusPill;
            visual.HeaderActionBtn = headerActionBtn;

            BindCustomSliderEvents(visual);
            BindValueInputEvents(visual);
            UpdateChannelVisualState(visual, tintHex);

            return visual;
        }

        private void BindCustomSliderEvents(ChannelStripVisual visual)
        {
            bool isDragging = false;

            void ApplyPointerPosition(PointerEventArgs e)
            {
                double h = visual.SliderTrackBg.Bounds.Height;
                if (h <= 1) return;
                Point pt = e.GetPosition(visual.SliderTrackBg);
                double ratio = 1.0 - (pt.Y / h);
                int maxCap = _settings.MaxDimPercent > 0 ? _settings.MaxDimPercent : 90;
                int newVal = DimmingMath.ClampDimPercent((int)Math.Round(ratio * maxCap), maxCap);
                SetChannelValue(visual, newVal, true);
            }

            visual.SliderTrackArea.PointerPressed += (s, e) =>
            {
                if (!e.GetCurrentPoint(visual.SliderTrackArea).Properties.IsLeftButtonPressed) return;
                isDragging = true;
                e.Pointer.Capture(visual.SliderTrackArea);
                ApplyPointerPosition(e);
                e.Handled = true;
            };

            visual.SliderTrackArea.PointerMoved += (s, e) =>
            {
                if (!isDragging) return;
                ApplyPointerPosition(e);
            };

            visual.SliderTrackArea.PointerReleased += (s, e) =>
            {
                if (!isDragging) return;
                isDragging = false;
                if (Equals(e.Pointer.Captured, visual.SliderTrackArea))
                {
                    e.Pointer.Capture(null);
                }
            };

            visual.SliderTrackArea.PointerCaptureLost += (s, e) => isDragging = false;
            visual.SliderTrackBg.SizeChanged += (s, e) => UpdateSliderGeometry(visual);

            visual.CardBorder.PointerWheelChanged += (s, e) =>
            {
                int delta = e.Delta.Y > 0 ? 5 : -5;
                int maxCap = _settings.MaxDimPercent > 0 ? _settings.MaxDimPercent : 90;
                int nextVal = DimmingMath.ClampDimPercent(visual.Value + delta, maxCap);
                if (nextVal != visual.Value)
                {
                    SetChannelValue(visual, nextVal, true);
                }
                e.Handled = true;
            };
        }

        private void BindValueInputEvents(ChannelStripVisual visual)
        {
            void CommitBoxText()
            {
                if (_suppressEvents) return;
                string raw = (visual.ValueBox.Text ?? string.Empty).Replace("%", "").Trim();
                if (int.TryParse(raw, out int parsed))
                {
                    int maxCap = _settings.MaxDimPercent > 0 ? _settings.MaxDimPercent : 90;
                    SetChannelValue(visual, DimmingMath.ClampDimPercent(parsed, maxCap), true);
                }
                else
                {
                    visual.ValueBox.Text = visual.Value + "%";
                }
            }

            visual.ValueBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    CommitBoxText();
                    e.Handled = true;
                }
            };
            visual.ValueBox.LostFocus += (s, e) => CommitBoxText();
        }

        private void UpdateSliderGeometry(ChannelStripVisual visual)
        {
            double h = visual.SliderTrackBg.Bounds.Height;
            if (h <= 0) return;

            int maxCap = _settings.MaxDimPercent > 0 ? _settings.MaxDimPercent : 90;
            double ratio = Math.Max(0.0, Math.Min(1.0, visual.Value / (double)maxCap));
            double fillH = h * ratio;

            visual.SliderFill.Height = fillH;
            double thumbBottom = Math.Max(0.0, Math.Min(Math.Max(0.0, h - 16.0), fillH - 8.0));
            visual.SliderThumb.Margin = new Thickness(0, 0, 0, thumbBottom);
        }

        private void UpdateChannelVisualState(ChannelStripVisual visual, string tintHex)
        {
            _suppressEvents = true;
            visual.ValueBox.Text = visual.Value + "%";
            _suppressEvents = false;

            TintPreset preset = DimmingMath.FindTintPreset(tintHex);
            bool isWarm = preset.HexCode != "000000";

            if (visual.IsMaster)
            {
                visual.BadgeBorder.Background = new SolidColorBrush(visual.IsOn ? ColPrimary : ColSurfaceInput);
                visual.BadgeText.Foreground = new SolidColorBrush(visual.IsOn ? ColCanvas : ColTextSecondary);
                visual.StatusLine.Text = visual.IsOn ? "Linked All" : "Unlinked";
                visual.StatusLine.Foreground = new SolidColorBrush(visual.IsOn ? ColPrimary : ColTextMuted);
                visual.HeaderActionBtn.Content = CreateVectorIcon(SvgLink, visual.IsOn ? ColPrimary : ColTextMuted, 14, 14);
                visual.SliderFill.Background = new SolidColorBrush(visual.IsOn ? ColPrimary : ColSurfaceHover);
            }
            else
            {
                visual.BadgeBorder.Background = new SolidColorBrush(visual.IsOn ? ColPrimaryStrong : ColSurfaceInput);
                visual.BadgeText.Foreground = new SolidColorBrush(visual.IsOn ? ColTextPrimary : ColTextMuted);

                if (isWarm)
                {
                    visual.HeaderActionBtn.Content = CreateVectorIcon(SvgNightlight, ColSecondary, 13, 13);
                    visual.StatusDot.Fill = new SolidColorBrush(ColSecondary);
                    visual.BottomLabel.Text = preset.ShortBadge;
                    visual.BottomLabel.Foreground = new SolidColorBrush(ColSecondary);
                }
                else
                {
                    visual.HeaderActionBtn.Content = CreateVectorIcon(SvgMonitor, ColTextSecondary, 13, 13);
                    visual.StatusDot.Fill = new SolidColorBrush(ColPrimary);
                    visual.BottomLabel.Text = "Native";
                    visual.BottomLabel.Foreground = new SolidColorBrush(ColTextSecondary);
                }

                visual.SliderFill.Background = new SolidColorBrush(
                    visual.IsOn ? (isWarm ? ColSecondary : ColPrimary) : ColSurfaceHover);
            }

            visual.CardBorder.Opacity = visual.IsOn ? 1.0 : 0.72;
            UpdateSliderGeometry(visual);
        }

        private void ToggleChannelPower(ChannelStripVisual visual)
        {
            visual.IsOn = !visual.IsOn;

            if (visual.IsMaster)
            {
                _settings.Master.On = visual.IsOn;
                UpdateChannelVisualState(visual, "000000");

                foreach (ChannelStripVisual m in _monitorStrips)
                {
                    m.IsOn = visual.IsOn;
                    ScreenSettings? cfg = FindScreenConfig(m);
                    if (cfg != null)
                    {
                        cfg.On = visual.IsOn;
                        if (visual.IsOn && !_settings.Master.Proportional)
                        {
                            cfg.Dim = visual.Value;
                            m.Value = visual.Value;
                        }
                        UpdateChannelVisualState(m, cfg.Tint);
                    }
                }
                if (_applyGamma) _displayService.ApplyAll(_settings);
            }
            else
            {
                ScreenSettings? cfg = FindScreenConfig(visual);
                if (cfg != null)
                {
                    cfg.On = visual.IsOn;
                    UpdateChannelVisualState(visual, cfg.Tint);
                    if (_applyGamma)
                    {
                        _displayService.ApplyScreen(
                            cfg.DeviceName, cfg.On, cfg.Dim, cfg.Tint, cfg.TintIntensity, _settings.MaxDimPercent);
                    }
                }

                if (_masterStrip != null)
                {
                    _masterStrip.IsOn = false;
                    _settings.Master.On = false;
                    UpdateChannelVisualState(_masterStrip, "000000");
                }
            }

            UpdatePresetButtonsHighlight();
            _settingsStore.Save(_settings);
        }

        private void SetChannelValue(ChannelStripVisual visual, int newVal, bool fromUser)
        {
            int maxCap = _settings.MaxDimPercent > 0 ? _settings.MaxDimPercent : 90;
            int clamped = DimmingMath.ClampDimPercent(newVal, maxCap);
            int prevMaster = visual.Value;
            visual.Value = clamped;

            if (visual.IsMaster)
            {
                DimmingMath.ApplyMasterChange(_settings, visual.IsOn, clamped, prevMaster);
                UpdateChannelVisualState(visual, "000000");

                foreach (ChannelStripVisual m in _monitorStrips)
                {
                    m.IsOn = visual.IsOn;
                    ScreenSettings? cfg = FindScreenConfig(m);
                    if (cfg == null) continue;
                    m.Value = cfg.Dim;
                    UpdateChannelVisualState(m, cfg.Tint);
                }
                if (_applyGamma) _displayService.ApplyAll(_settings);
            }
            else
            {
                ScreenSettings? cfg = FindScreenConfig(visual);
                if (cfg != null)
                {
                    cfg.On = visual.IsOn;
                    cfg.Dim = clamped;
                    UpdateChannelVisualState(visual, cfg.Tint);
                    if (_applyGamma)
                    {
                        _displayService.ApplyScreen(
                            cfg.DeviceName, cfg.On, cfg.Dim, cfg.Tint, cfg.TintIntensity, _settings.MaxDimPercent);
                    }
                }

                if (fromUser && _masterStrip != null)
                {
                    _masterStrip.IsOn = false;
                    _settings.Master.On = false;
                    UpdateChannelVisualState(_masterStrip, "000000");
                }
            }

            UpdatePresetButtonsHighlight();
            _settingsStore.Save(_settings);
        }

        private void CycleMonitorTint(int screenId)
        {
            ChannelStripVisual? strip = _monitorStrips.FirstOrDefault(x => x.ScreenId == screenId);
            if (strip == null) return;
            ScreenSettings? cfg = FindScreenConfig(strip);
            if (cfg == null) return;

            cfg.Tint = DimmingMath.GetNextTintCode(cfg.Tint);
            UpdateChannelVisualState(strip, cfg.Tint);
            UpdateNightSyncDotVisual();
            RebuildTintSettingsRows();
            if (_applyGamma)
            {
                _displayService.ApplyScreen(
                    cfg.DeviceName, cfg.On, cfg.Dim, cfg.Tint, cfg.TintIntensity, _settings.MaxDimPercent);
            }
            _settingsStore.Save(_settings);
        }

        private void OnNightSyncClicked()
        {
            bool anyWarm = _settings.Screens.Any(s => !string.IsNullOrEmpty(s.Tint) && s.Tint != "000000");
            string targetTint = anyWarm ? "000000" : "2B1300";

            foreach (ScreenSettings cfg in _settings.Screens)
            {
                cfg.Tint = targetTint;
            }
            foreach (ChannelStripVisual strip in _monitorStrips)
            {
                UpdateChannelVisualState(strip, targetTint);
            }

            UpdateNightSyncDotVisual();
            RebuildTintSettingsRows();
            if (_applyGamma) _displayService.ApplyAll(_settings);
            _settingsStore.Save(_settings);
        }

        private void UpdateNightSyncDotVisual()
        {
            if (_nightSyncDot == null) return;
            bool anyWarm = _settings.Screens.Any(s => !string.IsNullOrEmpty(s.Tint) && s.Tint != "000000");
            _nightSyncDot.Fill = new SolidColorBrush(anyWarm ? ColSecondary : ColTextSecondary);
        }

        private void ApplyQuickPreset(int presetDim)
        {
            DimmingMath.ApplyQuickPreset(_settings, presetDim);

            if (_masterStrip != null)
            {
                _masterStrip.IsOn = _settings.Master.On;
                _masterStrip.Value = _settings.Master.Dim;
                UpdateChannelVisualState(_masterStrip, "000000");
            }

            foreach (ChannelStripVisual strip in _monitorStrips)
            {
                ScreenSettings? cfg = FindScreenConfig(strip);
                if (cfg == null) continue;
                strip.IsOn = cfg.On;
                strip.Value = cfg.Dim;
                UpdateChannelVisualState(strip, cfg.Tint);
            }

            if (_applyGamma) _displayService.ApplyAll(_settings);
            UpdatePresetButtonsHighlight();
            _settingsStore.Save(_settings);
        }

        private void UpdatePresetButtonsHighlight()
        {
            string targetLabel = !_settings.Master.On || _settings.Master.Dim == 0
                ? "Off"
                : (_settings.Master.Dim + "%");

            foreach (Button btn in _presetButtons)
            {
                bool match = string.Equals(btn.Content as string, targetLabel, StringComparison.Ordinal);
                StyleNavPillButton(btn, match);
                if (match)
                {
                    btn.Foreground = new SolidColorBrush(ColPrimary);
                }
            }
        }

        private ScrollViewer BuildSettingsView()
        {
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                IsVisible = false
            };

            var rootStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 4, 4)
            };

            var tintCard = new Border
            {
                Background = new SolidColorBrush(ColSurfaceCard),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var tintStack = new StackPanel { Orientation = Orientation.Vertical };
            var tintHeader = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Control tintIcon = CreateVectorIcon(SvgWbIncandescent, ColSecondary, 15, 15);
            tintIcon.Margin = new Thickness(0, 0, 6, 0);
            tintHeader.Children.Add(tintIcon);
            tintHeader.Children.Add(new TextBlock
            {
                Text = "Color Filter (Night Light / Tint)",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            });
            tintStack.Children.Add(tintHeader);
            tintStack.Children.Add(new TextBlock
            {
                Text = "Reduce eye strain at night by adding a warm tint to the hardware gamma LUT.",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(ColTextSecondary),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 8)
            });

            _tintSettingsHost = new StackPanel { Orientation = Orientation.Vertical };
            tintStack.Children.Add(_tintSettingsHost);
            tintCard.Child = tintStack;
            rootStack.Children.Add(tintCard);

            var behaviorCard = new Border
            {
                Background = new SolidColorBrush(ColSurfaceCard),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var behaviorStack = new StackPanel { Orientation = Orientation.Vertical };

            var behaviorHeader = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 0, 0, 6) };
            var bLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Control bIcon = CreateVectorIcon(SvgSliders, ColPrimary, 14, 14);
            bIcon.Margin = new Thickness(0, 0, 6, 0);
            bLeft.Children.Add(bIcon);
            bLeft.Children.Add(new TextBlock
            {
                Text = "Behavior & Display",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            });
            DockPanel.SetDock(bLeft, Dock.Left);
            behaviorHeader.Children.Add(bLeft);

            _configuredCountText = new TextBlock
            {
                Text = "0 ACTIVE",
                FontSize = 9.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(ColTextSecondary),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(_configuredCountText, Dock.Right);
            behaviorHeader.Children.Add(_configuredCountText);
            behaviorStack.Children.Add(behaviorHeader);

            behaviorStack.Children.Add(CreateSwitchRow(
                "Proportional Master mode",
                "Preserve relative brightness offset between screens",
                _settings.Master.Proportional,
                true,
                val => { _settings.Master.Proportional = val; UpdateConfiguredCounter(); _settingsStore.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Close button (\u2715) hides to tray",
                "Keeps dimming active in background when closed",
                _settings.CloseHidesToTray,
                true,
                val => { _settings.CloseHidesToTray = val; UpdateConfiguredCounter(); _settingsStore.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Show in taskbar",
                "Displays OpenDimmer in the desktop taskbar",
                _settings.ShowInTaskbar,
                true,
                val => { _settings.ShowInTaskbar = val; ShowInTaskbar = val; UpdateConfiguredCounter(); _settingsStore.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Start minimized in tray",
                "Launches silently in the system tray",
                _settings.StartMinimised,
                true,
                val => { _settings.StartMinimised = val; UpdateConfiguredCounter(); _settingsStore.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Launch at Linux startup",
                "Creates XDG autostart desktop entry on login",
                _settings.AutoRun,
                false,
                val => { _settings.AutoRun = val; LinuxAutoRunService.SetAutoRun(val); UpdateConfiguredCounter(); _settingsStore.Save(_settings); }));

            var actionGrid = new UniformGrid
            {
                Columns = 3,
                Margin = new Thickness(0, 10, 0, 2)
            };

            Button btnReload = CreateActionButton("Reload Displays", "Re-scan XRandR monitors and re-apply gamma");
            btnReload.Margin = new Thickness(0, 0, 4, 0);
            btnReload.Click += (s, e) => ReloadDisplays();

            Button btnReset = CreateActionButton("Reset Window", "Restore default size and bottom-right position");
            btnReset.Margin = new Thickness(2, 0, 2, 0);
            btnReset.Click += (s, e) => ResetPositionToCorner();

            Button btnDiag = CreateActionButton("Copy Diagnostics", "Copy OS, XRandR/EDID, and JSON config to clipboard");
            btnDiag.Margin = new Thickness(4, 0, 0, 0);
            btnDiag.Click += async (s, e) =>
            {
                string diag = _displayService.BuildDiagnosticsReport(_settings, _settingsStore.ActivePath);
                TopLevel? top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null)
                {
                    await top.Clipboard.SetTextAsync(diag);
                    btnDiag.Content = "Copied!";
                }
            };

            actionGrid.Children.Add(btnReload);
            actionGrid.Children.Add(btnReset);
            actionGrid.Children.Add(btnDiag);
            behaviorStack.Children.Add(actionGrid);

            behaviorCard.Child = behaviorStack;
            rootStack.Children.Add(behaviorCard);
            scroll.Content = rootStack;
            return scroll;
        }

        private void RebuildTintSettingsRows()
        {
            if (_tintSettingsHost == null) return;
            _tintSettingsHost.Children.Clear();

            var monitors = _displayService.Monitors;
            foreach (X11MonitorInfo mon in monitors)
            {
                MonitorDescriptor desc = mon.Descriptor;
                ScreenSettings? cfg = _settings.Screens.FirstOrDefault(x =>
                    string.Equals(x.DeviceName, desc.DeviceName, StringComparison.OrdinalIgnoreCase))
                    ?? _settings.Screens.FirstOrDefault(x => x.Id == desc.Index);
                if (cfg == null) continue;

                cfg.TintIntensity = DimmingMath.ClampTintIntensity(cfg.TintIntensity);
                TintPreset currentPreset = DimmingMath.FindTintPreset(cfg.Tint);
                bool isWarm = currentPreset.HexCode != "000000";

                var subCard = new Border
                {
                    Background = new SolidColorBrush(ColSurfaceSubCard),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                var subStack = new StackPanel { Orientation = Orientation.Vertical };
                var topDock = new DockPanel { LastChildFill = false };

                var leftInfo = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                leftInfo.Children.Add(new Border
                {
                    Background = new SolidColorBrush(ColSurfaceCard),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(0, 0, 7, 0),
                    Child = new TextBlock
                    {
                        Text = desc.PortBadge,
                        FontSize = 9.5,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(ColTextSecondary)
                    }
                });
                leftInfo.Children.Add(new TextBlock
                {
                    Text = desc.FriendlyName,
                    FontSize = 11.5,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(ColTextPrimary),
                    VerticalAlignment = VerticalAlignment.Center
                });
                DockPanel.SetDock(leftInfo, Dock.Left);
                topDock.Children.Add(leftInfo);

                var dropBtn = new Button
                {
                    Background = new SolidColorBrush(ColSurfaceCard),
                    BorderBrush = new SolidColorBrush(ColBorderSubtle),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 4, 8, 4),
                    Cursor = new Cursor(StandardCursorType.Hand)
                };
                var dropRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                dropRow.Children.Add(new Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = new SolidColorBrush(Color.Parse(currentPreset.DotHex)),
                    Margin = new Thickness(0, 0, 6, 0)
                });
                dropRow.Children.Add(new TextBlock
                {
                    Text = currentPreset.Name,
                    FontSize = 10.5,
                    FontWeight = FontWeight.Medium,
                    Foreground = new SolidColorBrush(ColTextPrimary),
                    Margin = new Thickness(0, 0, 4, 0)
                });
                dropRow.Children.Add(CreateVectorIcon(SvgChevronDown, ColTextSecondary, 11, 11));
                dropBtn.Content = dropRow;

                ScreenSettings capturedCfg = cfg;
                dropBtn.Click += (s, e) => ShowTintPresetDropdown(capturedCfg);
                DockPanel.SetDock(dropBtn, Dock.Right);
                topDock.Children.Add(dropBtn);
                subStack.Children.Add(topDock);

                if (isWarm)
                {
                    var intensityGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
                    intensityGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                    intensityGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                    intensityGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

                    var kLabel = new TextBlock
                    {
                        Text = currentPreset.KelvinLabel + " Intensity",
                        FontSize = 9.5,
                        Foreground = new SolidColorBrush(ColTextSecondary),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 8, 0)
                    };
                    Grid.SetColumn(kLabel, 0);
                    intensityGrid.Children.Add(kLabel);

                    var pctText = new TextBlock
                    {
                        Text = capturedCfg.TintIntensity + "%",
                        FontSize = 9.5,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(ColSecondary),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    Grid.SetColumn(pctText, 2);
                    intensityGrid.Children.Add(pctText);

                    var slider = new Slider
                    {
                        Minimum = 10,
                        Maximum = 100,
                        Value = capturedCfg.TintIntensity,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    slider.PropertyChanged += (s, e) =>
                    {
                        if (e.Property == RangeBase.ValueProperty)
                        {
                            int val = (int)Math.Round(slider.Value);
                            capturedCfg.TintIntensity = val;
                            pctText.Text = val + "%";
                            if (_applyGamma)
                            {
                                _displayService.ApplyScreen(
                                    capturedCfg.DeviceName, capturedCfg.On, capturedCfg.Dim,
                                    capturedCfg.Tint, capturedCfg.TintIntensity, _settings.MaxDimPercent);
                            }
                            _settingsStore.Save(_settings);
                        }
                    };
                    Grid.SetColumn(slider, 1);
                    intensityGrid.Children.Add(slider);
                    subStack.Children.Add(intensityGrid);
                }

                subCard.Child = subStack;
                _tintSettingsHost.Children.Add(subCard);
            }
        }

        private void ShowTintPresetDropdown(ScreenSettings cfg)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical };

            var header = new DockPanel { LastChildFill = false, Margin = new Thickness(4, 2, 4, 6) };
            var hLeft = new TextBlock
            {
                Text = "TINT PRESET",
                FontSize = 9.5,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(ColTextSecondary)
            };
            DockPanel.SetDock(hLeft, Dock.Left);
            header.Children.Add(hLeft);

            var hRight = new TextBlock
            {
                Text = "4 Modes",
                FontSize = 9.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(ColSecondary)
            };
            DockPanel.SetDock(hRight, Dock.Right);
            header.Children.Add(hRight);
            stack.Children.Add(header);

            foreach (TintPreset preset in DimmingMath.TintPresets)
            {
                TintPreset captured = preset;
                bool active = string.Equals(cfg.Tint, captured.HexCode, StringComparison.OrdinalIgnoreCase);

                var optBtn = new Button
                {
                    Background = active ? new SolidColorBrush(ColSurfaceCard) : Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 1, 0, 1),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Cursor = new Cursor(StandardCursorType.Hand)
                };

                var row = new DockPanel { LastChildFill = false };
                var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                left.Children.Add(new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.Parse(captured.DotHex)),
                    Margin = new Thickness(0, 0, 8, 0)
                });
                var textStack = new StackPanel { Orientation = Orientation.Vertical };
                textStack.Children.Add(new TextBlock
                {
                    Text = captured.Name,
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(ColTextPrimary)
                });
                textStack.Children.Add(new TextBlock
                {
                    Text = captured.Subtitle,
                    FontSize = 9.5,
                    Foreground = new SolidColorBrush(ColTextSecondary)
                });
                left.Children.Add(textStack);
                DockPanel.SetDock(left, Dock.Left);
                row.Children.Add(left);

                if (active)
                {
                    var check = new TextBlock
                    {
                        Text = "\u2713",
                        FontSize = 11,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(ColPrimary),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    DockPanel.SetDock(check, Dock.Right);
                    row.Children.Add(check);
                }

                optBtn.Content = row;
                optBtn.Click += (s, e) =>
                {
                    cfg.Tint = captured.HexCode;
                    CloseTintPresetDropdown();
                    RebuildTintSettingsRows();
                    ChannelStripVisual? strip = _monitorStrips.FirstOrDefault(x =>
                        string.Equals(x.DeviceName, cfg.DeviceName, StringComparison.OrdinalIgnoreCase));
                    if (strip != null)
                    {
                        UpdateChannelVisualState(strip, cfg.Tint);
                    }
                    UpdateNightSyncDotVisual();
                    if (_applyGamma)
                    {
                        _displayService.ApplyScreen(
                            cfg.DeviceName, cfg.On, cfg.Dim, cfg.Tint, cfg.TintIntensity, _settings.MaxDimPercent);
                    }
                    _settingsStore.Save(_settings);
                };

                stack.Children.Add(optBtn);
            }

            _dropdownMenuCard.Child = stack;
            _dropdownOverlayLayer.IsVisible = true;
        }

        private void CloseTintPresetDropdown()
        {
            _dropdownOverlayLayer.IsVisible = false;
        }

        private StackPanel CreateSwitchRow(
            string title,
            string subtitle,
            bool initialValue,
            bool showBottomDivider,
            Action<bool> onChanged)
        {
            var container = new StackPanel { Orientation = Orientation.Vertical };

            var row = new DockPanel
            {
                LastChildFill = false,
                Margin = new Thickness(0, 6, 0, 6)
            };

            var textStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 240
            };
            textStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(ColTextPrimary)
            });
            textStack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 9.5,
                Foreground = new SolidColorBrush(ColTextSecondary),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 1, 0, 0)
            });
            DockPanel.SetDock(textStack, Dock.Left);
            row.Children.Add(textStack);

            bool state = initialValue;
            var swBorder = new Border
            {
                Width = 34,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush(state ? ColPrimary : ColSwitchOff),
                Padding = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            var swKnob = new Border
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(state ? ColTextPrimary : ColTextSecondary),
                HorizontalAlignment = state ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            swBorder.Child = swKnob;

            var swBtn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Content = swBorder,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            swBtn.Click += (s, e) =>
            {
                state = !state;
                swBorder.Background = new SolidColorBrush(state ? ColPrimary : ColSwitchOff);
                swKnob.Background = new SolidColorBrush(state ? ColTextPrimary : ColTextSecondary);
                swKnob.HorizontalAlignment = state ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                onChanged(state);
            };

            DockPanel.SetDock(swBtn, Dock.Right);
            row.Children.Add(swBtn);
            container.Children.Add(row);

            if (showBottomDivider)
            {
                container.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.Parse("#2A2F37"))
                });
            }

            return container;
        }

        private static Button CreateActionButton(string label, string tooltip)
        {
            var btn = new Button
            {
                Content = label,
                Height = 28,
                FontSize = 10.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                Background = new SolidColorBrush(ColSurfaceInput),
                BorderBrush = new SolidColorBrush(ColBorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            ToolTip.SetTip(btn, tooltip);
            return btn;
        }

        private Grid BuildFooterBar()
        {
            var footer = new Grid { Background = new SolidColorBrush(ColCanvas) };
            footer.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            footer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            left.Children.Add(new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(ColPrimaryStrong),
                Margin = new Thickness(0, 0, 6, 0)
            });
            _footerStatusText = new TextBlock
            {
                Text = "XRANDR HARDWARE GAMMA ACTIVE",
                FontSize = 9.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(ColTextSecondary),
                VerticalAlignment = VerticalAlignment.Center
            };
            left.Children.Add(_footerStatusText);
            Grid.SetColumn(left, 0);
            footer.Children.Add(left);

            var verText = new TextBlock
            {
                Text = "v2.0-dev \u00B7 Linux X11",
                FontSize = 9.5,
                Foreground = new SolidColorBrush(ColTextMuted),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(verText, 1);
            footer.Children.Add(verText);

            return footer;
        }

        private void UpdateConfiguredCounter()
        {
            if (_configuredCountText == null) return;
            int active = DimmingMath.CountActiveBehaviorSettings(_settings, false);
            _configuredCountText.Text = active + " ACTIVE";
        }

        private void SwitchView(string view)
        {
            bool isScreens = string.Equals(view, "Screens", StringComparison.Ordinal);
            _screensView.IsVisible = isScreens;
            _settingsView.IsVisible = !isScreens;
            StyleNavPillButton(_navScreensBtn, isScreens);
            StyleNavPillButton(_navSettingsBtn, !isScreens);
        }

        public void ReloadDisplays()
        {
            List<X11MonitorInfo> monitors = _displayService.DiscoverMonitors();
            var names = monitors.Select(m => m.Descriptor.DeviceName).ToList();
            _settingsStore.SyncScreensNonDestructively(_settings, names);
            RebuildChannelStrips();
            RebuildTintSettingsRows();
            if (_applyGamma) _displayService.ApplyAll(_settings);
            _settingsStore.Save(_settings);
        }

        public void ToggleMasterFromTray()
        {
            if (_masterStrip != null)
            {
                ToggleChannelPower(_masterStrip);
            }
        }

        public void TriggerNightSyncFromTray()
        {
            OnNightSyncClicked();
        }

        public void ShowAndActivate()
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
        }

        public void PrepareForShutdown()
        {
            _explicitExit = true;
            SaveWindowPosition();
        }

        private void RestoreOrResetPosition()
        {
            if (_settings.Position.Width.HasValue)
            {
                Width = Math.Max(MinWidth, _settings.Position.Width.Value);
            }
            if (_settings.Position.Height.HasValue)
            {
                Height = Math.Max(MinHeight, _settings.Position.Height.Value);
            }

            if (_settings.Position.Left.HasValue && _settings.Position.Top.HasValue)
            {
                int savedX = (int)Math.Round(_settings.Position.Left.Value);
                int savedY = (int)Math.Round(_settings.Position.Top.Value);
                bool visibleOnAnyScreen = Screens.All.Any(s =>
                    savedX < s.Bounds.Right - 40
                    && savedX + Width > s.Bounds.X + 40
                    && savedY < s.Bounds.Bottom - 40
                    && savedY + Height > s.Bounds.Y + 40);

                if (visibleOnAnyScreen)
                {
                    Position = new PixelPoint(savedX, savedY);
                    return;
                }
            }

            ResetPositionToCorner();
        }

        public void ResetPositionToCorner()
        {
            WindowState = WindowState.Normal;
            Width = 392;
            Height = 478;

            var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
            if (screen != null)
            {
                PixelRect work = screen.WorkingArea;
                double scale = screen.Scaling > 0 ? screen.Scaling : 1.0;
                int pxW = (int)Math.Round(Width * scale);
                int pxH = (int)Math.Round(Height * scale);
                int x = Math.Max(work.X, work.Right - pxW - 16);
                int y = Math.Max(work.Y, work.Bottom - pxH - 16);
                Position = new PixelPoint(x, y);
            }
            SaveWindowPosition();
        }

        private void ToggleMaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void OnBtnMinimiseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_settings.ShowInTaskbar)
            {
                WindowState = WindowState.Minimized;
            }
            else
            {
                Hide();
            }
        }

        private void OnBtnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SaveWindowPosition();
            if (_settings.CloseHidesToTray)
            {
                Hide();
            }
            else
            {
                _explicitExit = true;
                _shutdownCallback();
            }
        }

        private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            if (!_explicitExit && _settings.CloseHidesToTray)
            {
                e.Cancel = true;
                SaveWindowPosition();
                Hide();
                return;
            }

            SaveWindowPosition();
        }

        private void SaveWindowPosition()
        {
            if (WindowState == WindowState.Normal)
            {
                _settings.Position.Left = Position.X;
                _settings.Position.Top = Position.Y;
                _settings.Position.Width = Width;
                _settings.Position.Height = Height;
            }
            _settingsStore.Save(_settings);
        }

        private ScreenSettings? FindScreenConfig(ChannelStripVisual visual)
        {
            return _settings.Screens.FirstOrDefault(x =>
                string.Equals(x.DeviceName, visual.DeviceName, StringComparison.OrdinalIgnoreCase))
                ?? _settings.Screens.FirstOrDefault(x => x.Id == visual.ScreenId);
        }
    }
}
