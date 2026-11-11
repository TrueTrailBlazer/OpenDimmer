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
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Shell;
using OpenDimmer.Models;
using OpenDimmer.Services;
using DrawingColor = System.Drawing.Color;
using DrawingPen = System.Drawing.Pen;
using Forms = System.Windows.Forms;
using MediaColor = System.Windows.Media.Color;
using WpfPath = System.Windows.Shapes.Path;

namespace OpenDimmer.UI
{
    internal sealed class DarkMenuColorTable : Forms.ProfessionalColorTable
    {
        public override DrawingColor MenuBorder { get { return DrawingColor.FromArgb(45, 51, 59); } }
        public override DrawingColor MenuItemBorder { get { return DrawingColor.FromArgb(77, 142, 255); } }
        public override DrawingColor MenuItemSelected { get { return DrawingColor.FromArgb(39, 44, 52); } }
        public override DrawingColor ToolStripDropDownBackground { get { return DrawingColor.FromArgb(24, 27, 32); } }
        public override DrawingColor ImageMarginGradientBegin { get { return DrawingColor.FromArgb(24, 27, 32); } }
        public override DrawingColor ImageMarginGradientMiddle { get { return DrawingColor.FromArgb(24, 27, 32); } }
        public override DrawingColor ImageMarginGradientEnd { get { return DrawingColor.FromArgb(24, 27, 32); } }
        public override DrawingColor SeparatorDark { get { return DrawingColor.FromArgb(45, 51, 59); } }
        public override DrawingColor SeparatorLight { get { return DrawingColor.FromArgb(24, 27, 32); } }
    }

    internal sealed class DarkTrayMenuRenderer : Forms.ToolStripProfessionalRenderer
    {
        public DarkTrayMenuRenderer() : base(new DarkMenuColorTable()) { }

        protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected
                ? DrawingColor.FromArgb(243, 244, 246)
                : DrawingColor.FromArgb(194, 198, 214);
            base.OnRenderItemText(e);
        }
    }

    public sealed class MainWindow : Window
    {
        // Exact Google Stitch Color Palette
        private static readonly MediaColor ColSurfaceCanvas = MediaColor.FromRgb(24, 27, 32);       // #181B20
        private static readonly MediaColor ColSurfaceCard = MediaColor.FromRgb(34, 39, 46);         // #22272E
        private static readonly MediaColor ColSurfaceSubCard = MediaColor.FromRgb(22, 25, 30);      // #16191E
        private static readonly MediaColor ColSurfaceMaster = MediaColor.FromRgb(31, 41, 55);       // #1F2937
        private static readonly MediaColor ColSurfaceInput = MediaColor.FromRgb(20, 23, 28);        // #14171C
        private static readonly MediaColor ColSurfaceTrack = MediaColor.FromRgb(17, 20, 24);        // #111418
        private static readonly MediaColor ColSurfaceSwitchOff = MediaColor.FromRgb(54, 57, 63);    // #36393F
        private static readonly MediaColor ColSurfaceHover = MediaColor.FromRgb(39, 42, 47);        // #272A2F
        private static readonly MediaColor ColBorderSubtle = MediaColor.FromRgb(45, 51, 59);        // #2D333B
        private static readonly MediaColor ColDivider = MediaColor.FromRgb(42, 47, 55);             // #2A2F37
        private static readonly MediaColor ColPrimary = MediaColor.FromRgb(173, 198, 255);          // #ADC6FF
        private static readonly MediaColor ColPrimaryContainer = MediaColor.FromRgb(77, 142, 255);  // #4D8EFF
        private static readonly MediaColor ColOnPrimary = MediaColor.FromRgb(0, 40, 93);            // #00285D
        private static readonly MediaColor ColSecondary = MediaColor.FromRgb(255, 185, 95);         // #FFB95F
        private static readonly MediaColor ColTextPrimary = MediaColor.FromRgb(243, 244, 246);      // #F3F4F6
        private static readonly MediaColor ColTextSecondary = MediaColor.FromRgb(156, 163, 175);    // #9CA3AF
        private static readonly MediaColor ColOutlineVariant = MediaColor.FromRgb(66, 71, 84);      // #424754
        private static readonly MediaColor ColErrorContainer = MediaColor.FromRgb(147, 0, 10);      // #93000A

        // Crisp Material Symbols Vector Geometries
        private const string SvgBrightnessMedium = "M12,18V6A6,6 0 0,1 18,12A6,6 0 0,1 12,18M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4M12,1L14.39,3.42C13.65,3.15 12.84,3 12,3C11.16,3 10.35,3.15 9.61,3.42L12,1M3.34,7L7.5,6.65C6.9,7.16 6.36,7.78 5.94,8.5C5.5,9.24 5.25,10 5.11,10.79L3.34,7M3.36,17L5.12,13.23C5.26,14 5.53,14.78 5.95,15.5C6.37,16.24 6.91,16.86 7.5,17.37L3.36,17M20.65,7L18.88,10.79C18.74,10 18.47,9.23 18.05,8.5C17.63,7.78 17.1,7.15 16.5,6.64L20.65,7M20.64,17L16.5,17.36C17.09,16.85 17.62,16.22 18.04,15.5C18.46,14.77 18.73,14 18.87,13.21L20.64,17M12,23L9.59,20.56C10.33,20.83 11.14,21 12,21C12.86,21 13.67,20.83 14.41,20.56L12,23Z";
        private const string SvgLink = "M3.9,12C3.9,10.29 5.29,8.9 7,8.9H11V7H7A5,5 0 0,0 2,12A5,5 0 0,0 7,17H11V15.1H7C5.29,15.1 3.9,13.71 3.9,12M8,13H16V11H8V13M17,7H13V8.9H17C18.71,8.9 20.1,10.29 20.1,12C20.1,13.71 18.71,15.1 17,15.1H13V17H17A5,5 0 0,0 22,12A5,5 0 0,0 17,7Z";
        private const string SvgNightlight = "M14,2C15.82,2 17.53,2.5 19,3.35C16.06,5.08 14,8.3 14,12C14,15.7 16.06,18.92 19,20.65C17.53,21.5 15.82,22 14,22A10,10 0 0,1 4,12A10,10 0 0,1 14,2Z";
        private const string SvgDesktop = "M21,2H3C1.89,2 1,2.89 1,4V16A2,2 0 0,0 3,18H10V20H8V22H16V20H14V18H21A2,2 0 0,0 23,16V4C23,2.89 22.1,2 21,2M21,16H3V4H21V16Z";
        private const string SvgSunSmall = "M12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,2L14.39,5.42C13.65,5.15 12.84,5 12,5C11.16,5 10.35,5.15 9.61,5.42L12,2M3.34,7L7.5,6.65C6.9,7.16 6.36,7.78 5.94,8.5C5.5,9.24 5.25,10 5.11,10.79L3.34,7M3.36,17L5.12,13.23C5.26,14 5.53,14.78 5.95,15.5C6.37,16.24 6.91,16.86 7.5,17.37L3.36,17M20.65,7L18.88,10.79C18.74,10 18.47,9.23 18.05,8.5C17.63,7.78 17.1,7.15 16.5,6.64L20.65,7M20.64,17L16.5,17.36C17.09,16.85 17.62,16.22 18.04,15.5C18.46,14.77 18.73,14 18.87,13.21L20.64,17M12,22L9.59,18.56C10.33,18.83 11.14,19 12,19C12.86,19 13.67,18.83 14.41,18.56L12,22Z";
        private const string SvgSliders = "M3,17V19H9V17H3M3,5V7H13V5H3M13,21V19H21V17H13V15H11V21H13M7,9V11H3V13H7V15H9V9H7M21,13V11H11V13H21M15,9H17V7H21V5H17V3H15V9Z";
        private const string SvgChevronDown = "M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z";

        private static readonly string[] TintCodes = new[] { "000000", "2B1300", "330D00", "1F190D" };
        private static readonly string[] TintBadges = new[] { "Native", "4500K", "3500K", "Sepia" };
        private static readonly string[] TintDropdownLabels = new[] { "Neutral Black", "Warm Amber", "Deep Ember", "Soft Sepia" };

        private readonly AppSettings _settings;
        private readonly SettingsService _settingsService;
        private readonly OverlayManager _overlayManager;
        private Forms.NotifyIcon _notifyIcon;
        private IntPtr _trayIconHandle;

        private Grid _screensView;
        private UniformGrid _channelsGrid;
        private ScrollViewer _settingsScroll;
        private StackPanel _tintSettingsHost;
        private TextBlock _configuredCountText;
        private TextBlock _footerStatusText;
        private Ellipse _nightSyncDot;

        private Button _navBtnScreens;
        private Button _navBtnSettings;

        private ChannelStripVisual _masterStrip;
        private readonly List<ChannelStripVisual> _monitorStrips = new List<ChannelStripVisual>();
        private readonly List<Button> _presetButtons = new List<Button>();
        private bool _suppressEvents;

        private sealed class ChannelStripVisual
        {
            public bool IsMaster;
            public int ScreenId;
            public bool IsOn;
            public int Value;
            public Border CardBorder;
            public Border BadgeBox;
            public TextBlock BadgeText;
            public ContentControl TopRightIconSlot;
            public TextBlock StatusSubtitle;
            public Ellipse StatusDot;
            public Grid SliderHitArea;
            public Border TrackFill;
            public Border ThumbKnob;
            public TextBox ValueInput;
            public Ellipse MetricDot;
            public TextBlock MetricLabel;
        }

        public MainWindow(AppSettings settings, SettingsService settingsService, OverlayManager overlayManager)
        {
            _settings = settings;
            _settingsService = settingsService;
            _overlayManager = overlayManager;

            Title = "OpenDimmer";
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");

            MinWidth = 340;
            MinHeight = 410;
            Width = 386;
            Height = 468;

            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(6),
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(12)
            });

            Topmost = _settings.NonDimmableWindow;
            ShowInTaskbar = _settings.ShowInTaskbar;

            AutoRunService.SetAutoRun(_settings.AutoRun);

            InstallCustomScrollbarStyle();
            BuildUserInterface();
            BuildSystemTrayIcon();
            RestoreOrResetPosition();

            _overlayManager.DisplaysReconfigured += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    RebuildChannelStrips();
                    RebuildTintSettingsRows();
                });
            };

            SizeChanged += (s, e) =>
            {
                if (IsLoaded && WindowState == WindowState.Normal)
                {
                    _settings.Position.Width = Width;
                    _settings.Position.Height = Height;
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
            NativeMethods.TrimMemoryWorkingSet();
        }

        private static WpfPath CreateVectorIcon(string pathData, MediaColor color, double width, double height)
        {
            return new WpfPath
            {
                Data = Geometry.Parse(pathData),
                Fill = new SolidColorBrush(color),
                Stretch = Stretch.Uniform,
                Width = width,
                Height = height,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private void InstallCustomScrollbarStyle()
        {
            const string scrollBarXaml = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                       TargetType='{x:Type ScrollBar}'>
                    <Setter Property='Width' Value='4'/>
                    <Setter Property='MinWidth' Value='4'/>
                    <Setter Property='Background' Value='Transparent'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='{x:Type ScrollBar}'>
                                <Track Name='PART_Track' IsDirectionReversed='True'>
                                    <Track.Thumb>
                                        <Thumb>
                                            <Thumb.Template>
                                                <ControlTemplate TargetType='{x:Type Thumb}'>
                                                    <Border Background='#374151' CornerRadius='2' Margin='0,2,0,2'/>
                                                </ControlTemplate>
                                            </Thumb.Template>
                                        </Thumb>
                                    </Track.Thumb>
                                </Track>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>";

            try
            {
                var style = (Style)System.Windows.Markup.XamlReader.Parse(scrollBarXaml);
                Resources.Add(typeof(ScrollBar), style);
            }
            catch { }
        }

        private void BuildUserInterface()
        {
            // Rounded 12px outer frame matching Stitch window shell
            var windowFrame = new Border
            {
                Background = new SolidColorBrush(ColSurfaceCanvas),
                BorderBrush = new SolidColorBrush(ColBorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true
            };

            var outerGrid = new Grid();
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });

            var header = BuildHeaderBar();
            Grid.SetRow(header, 0);
            outerGrid.Children.Add(header);

            var mainContent = new Grid { Margin = new Thickness(10, 2, 10, 4) };
            _screensView = BuildScreensView();
            _settingsScroll = BuildSettingsView();

            mainContent.Children.Add(_screensView);
            mainContent.Children.Add(_settingsScroll);

            Grid.SetRow(mainContent, 1);
            outerGrid.Children.Add(mainContent);

            var footer = BuildFooterBar();
            Grid.SetRow(footer, 2);
            outerGrid.Children.Add(footer);

            windowFrame.Child = outerGrid;
            Content = windowFrame;

            RebuildChannelStrips();
            RebuildTintSettingsRows();
            UpdateConfiguredCounter();
            SwitchView("screens");
        }

        private Grid BuildHeaderBar()
        {
            var header = new Grid
            {
                Background = new SolidColorBrush(ColSurfaceCanvas),
                Margin = new Thickness(12, 0, 8, 0)
            };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            header.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    ToggleMaximizeRestore();
                }
                else if (_settings.MovableWindow && e.ButtonState == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            };

            // Left: Vector Sun/Starburst + OpenDimmer
            var brandStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var brandIcon = CreateVectorIcon(SvgBrightnessMedium, ColPrimary, 16, 16);
            brandIcon.Margin = new Thickness(0, 0, 6, 0);

            var titleText = new TextBlock
            {
                Text = "OpenDimmer",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            };

            brandStack.Children.Add(brandIcon);
            brandStack.Children.Add(titleText);
            Grid.SetColumn(brandStack, 0);
            header.Children.Add(brandStack);

            // Center: Segmented Pill (Screens | Settings)
            var navPill = new Border
            {
                Background = new SolidColorBrush(ColSurfaceInput),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var navStack = new StackPanel { Orientation = Orientation.Horizontal };
            _navBtnScreens = CreateNavPillButton("Screens", true);
            _navBtnSettings = CreateNavPillButton("Settings", false);

            _navBtnScreens.Click += (s, e) => SwitchView("screens");
            _navBtnSettings.Click += (s, e) =>
            {
                RebuildTintSettingsRows();
                UpdateConfiguredCounter();
                SwitchView("settings");
            };

            navStack.Children.Add(_navBtnScreens);
            navStack.Children.Add(_navBtnSettings);
            navPill.Child = navStack;
            Grid.SetColumn(navPill, 1);
            header.Children.Add(navPill);

            // Right: Window Controls
            var winControls = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            var btnMin = CreateWindowControlButton("—", "Minimize", false);
            btnMin.Click += OnBtnMinimiseClick;

            var btnMax = CreateWindowControlButton("▢", "Maximize / Restore", false);
            btnMax.Click += (s, e) => ToggleMaximizeRestore();

            var btnClose = CreateWindowControlButton("✕", "Close / Hide to tray", true);
            btnClose.Click += OnBtnCloseClick;

            winControls.Children.Add(btnMin);
            winControls.Children.Add(btnMax);
            winControls.Children.Add(btnClose);
            Grid.SetColumn(winControls, 2);
            header.Children.Add(winControls);

            return header;
        }

        private Button CreateNavPillButton(string label, bool active)
        {
            var btn = new Button
            {
                Content = label,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10, 3, 10, 3),
                Margin = new Thickness(1, 0, 1, 0)
            };

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;
            btn.Template = template;

            StyleNavPillButton(btn, active);
            return btn;
        }

        private void StyleNavPillButton(Button btn, bool active)
        {
            btn.Background = new SolidColorBrush(active ? ColSurfaceCard : Colors.Transparent);
            btn.Foreground = new SolidColorBrush(active ? ColTextPrimary : ColTextSecondary);
        }

        private Button CreateWindowControlButton(string symbol, string tooltip, bool isClose)
        {
            var btn = new Button
            {
                Content = symbol,
                ToolTip = tooltip,
                Width = 24,
                Height = 24,
                Margin = new Thickness(2, 0, 0, 0),
                FontSize = 10.5,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new SolidColorBrush(ColTextSecondary),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;
            btn.Template = template;

            btn.MouseEnter += (s, e) =>
            {
                btn.Background = new SolidColorBrush(isClose ? ColErrorContainer : ColSurfaceHover);
                btn.Foreground = new SolidColorBrush(ColTextPrimary);
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.Background = System.Windows.Media.Brushes.Transparent;
                btn.Foreground = new SolidColorBrush(ColTextSecondary);
            };

            return btn;
        }

        private Grid BuildScreensView()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _channelsGrid = new UniformGrid
            {
                Rows = 1,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(_channelsGrid, 0);
            grid.Children.Add(_channelsGrid);

            // Bottom Utility & Quick Preset Strip (Borderless elevated surface #22272E matching Stitch)
            var bottomBar = new Border
            {
                Background = new SolidColorBrush(ColSurfaceCard),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6)
            };

            var bottomDock = new DockPanel { LastChildFill = false };

            // Left: Night Sync Quick Toggle
            var nightBtn = CreateStyledPillButton();
            nightBtn.ToolTip = "Toggle warm Night Light (4500K) across all monitors";
            var nightStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
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

            var nightText = new TextBlock
            {
                Text = "Night Sync",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColTextSecondary),
                VerticalAlignment = VerticalAlignment.Center
            };
            nightStack.Children.Add(dotHolder);
            nightStack.Children.Add(nightText);
            nightBtn.Content = nightStack;
            nightBtn.Click += (s, e) => OnNightSyncClicked();

            DockPanel.SetDock(nightBtn, Dock.Left);
            bottomDock.Children.Add(nightBtn);

            // Right: Quick Presets (25%, 50%, 75%, Off)
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
                int capturedPreset = p;
                string label = p == 0 ? "Off" : (p + "%");
                var pBtn = CreateNavPillButton(label, p == _settings.Master.Dim);
                pBtn.FontSize = 10;
                pBtn.Padding = new Thickness(8, 2, 8, 2);
                pBtn.ToolTip = p == 0 ? "Disable dimming (100% brightness)" : ("Set dimming to " + p + "%");
                pBtn.Click += (s, e) => ApplyQuickPreset(capturedPreset);
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

        private Button CreateStyledPillButton()
        {
            var btn = new Button
            {
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Padding = new Thickness(2, 2, 6, 2)
            };
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            border.AppendChild(presenter);
            template.VisualTree = border;
            btn.Template = template;
            return btn;
        }

        private void RebuildChannelStrips()
        {
            _suppressEvents = true;
            _channelsGrid.Children.Clear();
            _monitorStrips.Clear();

            var screens = Forms.Screen.AllScreens;
            _channelsGrid.Columns = screens.Length + 1;

            _masterStrip = CreateChannelStrip(
                true,
                0,
                "Master",
                _settings.Master.On ? "Linked All" : "Unlinked",
                _settings.Master.On,
                _settings.Master.Dim,
                "000000");
            _channelsGrid.Children.Add(_masterStrip.CardBorder);

            for (int i = 0; i < screens.Length; i++)
            {
                int id = i + 1;
                var screen = screens[i];
                var cfg = _settings.Screens.FirstOrDefault(x => x.Id == id);
                if (cfg == null) continue;

                var hwInfo = DisplayInfoService.GetMonitorInfo(screen, id);

                var strip = CreateChannelStrip(
                    false,
                    id,
                    hwInfo.FriendlyName,
                    hwInfo.Subtitle,
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
            string title,
            string subtitle,
            bool isOn,
            int dimValue,
            string tintHex)
        {
            var visual = new ChannelStripVisual
            {
                IsMaster = isMaster,
                ScreenId = screenId,
                IsOn = isOn,
                Value = Math.Max(0, Math.Min(90, dimValue))
            };

            // Borderless card surface (#1F2937 for Master, #22272E for Monitors)
            var card = new Border
            {
                Background = new SolidColorBrush(isMaster ? ColSurfaceMaster : ColSurfaceCard),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 10, 10, 10),
                Margin = new Thickness(3, 0, 3, 0),
                ClipToBounds = true
            };
            visual.CardBorder = card;

            var cardContainer = new Grid();

            // Subtle top-left radial glow on Master card (matching Stitch)
            if (isMaster)
            {
                var glow = new Ellipse
                {
                    Width = 110,
                    Height = 110,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(-45, -45, 0, 0),
                    IsHitTestVisible = false,
                    Fill = new RadialGradientBrush(
                        MediaColor.FromArgb(28, 173, 198, 255),
                        MediaColor.FromArgb(0, 173, 198, 255))
                };
                cardContainer.Children.Add(glow);
            }

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 1. Top Header (Badge + Crisp SVG Icon + Title + Subtitle)
            var headerStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var topRow = new DockPanel
            {
                LastChildFill = false,
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = Cursors.Hand,
                Background = System.Windows.Media.Brushes.Transparent,
                ToolTip = isMaster ? "Click to toggle Master dimming on all screens" : ("Click to toggle " + title)
            };

            var badge = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = isMaster ? "M" : screenId.ToString(),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            visual.BadgeBox = badge;
            visual.BadgeText = (TextBlock)badge.Child;
            DockPanel.SetDock(badge, Dock.Left);
            topRow.Children.Add(badge);

            var iconSlot = new ContentControl
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            visual.TopRightIconSlot = iconSlot;
            DockPanel.SetDock(iconSlot, Dock.Right);
            topRow.Children.Add(iconSlot);

            topRow.MouseLeftButtonDown += (s, e) =>
            {
                ToggleChannelPower(visual);
                e.Handled = true;
            };

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var subStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };

            var statusDot = new Ellipse
            {
                Width = 4,
                Height = 4,
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = isMaster ? Visibility.Visible : Visibility.Collapsed
            };
            visual.StatusDot = statusDot;

            var subBlock = new TextBlock
            {
                Text = subtitle,
                FontSize = 10,
                Foreground = new SolidColorBrush(isMaster ? ColPrimary : ColTextSecondary)
            };
            visual.StatusSubtitle = subBlock;

            subStack.Children.Add(statusDot);
            subStack.Children.Add(subBlock);

            headerStack.Children.Add(topRow);
            headerStack.Children.Add(titleBlock);
            headerStack.Children.Add(subStack);

            Grid.SetRow(headerStack, 0);
            rootGrid.Children.Add(headerStack);

            // 2. Center Tactile Vertical Slider (Ticks positioned right next to slider track)
            var sliderArea = new Grid
            {
                Margin = new Thickness(0, 8, 0, 8),
                MinHeight = 140,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 64
            };

            var ticksGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(2, 6, 0, 6),
                IsHitTestVisible = false
            };
            string[] tickLabels = isMaster ? new[] { "100", "75", "50", "25", "0" } : new[] { "-", "-", "-", "-", "-" };
            for (int t = 0; t < tickLabels.Length; t++)
            {
                ticksGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = t == tickLabels.Length - 1 ? GridLength.Auto : new GridLength(1, GridUnitType.Star)
                });
                var tb = new TextBlock
                {
                    Text = tickLabels[t],
                    FontSize = 9,
                    Foreground = new SolidColorBrush(ColOutlineVariant),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetRow(tb, t);
                ticksGrid.Children.Add(tb);
            }
            sliderArea.Children.Add(ticksGrid);

            var sliderHit = new Grid
            {
                Width = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(10, 0, 0, 0),
                Background = System.Windows.Media.Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            visual.SliderHitArea = sliderHit;

            var trackBg = new Border
            {
                Width = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(ColSurfaceTrack),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 8),
                ClipToBounds = true
            };

            var trackFill = new Border
            {
                Width = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(isMaster ? ColPrimaryContainer : ColPrimary),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 0
            };
            visual.TrackFill = trackFill;
            trackBg.Child = trackFill;

            var thumbKnob = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(ColTextPrimary),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 4,
                    ShadowDepth = 1,
                    Opacity = 0.55
                },
                Child = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(isMaster ? ColSurfaceMaster : ColSurfaceCard),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            visual.ThumbKnob = thumbKnob;

            sliderHit.Children.Add(trackBg);
            sliderHit.Children.Add(thumbKnob);
            sliderArea.Children.Add(sliderHit);

            BindCustomSliderEvents(visual);

            Grid.SetRow(sliderArea, 1);
            rootGrid.Children.Add(sliderArea);

            // 3. Bottom Numeric Pill & Metric Badge
            var bottomStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var inputPill = new Border
            {
                Width = 44,
                Height = 24,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(ColSurfaceInput),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var valBox = new TextBox
            {
                Text = visual.Value + "%",
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new SolidColorBrush(ColTextPrimary),
                CaretBrush = new SolidColorBrush(ColPrimary),
                BorderThickness = new Thickness(0),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            visual.ValueInput = valBox;
            BindValueInputEvents(visual);
            inputPill.Child = valBox;
            bottomStack.Children.Add(inputPill);

            if (isMaster)
            {
                // Master bottom metric: Sun SVG icon + "ALL" (no pill background, matching Stitch)
                var masterMetric = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                var sunSmall = CreateVectorIcon(SvgSunSmall, ColPrimaryContainer, 12, 12);
                sunSmall.Margin = new Thickness(0, 0, 4, 0);
                var allLabel = new TextBlock
                {
                    Text = "ALL",
                    FontSize = 9.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(ColTextSecondary),
                    VerticalAlignment = VerticalAlignment.Center
                };
                masterMetric.Children.Add(sunSmall);
                masterMetric.Children.Add(allLabel);
                bottomStack.Children.Add(masterMetric);
            }
            else
            {
                // Monitor bottom metric: rounded pill (#14171C) with dot + temperature/Native
                var metricPill = new Border
                {
                    Background = new SolidColorBrush(ColSurfaceInput),
                    CornerRadius = new CornerRadius(9),
                    Padding = new Thickness(7, 2, 7, 2),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Cursor = Cursors.Hand,
                    ToolTip = "Click to cycle Night Light / Color Filter for this display"
                };

                var metricStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var metricDot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Margin = new Thickness(0, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                visual.MetricDot = metricDot;

                var metricLabel = new TextBlock
                {
                    FontSize = 9.5,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                visual.MetricLabel = metricLabel;

                metricStack.Children.Add(metricDot);
                metricStack.Children.Add(metricLabel);
                metricPill.Child = metricStack;

                metricPill.MouseLeftButtonDown += (s, e) =>
                {
                    CycleMonitorTint(visual.ScreenId);
                    e.Handled = true;
                };

                bottomStack.Children.Add(metricPill);
            }

            Grid.SetRow(bottomStack, 2);
            rootGrid.Children.Add(bottomStack);

            card.PreviewMouseWheel += (s, e) =>
            {
                int delta = e.Delta > 0 ? 2 : -2;
                if (!visual.IsOn && delta > 0)
                {
                    visual.IsOn = true;
                }
                SetChannelValue(visual, Math.Max(0, Math.Min(90, visual.Value + delta)), true);
                e.Handled = true;
            };

            cardContainer.Children.Add(rootGrid);
            card.Child = cardContainer;
            UpdateChannelVisualState(visual, tintHex);
            return visual;
        }

        private void BindCustomSliderEvents(ChannelStripVisual visual)
        {
            bool isDragging = false;

            Func<MouseEventArgs, int> calcPercent = e =>
            {
                double h = Math.Max(1.0, visual.SliderHitArea.ActualHeight - 16.0);
                var pos = e.GetPosition(visual.SliderHitArea);
                double relFromBottom = (visual.SliderHitArea.ActualHeight - 8.0) - pos.Y;
                double ratio = Math.Max(0.0, Math.Min(1.0, relFromBottom / h));
                return (int)Math.Round(ratio * 90.0);
            };

            visual.SliderHitArea.MouseLeftButtonDown += (s, e) =>
            {
                isDragging = true;
                visual.SliderHitArea.CaptureMouse();
                if (!visual.IsOn)
                {
                    visual.IsOn = true;
                }
                SetChannelValue(visual, calcPercent(e), true);
                e.Handled = true;
            };

            visual.SliderHitArea.MouseMove += (s, e) =>
            {
                if (!isDragging) return;
                SetChannelValue(visual, calcPercent(e), true);
            };

            visual.SliderHitArea.MouseLeftButtonUp += (s, e) =>
            {
                if (!isDragging) return;
                isDragging = false;
                visual.SliderHitArea.ReleaseMouseCapture();
            };

            visual.SliderHitArea.SizeChanged += (s, e) =>
            {
                UpdateSliderGeometry(visual);
            };
        }

        private void BindValueInputEvents(ChannelStripVisual visual)
        {
            Action commitInput = () =>
            {
                if (_suppressEvents) return;
                string raw = (visual.ValueInput.Text ?? string.Empty).Replace("%", "").Trim();
                int parsed;
                if (int.TryParse(raw, out parsed))
                {
                    int clamped = Math.Max(0, Math.Min(90, parsed));
                    if (!visual.IsOn && clamped > 0)
                    {
                        visual.IsOn = true;
                    }
                    SetChannelValue(visual, clamped, true);
                }
                else
                {
                    visual.ValueInput.Text = visual.Value + "%";
                }
            };

            visual.ValueInput.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    commitInput();
                    Keyboard.ClearFocus();
                    e.Handled = true;
                }
            };

            visual.ValueInput.LostFocus += (s, e) => commitInput();
        }

        private void UpdateSliderGeometry(ChannelStripVisual visual)
        {
            if (visual.SliderHitArea == null) return;
            double usableHeight = Math.Max(0.0, visual.SliderHitArea.ActualHeight - 16.0);
            double ratio = Math.Max(0.0, Math.Min(1.0, visual.Value / 90.0));
            double fillPx = usableHeight * ratio;

            visual.TrackFill.Height = fillPx;
            visual.ThumbKnob.Margin = new Thickness(0, 0, 0, fillPx);
        }

        private void UpdateChannelVisualState(ChannelStripVisual visual, string tintHex)
        {
            bool on = visual.IsOn;

            if (visual.IsMaster)
            {
                // Master badge is bright blue (#4D8EFF) when linked, dark (#14171C) when unlinked
                visual.BadgeBox.Background = new SolidColorBrush(on ? ColPrimaryContainer : ColSurfaceInput);
                visual.BadgeText.Foreground = new SolidColorBrush(on ? ColOnPrimary : ColTextSecondary);
                visual.TopRightIconSlot.Content = CreateVectorIcon(SvgLink, on ? ColPrimary : ColTextSecondary, 14, 14);
                visual.StatusDot.Fill = new SolidColorBrush(on ? ColPrimary : ColOutlineVariant);
                visual.StatusSubtitle.Text = on ? "Linked All" : "Unlinked";
                visual.StatusSubtitle.Foreground = new SolidColorBrush(on ? ColPrimary : ColTextSecondary);
            }
            else
            {
                // Monitor badge is dark (#14171C) with crisp number (matching Stitch)
                visual.BadgeBox.Background = new SolidColorBrush(ColSurfaceInput);
                visual.BadgeText.Foreground = new SolidColorBrush(on ? ColTextPrimary : ColTextSecondary);

                string code = string.IsNullOrEmpty(tintHex) ? "000000" : tintHex.ToUpperInvariant();
                bool isWarm = code != "000000";
                visual.TopRightIconSlot.Content = isWarm
                    ? CreateVectorIcon(SvgNightlight, ColSecondary, 13, 13)
                    : CreateVectorIcon(SvgDesktop, ColTextSecondary, 13, 13);

                int idx = Array.IndexOf(TintCodes, code);
                if (visual.MetricDot != null && visual.MetricLabel != null)
                {
                    visual.MetricDot.Fill = new SolidColorBrush(isWarm ? ColSecondary : ColPrimaryContainer);
                    visual.MetricLabel.Text = idx >= 0 ? TintBadges[idx] : "Custom";
                    visual.MetricLabel.Foreground = new SolidColorBrush(isWarm ? ColSecondary : ColTextSecondary);
                }
            }

            visual.TrackFill.Opacity = on ? 1.0 : 0.35;
            visual.ThumbKnob.Opacity = on ? 1.0 : 0.65;
            visual.ValueInput.Opacity = on ? 1.0 : 0.65;
            visual.ValueInput.Text = visual.Value + "%";

            UpdateSliderGeometry(visual);
        }

        private void ToggleChannelPower(ChannelStripVisual visual)
        {
            visual.IsOn = !visual.IsOn;

            if (visual.IsMaster)
            {
                _settings.Master.On = visual.IsOn;
                UpdateChannelVisualState(visual, "000000");

                foreach (var m in _monitorStrips)
                {
                    m.IsOn = visual.IsOn;
                    var cfg = _settings.Screens.FirstOrDefault(x => x.Id == m.ScreenId);
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
                _overlayManager.RefreshAllOverlays();
            }
            else
            {
                var cfg = _settings.Screens.FirstOrDefault(x => x.Id == visual.ScreenId);
                if (cfg != null)
                {
                    cfg.On = visual.IsOn;
                    UpdateChannelVisualState(visual, cfg.Tint);
                }

                _masterStrip.IsOn = false;
                _settings.Master.On = false;
                UpdateChannelVisualState(_masterStrip, "000000");

                _overlayManager.RefreshOverlay(visual.ScreenId);
            }

            _settingsService.Save(_settings);
        }

        private void SetChannelValue(ChannelStripVisual visual, int newVal, bool fromUser)
        {
            int clamped = Math.Max(0, Math.Min(90, newVal));
            int delta = clamped - visual.Value;
            visual.Value = clamped;

            if (visual.IsMaster)
            {
                _settings.Master.On = visual.IsOn;
                _settings.Master.Dim = clamped;
                UpdateChannelVisualState(visual, "000000");

                foreach (var m in _monitorStrips)
                {
                    m.IsOn = visual.IsOn;
                    var cfg = _settings.Screens.FirstOrDefault(x => x.Id == m.ScreenId);
                    if (cfg == null) continue;

                    cfg.On = visual.IsOn;
                    int targetVal = _settings.Master.Proportional
                        ? Math.Max(0, Math.Min(90, cfg.Dim + delta))
                        : clamped;

                    cfg.Dim = targetVal;
                    m.Value = targetVal;
                    UpdateChannelVisualState(m, cfg.Tint);
                }
                _overlayManager.RefreshAllOverlays();
            }
            else
            {
                var cfg = _settings.Screens.FirstOrDefault(x => x.Id == visual.ScreenId);
                if (cfg != null)
                {
                    cfg.On = visual.IsOn;
                    cfg.Dim = clamped;
                    UpdateChannelVisualState(visual, cfg.Tint);
                }

                if (fromUser)
                {
                    _masterStrip.IsOn = false;
                    _settings.Master.On = false;
                    UpdateChannelVisualState(_masterStrip, "000000");
                }

                _overlayManager.RefreshOverlay(visual.ScreenId);
            }

            _settingsService.Save(_settings);
        }

        private void CycleMonitorTint(int screenId)
        {
            var cfg = _settings.Screens.FirstOrDefault(x => x.Id == screenId);
            var strip = _monitorStrips.FirstOrDefault(x => x.ScreenId == screenId);
            if (cfg == null || strip == null) return;

            string cur = string.IsNullOrEmpty(cfg.Tint) ? "000000" : cfg.Tint.ToUpperInvariant();
            int idx = Array.IndexOf(TintCodes, cur);
            int nextIdx = (idx + 1) % TintCodes.Length;

            cfg.Tint = TintCodes[nextIdx];
            UpdateChannelVisualState(strip, cfg.Tint);
            UpdateNightSyncDotVisual();
            _overlayManager.RefreshOverlay(screenId);
            _settingsService.Save(_settings);
        }

        private void OnNightSyncClicked()
        {
            bool anyWarm = _settings.Screens.Any(s => !string.IsNullOrEmpty(s.Tint) && s.Tint != "000000");
            string targetTint = anyWarm ? "000000" : "2B1300";

            foreach (var cfg in _settings.Screens)
            {
                cfg.Tint = targetTint;
            }

            foreach (var strip in _monitorStrips)
            {
                UpdateChannelVisualState(strip, targetTint);
            }

            UpdateNightSyncDotVisual();
            _overlayManager.RefreshAllOverlays();
            _settingsService.Save(_settings);
        }

        private void UpdateNightSyncDotVisual()
        {
            if (_nightSyncDot == null) return;
            bool anyWarm = _settings.Screens.Any(s => !string.IsNullOrEmpty(s.Tint) && s.Tint != "000000");
            _nightSyncDot.Fill = new SolidColorBrush(anyWarm ? ColSecondary : ColTextSecondary);
        }

        private void ApplyQuickPreset(int presetDim)
        {
            bool turnOn = presetDim > 0;
            _masterStrip.IsOn = turnOn;
            SetChannelValue(_masterStrip, presetDim, true);

            string targetLabel = presetDim == 0 ? "Off" : (presetDim + "%");
            foreach (var btn in _presetButtons)
            {
                bool match = (btn.Content as string) == targetLabel;
                StyleNavPillButton(btn, match);
                if (match)
                {
                    btn.Foreground = new SolidColorBrush(ColPrimary);
                }
            }
        }

        // =====================================================================
        // SETTINGS VIEW (Exact Stitch Layout from Screenshot 2)
        // =====================================================================
        private ScrollViewer BuildSettingsView()
        {
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Visibility = Visibility.Collapsed
            };

            var rootStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 4, 4)
            };

            // Card 1: Color Filter (Night Light / Tint)
            var tintCard = new Border
            {
                Background = new SolidColorBrush(ColSurfaceCard),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var tintCardStack = new StackPanel { Orientation = Orientation.Vertical };

            var tintHeaderRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            var tintHeaderIcon = CreateVectorIcon(SvgSunSmall, ColSecondary, 14, 14);
            tintHeaderIcon.Margin = new Thickness(0, 0, 6, 0);
            tintHeaderRow.Children.Add(tintHeaderIcon);
            tintHeaderRow.Children.Add(new TextBlock
            {
                Text = "Color Filter (Night Light / Tint)",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            });
            tintCardStack.Children.Add(tintHeaderRow);

            tintCardStack.Children.Add(new TextBlock
            {
                Text = "Reduce eye strain at night by adding a warm tint to the dimming filter.",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(ColTextSecondary),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 8)
            });

            _tintSettingsHost = new StackPanel { Orientation = Orientation.Vertical };
            tintCardStack.Children.Add(_tintSettingsHost);
            tintCard.Child = tintCardStack;
            rootStack.Children.Add(tintCard);

            // Card 2: Behavior & Display
            var behaviorCard = new Border
            {
                Background = new SolidColorBrush(ColSurfaceCard),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var behaviorStack = new StackPanel { Orientation = Orientation.Vertical };

            var behaviorHeader = new DockPanel
            {
                LastChildFill = false,
                Margin = new Thickness(0, 0, 0, 6)
            };

            var bLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var bIcon = CreateVectorIcon(SvgSliders, ColPrimary, 14, 14);
            bIcon.Margin = new Thickness(0, 0, 6, 0);
            bLeft.Children.Add(bIcon);
            bLeft.Children.Add(new TextBlock
            {
                Text = "Behavior & Display",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            });
            DockPanel.SetDock(bLeft, Dock.Left);
            behaviorHeader.Children.Add(bLeft);

            _configuredCountText = new TextBlock
            {
                Text = "11 CONFIGURED",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColTextSecondary),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(_configuredCountText, Dock.Right);
            behaviorHeader.Children.Add(_configuredCountText);

            behaviorStack.Children.Add(behaviorHeader);

            behaviorStack.Children.Add(CreateSwitchRow(
                "Show screen numbers",
                "Display visual channel badges (1, 2)",
                _settings.ShowIds,
                true,
                val => { _settings.ShowIds = val; UpdateConfiguredCounter(); _overlayManager.RefreshAllOverlays(); _settingsService.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Show screen borders",
                "Accent outline around controlled monitors",
                _settings.ShowBoundaries,
                true,
                val => { _settings.ShowBoundaries = val; UpdateConfiguredCounter(); _overlayManager.RefreshAllOverlays(); _settingsService.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Proportional Master mode",
                "Preserve relative brightness offset",
                _settings.Master.Proportional,
                true,
                val => { _settings.Master.Proportional = val; UpdateConfiguredCounter(); _settingsService.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Dim Start Menu & Cursor",
                "Applies global hardware gamma ramp",
                _settings.UseHardwareGamma,
                true,
                val =>
                {
                    _settings.UseHardwareGamma = val;
                    UpdateFooterStatus();
                    UpdateConfiguredCounter();
                    _overlayManager.RefreshAllOverlays();
                    _settingsService.Save(_settings);
                }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Fix Auto-Hide Taskbar edge",
                "Leaves 1px trigger strip for auto-hidden taskbars",
                _settings.AvoidTaskbarAutoHideLock,
                true,
                val => { _settings.AvoidTaskbarAutoHideLock = val; UpdateConfiguredCounter(); _overlayManager.RefreshAllOverlays(); _settingsService.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Keep panel undimmed",
                "Keeps OpenDimmer above the dimming overlay",
                _settings.NonDimmableWindow,
                true,
                val => { _settings.NonDimmableWindow = val; Topmost = val; UpdateConfiguredCounter(); _overlayManager.EnforceAllTopmost(); _settingsService.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Movable control window",
                "Allows dragging the window by its header",
                _settings.MovableWindow,
                true,
                val => { _settings.MovableWindow = val; UpdateConfiguredCounter(); _settingsService.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Close button (✕) hides to tray",
                "Keeps dimming active in background when closed",
                _settings.CloseHidesToTray,
                true,
                val => { _settings.CloseHidesToTray = val; UpdateConfiguredCounter(); _settingsService.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Show in taskbar",
                "Displays OpenDimmer in the Windows taskbar",
                _settings.ShowInTaskbar,
                true,
                val => { _settings.ShowInTaskbar = val; ShowInTaskbar = val; UpdateConfiguredCounter(); _settingsService.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Start minimized in tray",
                "Launches silently in the system tray",
                _settings.StartMinimised,
                true,
                val => { _settings.StartMinimised = val; UpdateConfiguredCounter(); _settingsService.Save(_settings); }));

            behaviorStack.Children.Add(CreateSwitchRow(
                "Launch at Windows startup",
                "Automatically starts OpenDimmer after user login",
                _settings.AutoRun,
                false,
                val => { _settings.AutoRun = val; AutoRunService.SetAutoRun(val); UpdateConfiguredCounter(); _settingsService.Save(_settings); }));

            // Action Buttons Footer inside Settings
            var actionGrid = new UniformGrid
            {
                Columns = 2,
                Margin = new Thickness(0, 10, 0, 2)
            };

            var btnReset = CreateActionButton("Reset Window", "Restore default size and bottom-right position");
            btnReset.Margin = new Thickness(0, 0, 4, 0);
            btnReset.Click += (s, e) => ResetPositionToCorner();

            var btnDiag = CreateActionButton("Copy Diagnostics", "Copy system, monitor, and JSON config to clipboard");
            btnDiag.Margin = new Thickness(4, 0, 0, 0);
            btnDiag.Click += (s, e) =>
            {
                CopyDiagnosticsToClipboard();
                btnDiag.Content = "Copied!";
            };

            actionGrid.Children.Add(btnReset);
            actionGrid.Children.Add(btnDiag);
            behaviorStack.Children.Add(actionGrid);

            behaviorCard.Child = behaviorStack;
            rootStack.Children.Add(behaviorCard);

            scroll.Content = rootStack;
            return scroll;
        }

        private void UpdateConfiguredCounter()
        {
            if (_configuredCountText == null) return;
            _configuredCountText.Text = "11 CONFIGURED";
        }

        private void RebuildTintSettingsRows()
        {
            if (_tintSettingsHost == null) return;
            _tintSettingsHost.Children.Clear();

            var screens = Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                int screenId = i + 1;
                var screen = screens[i];
                var cfg = _settings.Screens.FirstOrDefault(x => x.Id == screenId);
                if (cfg == null) continue;

                if (cfg.TintIntensity <= 0) cfg.TintIntensity = 60;
                var hwInfo = DisplayInfoService.GetMonitorInfo(screen, screenId);

                // Inner dark sub-card (#16191E) matching Stitch Settings screenshot
                var subCard = new Border
                {
                    Background = new SolidColorBrush(ColSurfaceSubCard),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var subStack = new StackPanel { Orientation = Orientation.Vertical };

                // Top Row: [Port Badge] [Monitor Name]   <--->   [● Warm Amber ⌄]
                var topDock = new DockPanel { LastChildFill = false };

                var leftInfo = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var portBadge = new Border
                {
                    Background = new SolidColorBrush(ColSurfaceCard),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(0, 0, 7, 0),
                    Child = new TextBlock
                    {
                        Text = hwInfo.PortBadge,
                        FontSize = 9.5,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(ColTextSecondary)
                    }
                };

                var nameText = new TextBlock
                {
                    Text = hwInfo.FriendlyName,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(ColTextPrimary),
                    VerticalAlignment = VerticalAlignment.Center
                };

                leftInfo.Children.Add(portBadge);
                leftInfo.Children.Add(nameText);
                DockPanel.SetDock(leftInfo, Dock.Left);
                topDock.Children.Add(leftInfo);

                // Right Dropdown Pill Button
                string currentCode = string.IsNullOrEmpty(cfg.Tint) ? "000000" : cfg.Tint.ToUpperInvariant();
                int currentIdx = Math.Max(0, Array.IndexOf(TintCodes, currentCode));
                bool isWarm = currentCode != "000000";

                var dropBorder = new Border
                {
                    Background = new SolidColorBrush(ColSurfaceCard),
                    BorderBrush = new SolidColorBrush(ColBorderSubtle),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 4, 8, 4),
                    Cursor = Cursors.Hand
                };

                var dropStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var dropDot = new Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = new SolidColorBrush(isWarm ? ColSecondary : ColBorderSubtle),
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var dropLabel = new TextBlock
                {
                    Text = TintDropdownLabels[currentIdx],
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(ColTextPrimary),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0)
                };

                var dropChev = CreateVectorIcon(SvgChevronDown, ColTextSecondary, 11, 11);

                dropStack.Children.Add(dropDot);
                dropStack.Children.Add(dropLabel);
                dropStack.Children.Add(dropChev);
                dropBorder.Child = dropStack;

                DockPanel.SetDock(dropBorder, Dock.Right);
                topDock.Children.Add(dropBorder);
                subStack.Children.Add(topDock);

                // Bottom Row: [4500K Intensity] [========------] [60%] (Visible when a warm tint is selected)
                var intensityGrid = new Grid
                {
                    Margin = new Thickness(0, 8, 0, 2),
                    Visibility = isWarm ? Visibility.Visible : Visibility.Collapsed
                };
                intensityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                intensityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                intensityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var intLabelStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                var tempBadgeText = new TextBlock
                {
                    Text = TintBadges[currentIdx] + " ",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(ColSecondary)
                };
                intLabelStack.Children.Add(tempBadgeText);
                intLabelStack.Children.Add(new TextBlock
                {
                    Text = "Intensity",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(ColTextSecondary)
                });
                Grid.SetColumn(intLabelStack, 0);
                intensityGrid.Children.Add(intLabelStack);

                var barHit = new Grid
                {
                    Height = 14,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Cursor = Cursors.Hand
                };

                var barTrack = new Border
                {
                    Height = 5,
                    CornerRadius = new CornerRadius(2.5),
                    Background = new SolidColorBrush(ColSurfaceTrack),
                    VerticalAlignment = VerticalAlignment.Center,
                    ClipToBounds = true
                };

                var barFill = new Border
                {
                    Height = 5,
                    CornerRadius = new CornerRadius(2.5),
                    Background = new SolidColorBrush(ColSecondary),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 0
                };
                barTrack.Child = barFill;
                barHit.Children.Add(barTrack);

                Grid.SetColumn(barHit, 1);
                intensityGrid.Children.Add(barHit);

                var pctText = new TextBlock
                {
                    Text = cfg.TintIntensity + "%",
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(ColTextPrimary),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(pctText, 2);
                intensityGrid.Children.Add(pctText);

                Action updateBarWidth = () =>
                {
                    double w = barHit.ActualWidth;
                    if (w > 0)
                    {
                        barFill.Width = w * (Math.Max(10, Math.Min(100, cfg.TintIntensity)) / 100.0);
                    }
                };
                barHit.SizeChanged += (s, e) => updateBarWidth();

                int capturedScreenId = screenId;
                bool draggingInt = false;
                Action<MouseEventArgs> setIntensityFromMouse = e =>
                {
                    double w = Math.Max(1.0, barHit.ActualWidth);
                    double x = e.GetPosition(barHit).X;
                    int pct = (int)Math.Round(Math.Max(10.0, Math.Min(100.0, (x / w) * 100.0)));
                    cfg.TintIntensity = pct;
                    pctText.Text = pct + "%";
                    updateBarWidth();
                    _overlayManager.RefreshOverlay(capturedScreenId);
                    _settingsService.Save(_settings);
                };

                barHit.MouseLeftButtonDown += (s, e) =>
                {
                    draggingInt = true;
                    barHit.CaptureMouse();
                    setIntensityFromMouse(e);
                    e.Handled = true;
                };
                barHit.MouseMove += (s, e) =>
                {
                    if (draggingInt) setIntensityFromMouse(e);
                };
                barHit.MouseLeftButtonUp += (s, e) =>
                {
                    if (draggingInt)
                    {
                        draggingInt = false;
                        barHit.ReleaseMouseCapture();
                    }
                };

                // Dropdown menu on click
                dropBorder.MouseLeftButtonDown += (s, e) =>
                {
                    var menu = new Forms.ContextMenuStrip
                    {
                        Renderer = new DarkTrayMenuRenderer(),
                        ShowImageMargin = false,
                        ShowCheckMargin = false,
                        BackColor = DrawingColor.FromArgb(24, 27, 32),
                        ForeColor = DrawingColor.FromArgb(243, 244, 246)
                    };

                    for (int t = 0; t < TintCodes.Length; t++)
                    {
                        int selIdx = t;
                        menu.Items.Add(TintDropdownLabels[t] + " (" + TintBadges[t] + ")", null, (ms, me) =>
                        {
                            cfg.Tint = TintCodes[selIdx];
                            bool warmNow = cfg.Tint != "000000";
                            dropDot.Fill = new SolidColorBrush(warmNow ? ColSecondary : ColBorderSubtle);
                            dropLabel.Text = TintDropdownLabels[selIdx];
                            tempBadgeText.Text = TintBadges[selIdx] + " ";
                            intensityGrid.Visibility = warmNow ? Visibility.Visible : Visibility.Collapsed;
                            Dispatcher.BeginInvoke(new Action(updateBarWidth), System.Windows.Threading.DispatcherPriority.Loaded);

                            var strip = _monitorStrips.FirstOrDefault(x => x.ScreenId == capturedScreenId);
                            if (strip != null)
                            {
                                UpdateChannelVisualState(strip, cfg.Tint);
                            }
                            UpdateNightSyncDotVisual();
                            _overlayManager.RefreshOverlay(capturedScreenId);
                            _settingsService.Save(_settings);
                        });
                    }

                    var pt = Forms.Control.MousePosition;
                    menu.Show(pt);
                    e.Handled = true;
                };

                subStack.Children.Add(intensityGrid);
                subCard.Child = subStack;
                _tintSettingsHost.Children.Add(subCard);
            }
        }

        private StackPanel CreateSwitchRow(string title, string subtitle, bool initialValue, bool showBottomDivider, Action<bool> onToggled)
        {
            var wrapper = new StackPanel { Orientation = Orientation.Vertical };

            var row = new Grid
            {
                Margin = new Thickness(0, 6, 0, 6),
                Background = System.Windows.Media.Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            textStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                TextWrapping = TextWrapping.Wrap
            });

            textStack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 10,
                Foreground = new SolidColorBrush(ColTextSecondary),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 1, 0, 0)
            });

            // Stitch Toggle Switch (36x20px, #4D8EFF when ON, #36393F when OFF)
            bool state = initialValue;
            var switchTrack = new Border
            {
                Width = 36,
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(3),
                VerticalAlignment = VerticalAlignment.Center
            };

            var switchKnob = new Ellipse
            {
                Width = 14,
                Height = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            switchTrack.Child = switchKnob;

            Action updateSwitchVisual = () =>
            {
                switchTrack.Background = new SolidColorBrush(state ? ColPrimaryContainer : ColSurfaceSwitchOff);
                switchKnob.Fill = new SolidColorBrush(state ? Colors.White : ColTextSecondary);
                switchKnob.HorizontalAlignment = state ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            };
            updateSwitchVisual();

            row.MouseLeftButtonDown += (s, e) =>
            {
                state = !state;
                updateSwitchVisual();
                onToggled(state);
                e.Handled = true;
            };

            Grid.SetColumn(textStack, 0);
            Grid.SetColumn(switchTrack, 1);
            row.Children.Add(textStack);
            row.Children.Add(switchTrack);
            wrapper.Children.Add(row);

            if (showBottomDivider)
            {
                wrapper.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(ColDivider),
                    Margin = new Thickness(0, 1, 0, 1)
                });
            }

            return wrapper;
        }

        private Button CreateActionButton(string label, string tooltip)
        {
            var btn = new Button
            {
                Content = label,
                ToolTip = tooltip,
                Height = 26,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(ColSurfaceSubCard),
                Foreground = new SolidColorBrush(ColTextPrimary),
                BorderBrush = new SolidColorBrush(ColBorderSubtle),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;
            btn.Template = template;

            btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(ColSurfaceHover);
            btn.MouseLeave += (s, e) => btn.Background = new SolidColorBrush(ColSurfaceSubCard);

            return btn;
        }

        private Grid BuildFooterBar()
        {
            var footer = new Grid
            {
                Background = new SolidColorBrush(ColSurfaceCanvas),
                Margin = new Thickness(14, 0, 14, 4)
            };

            var leftStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            leftStack.Children.Add(new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(ColPrimary),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            _footerStatusText = new TextBlock
            {
                FontSize = 9.5,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(ColTextSecondary),
                VerticalAlignment = VerticalAlignment.Center
            };
            UpdateFooterStatus();
            leftStack.Children.Add(_footerStatusText);

            var versionText = new TextBlock
            {
                Text = "v1.0.0",
                FontSize = 9.5,
                Foreground = new SolidColorBrush(ColTextSecondary),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            footer.Children.Add(leftStack);
            footer.Children.Add(versionText);
            return footer;
        }

        private void UpdateFooterStatus()
        {
            if (_footerStatusText == null) return;
            _footerStatusText.Text = _settings.UseHardwareGamma
                ? "HYBRID GAMMA + Z-ORDER ACTIVE"
                : "Z-ORDER GUARD ACTIVE";
        }

        private void SwitchView(string view)
        {
            bool isScreens = view == "screens";
            _screensView.Visibility = isScreens ? Visibility.Visible : Visibility.Collapsed;
            _settingsScroll.Visibility = isScreens ? Visibility.Collapsed : Visibility.Visible;

            StyleNavPillButton(_navBtnScreens, isScreens);
            StyleNavPillButton(_navBtnSettings, !isScreens);
        }

        private void ToggleMaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CopyDiagnosticsToClipboard()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== OpenDimmer Diagnostic Report ===");
                sb.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine(string.Format("OS: [{0}]", Environment.OSVersion));
                sb.AppendLine(string.Format("64-bit: [OS: {0}] [Process: {1}]",
                    Environment.Is64BitOperatingSystem, Environment.Is64BitProcess));
                sb.AppendLine();

                var screens = Forms.Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    var s = screens[i];
                    var hw = DisplayInfoService.GetMonitorInfo(s, i + 1);
                    sb.AppendLine(string.Format("DVC #{0}: [{1}] [{2}] [BPP: {3}] [Primary: {4}]",
                        i + 1, s.DeviceName, hw.FriendlyName, s.BitsPerPixel, s.Primary));
                    sb.AppendLine(string.Format("BOU #{0}: {1}", i + 1, s.Bounds));
                }

                sb.AppendLine();
                sb.AppendLine("=== OpenDimmer.json ===");
                if (File.Exists(_settingsService.FilePath))
                {
                    sb.AppendLine(File.ReadAllText(_settingsService.FilePath, Encoding.UTF8));
                }

                Clipboard.SetText(sb.ToString());
            }
            catch { }
        }

        private Icon CreateBrightnessIcon()
        {
            using (var bmp = new Bitmap(16, 16))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(DrawingColor.Transparent);

                using (var brush = new SolidBrush(DrawingColor.FromArgb(255, 173, 198, 255)))
                {
                    g.FillEllipse(brush, 4, 4, 8, 8);
                }

                using (var pen = new DrawingPen(DrawingColor.FromArgb(255, 173, 198, 255), 1.5f))
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
                BackColor = DrawingColor.FromArgb(24, 27, 32),
                ForeColor = DrawingColor.FromArgb(243, 244, 246)
            };

            menu.Items.Add("Open OpenDimmer", null, (s, e) => ShowAndActivateWindow());
            menu.Items.Add("Toggle Master", null, (s, e) =>
            {
                if (_masterStrip != null)
                {
                    ToggleChannelPower(_masterStrip);
                }
            });
            menu.Items.Add("Night Sync (4500K)", null, (s, e) => OnNightSyncClicked());
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
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
            _overlayManager.EnforceAllTopmost();
        }

        private void RestoreOrResetPosition()
        {
            if (_settings.Position.Width.HasValue && _settings.Position.Width.Value >= MinWidth)
            {
                Width = _settings.Position.Width.Value;
            }
            if (_settings.Position.Height.HasValue && _settings.Position.Height.Value >= MinHeight)
            {
                Height = _settings.Position.Height.Value;
            }

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
            WindowState = WindowState.Normal;
            Width = 386;
            Height = 468;
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
            if (WindowState == WindowState.Normal)
            {
                _settings.Position.Left = Left;
                _settings.Position.Top = Top;
                _settings.Position.Width = Width;
                _settings.Position.Height = Height;
            }
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
