using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using OpenDimmer.Services;

namespace OpenDimmer.UI
{
    public sealed class OverlayWindow : NativeWindow, IDisposable
    {
        private readonly IndicatorWindow _indicatorWindow;
        private Rectangle _bounds;
        private uint _fillColorRef;
        private byte _alpha;
        private bool _excludeFromCapture;
        private bool _disposed;

        public int ScreenId { get; private set; }
        public string DeviceName { get; private set; }

        public OverlayWindow(int screenId, string deviceName, Rectangle bounds, bool excludeFromCapture)
        {
            ScreenId = screenId;
            DeviceName = deviceName ?? string.Empty;
            _bounds = bounds;
            _excludeFromCapture = excludeFromCapture;
            _fillColorRef = 0;
            _alpha = 0;

            _indicatorWindow = new IndicatorWindow(screenId, bounds, excludeFromCapture);
        }

        public void Show()
        {
            EnsureCreated();
            UpdateBounds(_bounds);
        }

        private void EnsureCreated()
        {
            if (_disposed || Handle != IntPtr.Zero)
            {
                return;
            }

            var cp = new CreateParams
            {
                Caption = "OpenDimmer_Overlay_" + ScreenId,
                X = _bounds.Left,
                Y = _bounds.Top,
                Width = Math.Max(1, _bounds.Width),
                Height = Math.Max(1, _bounds.Height),
                Style = NativeMethods.WS_POPUP | NativeMethods.WS_DISABLED,
                ExStyle = NativeMethods.WS_EX_LAYERED
                        | NativeMethods.WS_EX_TRANSPARENT
                        | NativeMethods.WS_EX_TOOLWINDOW
                        | NativeMethods.WS_EX_NOACTIVATE
                        | NativeMethods.WS_EX_TOPMOST
            };

            CreateHandle(cp);
            if (Handle != IntPtr.Zero)
            {
                NativeMethods.SetProp(Handle, "NonRudeHWND", Handle);
                NativeMethods.SetLayeredWindowAttributes(Handle, 0, 0, NativeMethods.LWA_ALPHA);
                ApplyDisplayAffinity(Handle, _excludeFromCapture);
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WM_ERASEBKGND:
                    m.Result = (IntPtr)1;
                    return;

                case NativeMethods.WM_NCHITTEST:
                    m.Result = (IntPtr)NativeMethods.HTTRANSPARENT;
                    return;

                case NativeMethods.WM_MOUSEACTIVATE:
                    m.Result = (IntPtr)NativeMethods.MA_NOACTIVATE;
                    return;

                case NativeMethods.WM_DPICHANGED:
                    m.Result = IntPtr.Zero;
                    return;

                case NativeMethods.WM_PAINT:
                    PaintSolidOverlay();
                    m.Result = IntPtr.Zero;
                    return;
            }

            base.WndProc(ref m);
        }

        private void PaintSolidOverlay()
        {
            NativeMethods.PAINTSTRUCT ps;
            IntPtr hdc = NativeMethods.BeginPaint(Handle, out ps);
            if (hdc == IntPtr.Zero)
            {
                return;
            }

            try
            {
                NativeMethods.RECT clientRect;
                if (NativeMethods.GetClientRect(Handle, out clientRect))
                {
                    IntPtr brush = NativeMethods.CreateSolidBrush(_fillColorRef);
                    if (brush != IntPtr.Zero)
                    {
                        try
                        {
                            NativeMethods.FillRect(hdc, ref clientRect, brush);
                        }
                        finally
                        {
                            NativeMethods.DeleteObject(brush);
                        }
                    }
                }
            }
            finally
            {
                NativeMethods.EndPaint(Handle, ref ps);
            }
        }

        public void ApplyDimming(bool enabled, int dimPercent, string hexTint, int tintIntensity, int maxCap)
        {
            if (_disposed) return;
            EnsureCreated();
            if (Handle == IntPtr.Zero) return;

            byte r, g, b, alpha;
            DimmingMath.ComputeOverlayColorAndAlpha(
                enabled, dimPercent, hexTint, tintIntensity, maxCap,
                out r, out g, out b, out alpha);

            uint nextColorRef = DimmingMath.ToWin32ColorRef(r, g, b);
            bool colorChanged = nextColorRef != _fillColorRef;
            bool alphaChanged = alpha != _alpha;

            _fillColorRef = nextColorRef;
            _alpha = alpha;

            if (_alpha == 0)
            {
                NativeMethods.SetLayeredWindowAttributes(Handle, 0, 0, NativeMethods.LWA_ALPHA);
                NativeMethods.ShowWindow(Handle, NativeMethods.SW_HIDE);
            }
            else
            {
                if (alphaChanged || colorChanged)
                {
                    NativeMethods.SetLayeredWindowAttributes(Handle, 0, _alpha, NativeMethods.LWA_ALPHA);
                }

                NativeMethods.ShowWindow(Handle, NativeMethods.SW_SHOWNOACTIVATE);

                if (colorChanged)
                {
                    NativeMethods.InvalidateRect(Handle, IntPtr.Zero, false);
                }

                EnsureTopmost();
            }
        }

