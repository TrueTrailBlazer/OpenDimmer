using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using OpenDimmer.Services;
using MediaColor = System.Windows.Media.Color;

namespace OpenDimmer.UI
{
    public sealed class OverlayWindow : Window
    {
        private readonly SolidColorBrush _dimBrush;
        private readonly Border _boundaryBorder;
        private readonly TextBlock _identityText;
        private IntPtr _hwnd;

        public int ScreenId { get; private set; }
        public string DeviceName { get; private set; }
        public IntPtr Handle { get { return _hwnd; } }

        public OverlayWindow(int screenId, string deviceName, Rectangle bounds)
        {
            ScreenId = screenId;
            DeviceName = deviceName;

            Title = "OpenDimmer_Overlay_" + screenId;
            ShowInTaskbar = false;
            Topmost = true;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Focusable = false;
            ShowActivated = false;

            _dimBrush = new SolidColorBrush(Colors.Black) { Opacity = 0.0 };
            Background = _dimBrush;

            var rootGrid = new Grid();

            _boundaryBorder = new Border
            {
                BorderThickness = new Thickness(4),
                BorderBrush = new SolidColorBrush(Colors.Gold) { Opacity = 0.0 }
            };

            _identityText = new TextBlock
            {
                Text = screenId.ToString(),
                FontSize = 96,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(32, 24, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 3,
                    BlurRadius = 8,
                    Opacity = 0.9
                }
            };

            rootGrid.Children.Add(_boundaryBorder);
            rootGrid.Children.Add(_identityText);
            Content = rootGrid;

            UpdateBounds(bounds);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = NativeMethods.GetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE);
            exStyle |= NativeMethods.WS_EX_LAYERED
                     | NativeMethods.WS_EX_TRANSPARENT
                     | NativeMethods.WS_EX_TOOLWINDOW
                     | NativeMethods.WS_EX_NOACTIVATE
                     | NativeMethods.WS_EX_TOPMOST;

            NativeMethods.SetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE, exStyle);
            NativeMethods.SetProp(_hwnd, "NonRudeHWND", _hwnd);
        }

        public void ApplyDimming(bool enabled, int dimPercent, string hexTint)
        {
            if (!enabled)
            {
                _dimBrush.Opacity = 0.0;
                return;
            }

            if (!string.IsNullOrEmpty(hexTint) && hexTint.Length == 6)
            {
                try
                {
                    byte r = Convert.ToByte(hexTint.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hexTint.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hexTint.Substring(4, 2), 16);
                    _dimBrush.Color = MediaColor.FromRgb(r, g, b);
                }
                catch
                {
                    _dimBrush.Color = Colors.Black;
                }
            }

            double clamped = Math.Max(0, Math.Min(95, dimPercent));
            _dimBrush.Opacity = clamped / 100.0;
            EnsureTopmost();
        }

        public void SetIndicators(bool showId, bool showBoundary)
        {
            _identityText.Visibility = showId ? Visibility.Visible : Visibility.Collapsed;
            _boundaryBorder.BorderBrush.Opacity = showBoundary ? 0.85 : 0.0;
        }

        public void UpdateBounds(Rectangle physicalBounds)
        {
            if (_hwnd != IntPtr.Zero)
            {
                NativeMethods.SetWindowPos(
                    _hwnd,
                    NativeMethods.HWND_TOPMOST,
                    physicalBounds.Left,
                    physicalBounds.Top,
                    physicalBounds.Width,
                    physicalBounds.Height,
                    NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
            }
            else
            {
                Left = physicalBounds.Left;
                Top = physicalBounds.Top;
                Width = physicalBounds.Width;
                Height = physicalBounds.Height;
            }
        }

        public void EnsureTopmost()
        {
            if (_hwnd != IntPtr.Zero)
            {
                NativeMethods.SetWindowPos(
                    _hwnd,
                    NativeMethods.HWND_TOPMOST,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
            }
        }
    }
}
