using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenDimmer.Models;
using OpenDimmer.Services;

namespace OpenDimmer.Linux.Services
{
    internal sealed class SavedGammaLut
    {
        public nuint CrtcId { get; set; }
        public int Size { get; set; }
        public ushort[] Red { get; set; } = Array.Empty<ushort>();
        public ushort[] Green { get; set; } = Array.Empty<ushort>();
        public ushort[] Blue { get; set; } = Array.Empty<ushort>();
    }

    public sealed class X11MonitorInfo
    {
        public MonitorDescriptor Descriptor { get; init; } = new MonitorDescriptor();
        public nuint OutputId { get; init; }
        public nuint CrtcId { get; init; }
        public int GammaSize { get; init; }
    }

    public sealed class X11DisplayService : IDisposable
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, SavedGammaLut> _baselineLuts =
            new Dictionary<string, SavedGammaLut>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _modifiedOutputs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private IntPtr _display;
        private List<X11MonitorInfo> _monitors = new List<X11MonitorInfo>();

        public IReadOnlyList<X11MonitorInfo> Monitors
        {
            get
            {
                lock (_sync)
                {
                    return _monitors.ToList();
                }
            }
        }

        public X11DisplayService()
        {
            _display = X11Native.XOpenDisplay(null);
            if (_display == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Unable to open X11 display. Verify DISPLAY is set and running under X11/XWayland.");
            }
            DiscoverMonitors();
        }