        public void SetIndicators(bool showId, bool showBoundary)
        {
            if (_disposed) return;
            _indicatorWindow.SetIndicators(showId, showBoundary);
            EnsureTopmost();
        }

        public void SetExcludeFromCapture(bool exclude)
        {
            _excludeFromCapture = exclude;
            if (Handle != IntPtr.Zero)
            {
                ApplyDisplayAffinity(Handle, exclude);
            }
            _indicatorWindow.SetExcludeFromCapture(exclude);
        }

        public void UpdateBounds(Rectangle physicalBounds)
        {
            _bounds = physicalBounds;
            if (Handle != IntPtr.Zero)
            {
                uint flags = NativeMethods.SWP_NOACTIVATE;
                if (_alpha > 0)
                {
                    flags |= NativeMethods.SWP_SHOWWINDOW;
                }

                NativeMethods.SetWindowPos(
                    Handle,
                    NativeMethods.HWND_TOPMOST,
                    physicalBounds.Left,
                    physicalBounds.Top,
                    Math.Max(1, physicalBounds.Width),
                    Math.Max(1, physicalBounds.Height),
                    flags);

                if (_alpha > 0)
                {
                    NativeMethods.InvalidateRect(Handle, IntPtr.Zero, false);
                }
            }
            _indicatorWindow.UpdateBounds(physicalBounds);
        }

