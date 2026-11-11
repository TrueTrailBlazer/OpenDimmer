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
using System.Windows.Shapes;
using System.Windows.Shell;
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
        public override DrawingColor MenuBorder { get { return DrawingColor.FromArgb(45, 51, 59); } }
        public override DrawingColor MenuItemBorder { get { return DrawingColor.FromArgb(77, 142, 255); } }
        public override DrawingColor MenuItemSelected { get { return DrawingColor.FromArgb(39, 42, 47); } }
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
                : DrawingColor.FromArgb(156, 163, 175);
            base.OnRenderItemText(e);
        }
    }

    public sealed class MainWindow : Window
    {
        // Stitch Design Tokens
        private static readonly MediaColor ColSurfaceCanvas = MediaColor.FromRgb(24, 27, 32);       // #181B20
        private static readonly MediaColor ColSurfaceCard = MediaColor.FromRgb(34, 39, 46);         // #22272E
        private static readonly MediaColor ColSurfaceMaster = MediaColor.FromRgb(31, 41, 55);       // #1F2937
        private static readonly MediaColor ColSurfaceInput = MediaColor.FromRgb(20, 23, 28);        // #14171C
        private static readonly MediaColor ColSurfaceTrack = MediaColor.FromRgb(17, 20, 24);        // #111418
        private static readonly MediaColor ColSurfaceHover = MediaColor.FromRgb(39, 42, 47);        // #272A2F
        private static readonly MediaColor ColBorderSubtle = MediaColor.FromRgb(45, 51, 59);        // #2D333B
        private static readonly MediaColor ColBorderMaster = MediaColor.FromArgb(90, 59, 130, 246); // #3B82F659
        private static readonly MediaColor ColPrimary = MediaColor.FromRgb(173, 198, 255);          // #ADC6FF
        private static readonly MediaColor ColPrimaryContainer = MediaColor.FromRgb(77, 142, 255);  // #4D8EFF
        private static readonly MediaColor ColOnPrimary = MediaColor.FromRgb(0, 40, 93);            // #00285D
        private static readonly MediaColor ColSecondary = MediaColor.FromRgb(255, 185, 95);         // #FFB95F
        private static readonly MediaColor ColTextPrimary = MediaColor.FromRgb(243, 244, 246);      // #F3F4F6
        private static readonly MediaColor ColTextSecondary = MediaColor.FromRgb(156, 163, 175);    // #9CA3AF
        private static readonly MediaColor ColOutlineVariant = MediaColor.FromRgb(66, 71, 84);      // #424754
        private static readonly MediaColor ColErrorContainer = MediaColor.FromRgb(147, 0, 10);      // #93000A

        private static readonly string[] TintCodes = new[] { "000000", "2B1300", "330D00", "1F190D" };
        private static readonly string[] TintBadges = new[] { "Native", "4500K", "3500K", "Sepia" };
        private static readonly string[] TintFullNames = new[]
        {
            "Native (Pure Brightness)",
            "4500K Warm Amber (Night Light)",
            "3500K Deep Ember (Low Blue Light)",
            "Soft Sepia (Reading Mode)"
        };

        private readonly AppSettings _settings;
        private readonly SettingsService _settingsService;
        private readonly OverlayManager _overlayManager;
        private Forms.NotifyIcon _notifyIcon;
        private IntPtr _trayIconHandle;

        private Grid _screensView;
        private UniformGrid _channelsGrid;
        private ScrollViewer _settingsScroll;
        private StackPanel _tintSettingsHost;
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
            Background = new SolidColorBrush(ColSurfaceCanvas);
            BorderBrush = new SolidColorBrush(ColBorderSubtle);
            BorderThickness = new Thickness(1);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");

            MinWidth = 320;
            MinHeight = 380;
            Width = 376;
            Height = 456;

            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(6),
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0)
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

        private void InstallCustomScrollbarStyle()
        {
            const string scrollBarXaml = @"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                       TargetType='{x:Type ScrollBar}'>
                    <Setter Property='Width' Value='6'/>
                    <Setter Property='MinWidth' Value='6'/>
                    <Setter Property='Background' Value='Transparent'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='{x:Type ScrollBar}'>
                                <Track Name='PART_Track' IsDirectionReversed='True'>
                                    <Track.Thumb>
                                        <Thumb>
                                            <Thumb.Template>
                                                <ControlTemplate TargetType='{x:Type Thumb}'>
                                                    <Border Background='#374151' CornerRadius='3' Margin='1,2,1,2'/>
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
            var outerGrid = new Grid();
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });

            // Row 0: Stitch Header
            var header = BuildHeaderBar();
            Grid.SetRow(header, 0);
            outerGrid.Children.Add(header);

            // Row 1: Main Content Area
            var mainContent = new Grid { Margin = new Thickness(10, 2, 10, 4) };
            _screensView = BuildScreensView();
            _settingsScroll = BuildSettingsView();

            mainContent.Children.Add(_screensView);
            mainContent.Children.Add(_settingsScroll);

            Grid.SetRow(mainContent, 1);
            outerGrid.Children.Add(mainContent);

            // Row 2: Stitch Footer
            var footer = BuildFooterBar();
            Grid.SetRow(footer, 2);
            outerGrid.Children.Add(footer);

            Content = outerGrid;

            RebuildChannelStrips();
            RebuildTintSettingsRows();
            SwitchView("screens");
        }

        private Grid BuildHeaderBar()
        {
            var header = new Grid
            {
                Background = new SolidColorBrush(ColSurfaceCanvas),
                Margin = new Thickness(10, 0, 8, 0)
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

            // Left: Icon + Title
            var brandStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var sunIcon = new TextBlock
            {
                Text = "☀",
                FontSize = 14,
                Foreground = new SolidColorBrush(ColPrimary),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -1, 6, 0)
            };

            var titleText = new TextBlock
            {
                Text = "OpenDimmer",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColTextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            };

            brandStack.Children.Add(sunIcon);
            brandStack.Children.Add(titleText);
            Grid.SetColumn(brandStack, 0);
            header.Children.Add(brandStack);

            // Center: Segmented Nav Pill (Screens | Settings)
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
                FontSize = 11,
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

            // Bottom Utility & Quick Preset Strip
            var bottomBar = new Border
            {
                Background = new SolidColorBrush(ColSurfaceCard),
                BorderBrush = new SolidColorBrush(ColBorderSubtle),
                BorderThickness = new Thickness(1),
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
                Foreground = new SolidColorBrush(ColTextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            };
            nightStack.Children.Add(dotHolder);
            nightStack.Children.Add(nightText);
            nightBtn.Content = nightStack;
            nightBtn.Click += (s, e) => OnNightSyncClicked();

            DockPanel.SetDock(nightBtn, Dock.Left);
            bottomDock.Children.Add(nightBtn);

            // Right: Quick Dimming Presets (0%, 25%, 50%, 75%)
            var presetsPill = new Border
            {
                Background = new SolidColorBrush(ColSurfaceInput),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(2)
            };
            var presetsStack = new StackPanel { Orientation = Orientation.Horizontal };
            _presetButtons.Clear();

            int[] presets = new[] { 0, 25, 50, 75 };
            foreach (int p in presets)
            {
                int capturedPreset = p;
                var pBtn = CreateNavPillButton(p + "%", false);
                pBtn.FontSize = 10;
                pBtn.Padding = new Thickness(7, 2, 7, 2);
                pBtn.ToolTip = "Set dimming level to " + p + "%";
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
                Padding = new Thickness(4, 2, 6, 2)
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
                _settings.Master.On ? "Linked All" : "Off",
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

                string shortTitle = screen.Primary ? ("Display " + id + " ★") : ("Display " + id);
                string sub = screen.Bounds.Width + "×" + screen.Bounds.Height;

                var strip = CreateChannelStrip(
                    false,
                    id,
                    shortTitle,
                    sub,
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

            var card = new Border
            {
                Background = new SolidColorBrush(isMaster ? ColSurfaceMaster : ColSurfaceCard),
                BorderBrush = new SolidColorBrush(isMaster ? ColBorderMaster : ColBorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(3, 0, 3, 0)
            };
            visual.CardBorder = card;

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 1. Top Header (Badge + Toggle Power + Title + Subtitle)
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
                ToolTip = isMaster ? "Click to toggle Master dimming on all screens" : ("Click to toggle Display #" + screenId)
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

            var statusIcon = new TextBlock
            {
                Text = isMaster ? "⛓" : "▣",
                FontSize = 12,
                Foreground = new SolidColorBrush(isOn ? ColPrimary : ColTextSecondary),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(statusIcon, Dock.Right);
            topRow.Children.Add(statusIcon);

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
                Width = 5,
                Height = 5,
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
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

            // 2. Center Tactile Vertical Slider
            var sliderArea = new Grid
            {
                Margin = new Thickness(0, 8, 0, 8),
                MinHeight = 135
            };

            // Left scale ticks
            var ticksGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(2, 6, 0, 6),
                IsHitTestVisible = false
            };
            string[] tickLabels = isMaster ? new[] { "90", "70", "45", "20", "0" } : new[] { "-", "-", "-", "-", "-" };
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

            // Interactive Slider Hit Box
            var sliderHit = new Grid
            {
                Width = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
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
                BorderBrush = new SolidColorBrush(MediaColor.FromArgb(60, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
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

            // 3. Bottom Numeric Pill & Warmth/Metric Pill
            var bottomStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var inputPill = new Border
            {
                Width = 46,
                Height = 24,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(ColSurfaceInput),
                BorderBrush = new SolidColorBrush(ColBorderSubtle),
                BorderThickness = new Thickness(1),
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

            // Warmth / Status Badge
            var metricPill = new Border
            {
                Background = new SolidColorBrush(ColSurfaceInput),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(7, 2, 7, 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                Cursor = isMaster ? Cursors.Arrow : Cursors.Hand,
                ToolTip = isMaster ? "Controls all displays" : "Click to cycle Night Light / Color Filter for this display"
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

            if (!isMaster)
            {
                metricPill.MouseLeftButtonDown += (s, e) =>
                {
                    CycleMonitorTint(visual.ScreenId);
                    e.Handled = true;
                };
            }

            bottomStack.Children.Add(metricPill);

            Grid.SetRow(bottomStack, 2);
            rootGrid.Children.Add(bottomStack);

            // Mouse wheel anywhere on card
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

            card.Child = rootGrid;
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

            visual.BadgeBox.Background = new SolidColorBrush(on ? ColPrimaryContainer : ColSurfaceInput);
            visual.BadgeText.Foreground = new SolidColorBrush(on ? ColOnPrimary : ColTextSecondary);

            visual.StatusDot.Fill = new SolidColorBrush(on ? ColPrimary : ColOutlineVariant);
            if (visual.IsMaster)
            {
                visual.StatusSubtitle.Text = on ? "Linked All" : "Unlinked";
                visual.StatusSubtitle.Foreground = new SolidColorBrush(on ? ColPrimary : ColTextSecondary);
            }

            visual.TrackFill.Opacity = on ? 1.0 : 0.35;
            visual.ThumbKnob.Opacity = on ? 1.0 : 0.55;
            visual.ValueInput.Opacity = on ? 1.0 : 0.6;
            visual.ValueInput.Text = visual.Value + "%";

            if (visual.IsMaster)
            {
                visual.MetricDot.Fill = new SolidColorBrush(ColPrimary);
                visual.MetricLabel.Text = "ALL";
                visual.MetricLabel.Foreground = new SolidColorBrush(ColTextSecondary);
            }
            else
            {
                string code = string.IsNullOrEmpty(tintHex) ? "000000" : tintHex.ToUpperInvariant();
                int idx = Array.IndexOf(TintCodes, code);
                bool isWarm = code != "000000";
                visual.MetricDot.Fill = new SolidColorBrush(isWarm ? ColSecondary : ColPrimaryContainer);
                visual.MetricLabel.Text = idx >= 0 ? TintBadges[idx] : "Custom";
                visual.MetricLabel.Foreground = new SolidColorBrush(isWarm ? ColSecondary : ColTextSecondary);
            }

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

                // Toggling an individual monitor disengages Master lock
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
                    // Adjusting an individual screen disengages Master lock
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

            foreach (var btn in _presetButtons)
            {
                bool match = (btn.Content as string) == (presetDim + "%");
                StyleNavPillButton(btn, match);
            }
        }

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

            // Group 1: Color Filter & Night Light
            var tintCard = CreateSettingsCard(
                "Color Filter & Night Light",
                "Select a warm color temperature per display to reduce blue light at night.");
            _tintSettingsHost = new StackPanel { Orientation = Orientation.Vertical };
            ((StackPanel)tintCard.Child).Children.Add(_tintSettingsHost);
            rootStack.Children.Add(tintCard);

            // Group 2: Display & Dimming Engine
            var engineCard = CreateSettingsCard(
                "Dimming Engine & Multi-Monitor",
                "Configure overlays, hardware LUT gamma, and multi-monitor behavior.");
            var engineStack = (StackPanel)engineCard.Child;

            engineStack.Children.Add(CreateSwitchRow(
                "Show screen identity numbers",
                "Displays large monitor ID (1, 2) on each screen",
                _settings.ShowIds,
                val => { _settings.ShowIds = val; _overlayManager.RefreshAllOverlays(); _settingsService.Save(_settings); }));

            engineStack.Children.Add(CreateSwitchRow(
                "Show screen boundaries",
                "Draws a highlight border around each monitor",
                _settings.ShowBoundaries,
                val => { _settings.ShowBoundaries = val; _overlayManager.RefreshAllOverlays(); _settingsService.Save(_settings); }));

            engineStack.Children.Add(CreateSwitchRow(
                "Proportional Master mode",
                "Preserves brightness difference between displays",
                _settings.Master.Proportional,
                val => { _settings.Master.Proportional = val; _settingsService.Save(_settings); }));

            engineStack.Children.Add(CreateSwitchRow(
                "Hardware Gamma (Start Menu & Cursor)",
                "Also dims Start Menu, Taskbar, and mouse cursor",
                _settings.UseHardwareGamma,
                val =>
                {
                    _settings.UseHardwareGamma = val;
                    UpdateFooterStatus();
                    _overlayManager.RefreshAllOverlays();
                    _settingsService.Save(_settings);
                }));

            engineStack.Children.Add(CreateSwitchRow(
                "Fix Auto-Hide Taskbar edge",
                "Leaves 1px trigger strip for auto-hidden taskbars",
                _settings.AvoidTaskbarAutoHideLock,
                val => { _settings.AvoidTaskbarAutoHideLock = val; _overlayManager.RefreshAllOverlays(); _settingsService.Save(_settings); }));

            rootStack.Children.Add(engineCard);

            // Group 3: Window & System Startup
            var appCard = CreateSettingsCard(
                "Application & Startup",
                "Window behavior, system tray integration, and startup options.");
            var appStack = (StackPanel)appCard.Child;

            appStack.Children.Add(CreateSwitchRow(
                "Keep control panel undimmed",
                "Keeps OpenDimmer above the dimming overlay",
                _settings.NonDimmableWindow,
                val => { _settings.NonDimmableWindow = val; Topmost = val; _overlayManager.EnforceAllTopmost(); _settingsService.Save(_settings); }));

            appStack.Children.Add(CreateSwitchRow(
                "Movable control window",
                "Allows dragging the window by its header",
                _settings.MovableWindow,
                val => { _settings.MovableWindow = val; _settingsService.Save(_settings); }));

            appStack.Children.Add(CreateSwitchRow(
                "Close button (✕) hides to tray",
                "Keeps dimming active in background when closed",
                _settings.CloseHidesToTray,
                val => { _settings.CloseHidesToTray = val; _settingsService.Save(_settings); }));

            appStack.Children.Add(CreateSwitchRow(
                "Show in taskbar",
                "Displays OpenDimmer in the Windows taskbar",
                _settings.ShowInTaskbar,
                val => { _settings.ShowInTaskbar = val; ShowInTaskbar = val; _settingsService.Save(_settings); }));

            appStack.Children.Add(CreateSwitchRow(
                "Start minimized in system tray",
                "Launches silently in the background",
                _settings.StartMinimised,
                val => { _settings.StartMinimised = val; _settingsService.Save(_settings); }));

            appStack.Children.Add(CreateSwitchRow(
                "Launch at Windows login",
                "Automatically starts OpenDimmer with Windows",
                _settings.AutoRun,
                val => { _settings.AutoRun = val; AutoRunService.SetAutoRun(val); _settingsService.Save(_settings); }));

            // Action Buttons (Reset Window + Copy Diagnostics)
            var actionGrid = new UniformGrid
            {
                Columns = 2,
                Margin = new Thickness(0, 8, 0, 2)
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
            appStack.Children.Add(actionGrid);

            var repoLink = CreateActionButton("GitHub Repository (TrueTrailBlazer/OpenDimmer)", "Open project page in browser");
            repoLink.Margin = new Thickness(0, 6, 0, 0);
            repoLink.Click += (s, e) =>
            {
                try { Process.Start("https://github.com/TrueTrailBlazer/OpenDimmer"); } catch { }
            };
            appStack.Children.Add(repoLink);

            rootStack.Children.Add(appCard);

            scroll.Content = rootStack;
            return scroll;
        }

        private Border CreateSettingsCard(string title, string subtitle)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(ColSurfaceCard),
                BorderBrush = new SolidColorBrush(ColBorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColTextPrimary)
            });

            stack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(ColTextSecondary),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 6)
            });

            card.Child = stack;
            return card;
        }

        private void RebuildTintSettingsRows()
        {
            if (_tintSettingsHost == null) return;
            _tintSettingsHost.Children.Clear();

            var screens = Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                int screenId = i + 1;
                var cfg = _settings.Screens.FirstOrDefault(x => x.Id == screenId);
                if (cfg == null) continue;

                var rowStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 4, 0, 4)
                };

                rowStack.Children.Add(new TextBlock
                {
                    Text = "Display #" + screenId + " (" + cfg.DeviceName + ")",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(ColTextPrimary),
                    Margin = new Thickness(0, 0, 0, 4)
                });

                var pillGrid = new UniformGrid { Columns = 4 };
                var rowButtons = new List<Button>();
                string currentCode = string.IsNullOrEmpty(cfg.Tint) ? "000000" : cfg.Tint.ToUpperInvariant();

                for (int t = 0; t < TintCodes.Length; t++)
                {
                    int capturedTintIdx = t;
                    int capturedScreenId = screenId;
                    bool isActive = TintCodes[t] == currentCode;

                    var btn = CreateNavPillButton(TintBadges[t], isActive);
                    btn.ToolTip = TintFullNames[t];
                    btn.Background = new SolidColorBrush(isActive ? ColPrimaryContainer : ColSurfaceInput);
                    btn.Foreground = new SolidColorBrush(isActive ? ColOnPrimary : ColTextSecondary);

                    rowButtons.Add(btn);
                    btn.Click += (s, e) =>
                    {
                        var targetCfg = _settings.Screens.FirstOrDefault(x => x.Id == capturedScreenId);
                        if (targetCfg != null)
                        {
                            targetCfg.Tint = TintCodes[capturedTintIdx];
                            for (int b = 0; b < rowButtons.Count; b++)
                            {
                                bool sel = b == capturedTintIdx;
                                rowButtons[b].Background = new SolidColorBrush(sel ? ColPrimaryContainer : ColSurfaceInput);
                                rowButtons[b].Foreground = new SolidColorBrush(sel ? ColOnPrimary : ColTextSecondary);
                            }

                            var strip = _monitorStrips.FirstOrDefault(x => x.ScreenId == capturedScreenId);
                            if (strip != null)
                            {
                                UpdateChannelVisualState(strip, targetCfg.Tint);
                            }
                            UpdateNightSyncDotVisual();
                            _overlayManager.RefreshOverlay(capturedScreenId);
                            _settingsService.Save(_settings);
                        }
                    };

                    pillGrid.Children.Add(btn);
                }

                rowStack.Children.Add(pillGrid);
                _tintSettingsHost.Children.Add(rowStack);
            }
        }

        private Grid CreateSwitchRow(string title, string subtitle, bool initialValue, Action<bool> onToggled)
        {
            var row = new Grid
            {
                Margin = new Thickness(0, 4, 0, 4),
                Background = System.Windows.Media.Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            textStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(ColTextPrimary),
                TextWrapping = TextWrapping.Wrap
            });

            textStack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 10,
                Foreground = new SolidColorBrush(ColTextSecondary),
                TextWrapping = TextWrapping.Wrap
            });

            // Modern Pill Switch (34x18px)
            bool state = initialValue;
            var switchTrack = new Border
            {
                Width = 34,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center
            };

            var switchKnob = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush(ColTextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            };
            switchTrack.Child = switchKnob;

            Action updateSwitchVisual = () =>
            {
                switchTrack.Background = new SolidColorBrush(state ? ColPrimaryContainer : ColSurfaceInput);
                switchTrack.BorderBrush = new SolidColorBrush(state ? ColPrimaryContainer : ColBorderSubtle);
                switchTrack.BorderThickness = new Thickness(1);
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

            return row;
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
                Background = new SolidColorBrush(ColSurfaceInput),
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
            btn.MouseLeave += (s, e) => btn.Background = new SolidColorBrush(ColSurfaceInput);

            return btn;
        }

        private Grid BuildFooterBar()
        {
            var footer = new Grid
            {
                Background = new SolidColorBrush(ColSurfaceCanvas),
                Margin = new Thickness(12, 0, 12, 2)
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
            Width = 376;
            Height = 456;
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