        public List<X11MonitorInfo> DiscoverMonitors()
        {
            lock (_sync)
            {
                if (_display == IntPtr.Zero)
                {
                    return new List<X11MonitorInfo>();
                }

                nuint root = X11Native.XDefaultRootWindow(_display);
                nuint primaryOutput = X11Native.XRRGetOutputPrimary(_display, root);
                IntPtr resPtr = X11Native.XRRGetScreenResourcesCurrent(_display, root);
                if (resPtr == IntPtr.Zero)
                {
                    return new List<X11MonitorInfo>();
                }

                var rawMonitors = new List<X11MonitorInfo>();
                try
                {
                    var res = Marshal.PtrToStructure<X11Native.XRRScreenResources>(resPtr);
                    int nOut = res.noutput;
                    int ptrSize = IntPtr.Size;

                    for (int i = 0; i < nOut; i++)
                    {
                        nuint outputId = ReadNuint(res.outputs, i * ptrSize);
                        IntPtr outInfoPtr = X11Native.XRRGetOutputInfo(_display, resPtr, outputId);
                        if (outInfoPtr == IntPtr.Zero) continue;

                        try
                        {
                            var outInfo = Marshal.PtrToStructure<X11Native.XRROutputInfo>(outInfoPtr);
                            if (outInfo.connection != X11Native.RR_Connected || outInfo.crtc == 0)
                            {
                                continue;
                            }

                            string outputName = Marshal.PtrToStringUTF8(outInfo.name, outInfo.nameLen)
                                ?? ("OUTPUT-" + (i + 1));

                            IntPtr crtcInfoPtr = X11Native.XRRGetCrtcInfo(_display, resPtr, outInfo.crtc);
                            if (crtcInfoPtr == IntPtr.Zero) continue;

                            try
                            {
                                var crtcInfo = Marshal.PtrToStructure<X11Native.XRRCrtcInfo>(crtcInfoPtr);
                                int gammaSize = X11Native.XRRGetCrtcGammaSize(_display, outInfo.crtc);
                                if (gammaSize > 0 && !_baselineLuts.ContainsKey(outputName))
                                {
                                    SavedGammaLut? lut = CaptureCurrentLut(_display, outInfo.crtc, gammaSize);
                                    if (lut != null)
                                    {
                                        _baselineLuts[outputName] = lut;
                                    }
                                }
                                else if (_baselineLuts.TryGetValue(outputName, out SavedGammaLut? existingLut))
                                {
                                    existingLut.CrtcId = outInfo.crtc;
                                }

                                bool isPrimary = outputId == primaryOutput;
                                byte[]? edid = ReadDrmEdidForOutput(outputName);
                                bool isInternal;
                                string friendly = EdidParser.ResolveFriendlyName(edid, null, rawMonitors.Count + 1, out isInternal);

                                string role = isPrimary ? "Primary" : "Extended";
                                var desc = new MonitorDescriptor
                                {
                                    Index = rawMonitors.Count + 1,
                                    DeviceName = outputName,
                                    FriendlyName = friendly,
                                    PortBadge = outputName,
                                    Subtitle = outputName + " \u00B7 " + role,
                                    IsPrimary = isPrimary,
                                    X = crtcInfo.x,
                                    Y = crtcInfo.y,
                                    Width = (int)crtcInfo.width,
                                    Height = (int)crtcInfo.height
                                };

                                rawMonitors.Add(new X11MonitorInfo
                                {
                                    Descriptor = desc,
                                    OutputId = outputId,
                                    CrtcId = outInfo.crtc,
                                    GammaSize = gammaSize
                                });
                            }
                            finally
                            {
                                X11Native.XRRFreeCrtcInfo(crtcInfoPtr);
                            }
                        }
                        finally
                        {
                            X11Native.XRRFreeOutputInfo(outInfoPtr);
                        }
                    }
                }
                finally
                {
                    X11Native.XRRFreeScreenResources(resPtr);
                }

                // Order Primary monitor first, then left-to-right by X position
                _monitors = rawMonitors
                    .OrderByDescending(m => m.Descriptor.IsPrimary)
                    .ThenBy(m => m.Descriptor.X)
                    .ThenBy(m => m.Descriptor.Y)
                    .ThenBy(m => m.Descriptor.DeviceName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                for (int i = 0; i < _monitors.Count; i++)
                {
                    _monitors[i].Descriptor.Index = i + 1;
                }

                return _monitors.ToList();
            }
        }

        public bool ApplyScreen(
            string deviceName,
            bool enabled,
            int dimPercent,
            string tintHex,
            int tintIntensity,
            int maxCap)
        {
            lock (_sync)
            {
                if (_display == IntPtr.Zero || string.IsNullOrEmpty(deviceName))
                {
                    return false;
                }

                X11MonitorInfo? monitor = _monitors.FirstOrDefault(m =>
                    string.Equals(m.Descriptor.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
                if (monitor == null || monitor.GammaSize <= 0)
                {
                    return false;
                }

                if (!_baselineLuts.TryGetValue(deviceName, out SavedGammaLut? baseline))
                {
                    baseline = CaptureCurrentLut(_display, monitor.CrtcId, monitor.GammaSize);
                    if (baseline == null) return false;
                    _baselineLuts[deviceName] = baseline;
                }

                DimmingMath.ApplyGammaTransform(
                    baseline.Red,
                    baseline.Green,
                    baseline.Blue,
                    enabled,
                    dimPercent,
                    tintHex,
                    tintIntensity,
                    maxCap,
                    out ushort[] outR,
                    out ushort[] outG,
                    out ushort[] outB);

                bool ok = WriteLutToCrtc(_display, monitor.CrtcId, outR, outG, outB);
                if (ok)
                {
                    if (enabled && (dimPercent > 0 || !string.Equals(tintHex, "000000", StringComparison.OrdinalIgnoreCase)))
                    {
                        _modifiedOutputs.Add(deviceName);
                    }
                    else
                    {
                        _modifiedOutputs.Remove(deviceName);
                    }
                }
                return ok;
            }
        }

        public void ApplyAll(AppSettings settings)
        {
            if (settings == null) return;
            lock (_sync)
            {
                foreach (X11MonitorInfo mon in _monitors)
                {
                    ScreenSettings? cfg = settings.Screens.FirstOrDefault(s =>
                        string.Equals(s.DeviceName, mon.Descriptor.DeviceName, StringComparison.OrdinalIgnoreCase));
                    if (cfg == null) continue;

                    ApplyScreen(
                        mon.Descriptor.DeviceName,
                        cfg.On,
                        cfg.Dim,
                        cfg.Tint,
                        cfg.TintIntensity,
                        settings.MaxDimPercent);
                }
            }
        }

        public void RestoreAll()
        {
            lock (_sync)
            {
                if (_display == IntPtr.Zero) return;

                foreach (string dev in _modifiedOutputs.ToList())
                {
                    if (_baselineLuts.TryGetValue(dev, out SavedGammaLut? lut) && lut.CrtcId != 0)
                    {
                        WriteLutToCrtc(_display, lut.CrtcId, lut.Red, lut.Green, lut.Blue);
                    }
                }
                _modifiedOutputs.Clear();
            }
        }

        public string BuildDiagnosticsReport(AppSettings settings, string configPath)
        {
            lock (_sync)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== OpenDimmer Linux Diagnostic Report ===");
                sb.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("OS: " + RuntimeInformation.OSDescription);
                sb.AppendLine("Arch: " + RuntimeInformation.OSArchitecture + " | Process: " + RuntimeInformation.ProcessArchitecture);
                sb.AppendLine("Session: " + (Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "unknown")
                    + " | Desktop: " + (Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "unknown")
                    + " | DISPLAY: " + (Environment.GetEnvironmentVariable("DISPLAY") ?? "unset"));
                sb.AppendLine("Config Path: " + configPath);
                sb.AppendLine();

                foreach (X11MonitorInfo m in _monitors)
                {
                    MonitorDescriptor d = m.Descriptor;
                    sb.AppendLine($"DVC #{d.Index}: [{d.DeviceName}] [{d.FriendlyName}] [Primary: {d.IsPrimary}] [CRTC: {m.CrtcId}] [GammaSize: {m.GammaSize}]");
                    sb.AppendLine($"BOU #{d.Index}: {{X={d.X}, Y={d.Y}, Width={d.Width}, Height={d.Height}}}");
                }

                sb.AppendLine();
                sb.AppendLine("=== OpenDimmer.json ===");
                if (File.Exists(configPath))
                {
                    sb.AppendLine(File.ReadAllText(configPath, Encoding.UTF8));
                }
                else
                {
                    sb.AppendLine("(not created yet)");
                }
                return sb.ToString();
            }
        }

        private static nuint ReadNuint(IntPtr basePtr, int byteOffset)
        {
            IntPtr val = Marshal.ReadIntPtr(basePtr, byteOffset);
            return IntPtr.Size == 8 ? (nuint)(ulong)val.ToInt64() : (nuint)(uint)val.ToInt32();
        }

        private static SavedGammaLut? CaptureCurrentLut(IntPtr display, nuint crtcId, int expectedSize)
        {
            IntPtr gammaPtr = X11Native.XRRGetCrtcGamma(display, crtcId);
            if (gammaPtr == IntPtr.Zero) return null;

            try
            {
                var gamma = Marshal.PtrToStructure<X11Native.XRRCrtcGamma>(gammaPtr);
                int size = gamma.size > 0 ? gamma.size : expectedSize;
                if (size <= 0) return null;

                return new SavedGammaLut
                {
                    CrtcId = crtcId,
                    Size = size,
                    Red = CopyUShortArray(gamma.red, size),
                    Green = CopyUShortArray(gamma.green, size),
                    Blue = CopyUShortArray(gamma.blue, size)
                };
            }
            finally
            {
                X11Native.XRRFreeGamma(gammaPtr);
            }
        }

        private static bool WriteLutToCrtc(
            IntPtr display,
            nuint crtcId,
            ushort[] red,
            ushort[] green,
            ushort[] blue)
        {
            int size = red.Length;
            IntPtr gammaPtr = X11Native.XRRAllocGamma(size);
            if (gammaPtr == IntPtr.Zero) return false;

            try
            {
                var gamma = Marshal.PtrToStructure<X11Native.XRRCrtcGamma>(gammaPtr);
                CopyToNativeUShort(red, gamma.red);
                CopyToNativeUShort(green, gamma.green);
                CopyToNativeUShort(blue, gamma.blue);

                X11Native.XRRSetCrtcGamma(display, crtcId, gammaPtr);
                X11Native.XFlush(display);
                return true;
            }
            finally
            {
                X11Native.XRRFreeGamma(gammaPtr);
            }
        }

        private static ushort[] CopyUShortArray(IntPtr ptr, int size)
        {
            var temp = new short[size];
            Marshal.Copy(ptr, temp, 0, size);
            var result = new ushort[size];
            Buffer.BlockCopy(temp, 0, result, 0, size * sizeof(ushort));
            return result;
        }

        private static void CopyToNativeUShort(ushort[] src, IntPtr dstPtr)
        {
            var temp = new short[src.Length];
            Buffer.BlockCopy(src, 0, temp, 0, src.Length * sizeof(ushort));
            Marshal.Copy(temp, 0, dstPtr, src.Length);
        }

        private static byte[]? ReadDrmEdidForOutput(string xrandrName)
        {
            try
            {
                const string drmRoot = "/sys/class/drm";
                if (!Directory.Exists(drmRoot)) return null;

                string targetNorm = NormalizeConnectorName(xrandrName);
                foreach (string dir in Directory.GetDirectories(drmRoot))
                {
                    string folder = Path.GetFileName(dir);
                    int dashIdx = folder.IndexOf('-');
                    if (dashIdx <= 0) continue;

                    string connector = folder.Substring(dashIdx + 1);
                    if (string.Equals(connector, xrandrName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(NormalizeConnectorName(connector), targetNorm, StringComparison.OrdinalIgnoreCase))
                    {
                        string edidPath = Path.Combine(dir, "edid");
                        if (File.Exists(edidPath))
                        {
                            byte[] data = File.ReadAllBytes(edidPath);
                            if (data.Length >= 128) return data;
                        }
                    }
                }
            }
            catch
            {
                // Ignore DRM sysfs permission errors
            }

            return null;
        }

        private static string NormalizeConnectorName(string name)
        {
            return name
                .Replace("-A-", "-")
                .Replace("-D-", "-")
                .Replace("-I-", "-")
                .ToUpperInvariant();
        }

        public void Dispose()
        {
            lock (_sync)
            {
                RestoreAll();
                if (_display != IntPtr.Zero)
                {
                    X11Native.XCloseDisplay(_display);
                    _display = IntPtr.Zero;
                }
            }
        }
    }
}