        public void EnsureTopmost()
        {
            if (Handle != IntPtr.Zero && _alpha > 0)
            {
                NativeMethods.SetWindowPos(
                    Handle,
                    NativeMethods.HWND_TOPMOST,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
            }
            _indicatorWindow.EnsureTopmost();
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _indicatorWindow.Dispose();
            if (Handle != IntPtr.Zero)
            {
                try { DestroyHandle(); } catch { }
            }
        }

        internal static void ApplyDisplayAffinity(IntPtr hwnd, bool exclude)
        {
            try
            {
                NativeMethods.SetWindowDisplayAffinity(
                    hwnd,
                    exclude ? NativeMethods.WDA_EXCLUDEFROMCAPTURE : NativeMethods.WDA_NONE);
            }
            catch
            {
                // Ignore on older Windows builds without WDA_EXCLUDEFROMCAPTURE
            }
        }

        private sealed class IndicatorWindow : NativeWindow, IDisposable
        {
            private const uint KeyColorRef = 0x00FF00FF; // Magenta RGB(255, 0, 255)

            private readonly int _screenId;
            private Rectangle _bounds;
            private bool _showId;
            private bool _showBoundary;
            private bool _excludeFromCapture;
            private bool _disposed;

            public IndicatorWindow(int screenId, Rectangle bounds, bool excludeFromCapture)
            {
                _screenId = screenId;
                _bounds = bounds;
                _excludeFromCapture = excludeFromCapture;
            }

            public void SetIndicators(bool showId, bool showBoundary)
            {
                _showId = showId;
                _showBoundary = showBoundary;

                if (!_showId && !_showBoundary)
                {
                    if (Handle != IntPtr.Zero)
                    {
                        NativeMethods.ShowWindow(Handle, NativeMethods.SW_HIDE);
                    }
                    return;
                }

                EnsureCreated();
                if (Handle != IntPtr.Zero)
                {
                    NativeMethods.ShowWindow(Handle, NativeMethods.SW_SHOWNOACTIVATE);
                    NativeMethods.InvalidateRect(Handle, IntPtr.Zero, false);
                    EnsureTopmost();
                }
            }

            public void SetExcludeFromCapture(bool exclude)
            {
                _excludeFromCapture = exclude;
                if (Handle != IntPtr.Zero)
                {
                    ApplyDisplayAffinity(Handle, exclude);
                }
            }

            public void UpdateBounds(Rectangle physicalBounds)
            {
                _bounds = physicalBounds;
                if (Handle != IntPtr.Zero)
                {
                    NativeMethods.SetWindowPos(
                        Handle,
                        NativeMethods.HWND_TOPMOST,
                        physicalBounds.Left,
                        physicalBounds.Top,
                        Math.Max(1, physicalBounds.Width),
                        Math.Max(1, physicalBounds.Height),
                        NativeMethods.SWP_NOACTIVATE);

                    if (_showId || _showBoundary)
                    {
                        NativeMethods.InvalidateRect(Handle, IntPtr.Zero, false);
                    }
                }
            }

            public void EnsureTopmost()
            {
                if (Handle != IntPtr.Zero && (_showId || _showBoundary))
                {
                    NativeMethods.SetWindowPos(
                        Handle,
                        NativeMethods.HWND_TOPMOST,
                        0, 0, 0, 0,
                        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
                }
            }

            private void EnsureCreated()
            {
                if (_disposed || Handle != IntPtr.Zero) return;

                var cp = new CreateParams
                {
                    Caption = "OpenDimmer_Indicator_" + _screenId,
                    X = _bounds.Left,
                    Y = _bounds.Top,
                    Width = Math.Max(1, _bounds.Width),
                    Height = Math.Max(1, _bounds.Height),
                    Style = NativeMethods.WS_POPUP | NativeMethods.WS_DISABLED,
                    ExStyle = NativeMethods.WS_EX_LAYERED
                            | NativeMethods.WS_EX_TRANSPARENT
                            | NativeMethods.WS_EX_TOOLWINDOW
                            | NativeMethods.WS_EX_NOACTIVATE
                            | NativeMethods.WS_EX_TOPMOST
                };

                CreateHandle(cp);
                if (Handle != IntPtr.Zero)
                {
                    NativeMethods.SetProp(Handle, "NonRudeHWND", Handle);
                    NativeMethods.SetLayeredWindowAttributes(Handle, KeyColorRef, 255, NativeMethods.LWA_COLORKEY);
                    ApplyDisplayAffinity(Handle, _excludeFromCapture);
                }
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case NativeMethods.WM_ERASEBKGND:
                        m.Result = (IntPtr)1;
                        return;

                    case NativeMethods.WM_NCHITTEST:
                        m.Result = (IntPtr)NativeMethods.HTTRANSPARENT;
                        return;

                    case NativeMethods.WM_MOUSEACTIVATE:
                        m.Result = (IntPtr)NativeMethods.MA_NOACTIVATE;
                        return;

                    case NativeMethods.WM_DPICHANGED:
                        m.Result = IntPtr.Zero;
                        return;

                    case NativeMethods.WM_PAINT:
                        PaintIndicators();
                        m.Result = IntPtr.Zero;
                        return;
                }

                base.WndProc(ref m);
            }

            private void PaintIndicators()
            {
                NativeMethods.PAINTSTRUCT ps;
                IntPtr hdc = NativeMethods.BeginPaint(Handle, out ps);
                if (hdc == IntPtr.Zero) return;

                try
                {
                    NativeMethods.RECT rc;
                    if (!NativeMethods.GetClientRect(Handle, out rc)) return;

                    IntPtr keyBrush = NativeMethods.CreateSolidBrush(KeyColorRef);
                    if (keyBrush != IntPtr.Zero)
                    {
                        try { NativeMethods.FillRect(hdc, ref rc, keyBrush); }
                        finally { NativeMethods.DeleteObject(keyBrush); }
                    }

                    using (Graphics g = Graphics.FromHdc(hdc))
                    {
                        g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

                        if (_showBoundary)
                        {
                            using (var pen = new Pen(Color.Gold, 4f))
                            {
                                pen.Alignment = PenAlignment.Inset;
                                g.DrawRectangle(pen, 0, 0, Math.Max(1, rc.right - rc.left), Math.Max(1, rc.bottom - rc.top));
                            }
                        }

                        if (_showId)
                        {
                            string text = _screenId.ToString();
                            using (var font = new Font("Segoe UI", 72f, FontStyle.Bold, GraphicsUnit.Point))
                            {
                                g.DrawString(text, font, Brushes.Black, 35f, 27f);
                                g.DrawString(text, font, Brushes.White, 32f, 24f);
                            }
                        }
                    }
                }
                finally
                {
                    NativeMethods.EndPaint(Handle, ref ps);
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (Handle != IntPtr.Zero)
                {
                    try { DestroyHandle(); } catch { }
                }
            }
        }
    }
}
