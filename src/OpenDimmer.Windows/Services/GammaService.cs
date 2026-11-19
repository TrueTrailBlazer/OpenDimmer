using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OpenDimmer.Services
{
    public static class GammaService
    {
        // Windows GDI SetDeviceGammaRamp allows dimming down to ~50% (factor 0.50).
        // In hybrid mode, we apply up to 45% dimming via hardware LUT (which dims the Start Menu,
        // Taskbar, system popups, and hardware mouse cursor), and the remaining percentage via overlay.
        public const int MaxHardwareDimPercent = 45;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, NativeMethods.RAMP> BaselineRamps =
            new Dictionary<string, NativeMethods.RAMP>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ModifiedDevices =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static bool ApplyMonitorGamma(string deviceName, bool enabled, int hardwareDimPercent, string hexTint)
        {
            return ApplyMonitorGamma(deviceName, enabled, hardwareDimPercent, hexTint, 60);
        }

        public static bool ApplyMonitorGamma(
            string deviceName,
            bool enabled,
            int hardwareDimPercent,
            string hexTint,
            int tintIntensity)
        {
            if (string.IsNullOrEmpty(deviceName))
                return false;

            lock (Sync)
            {
                if (!enabled || hardwareDimPercent <= 0)
                {
                    if (!ModifiedDevices.Contains(deviceName))
                    {
                        return true;
                    }

                    NativeMethods.RAMP savedBaseline;
                    if (!BaselineRamps.TryGetValue(deviceName, out savedBaseline))
                    {
                        savedBaseline = CreateLinearRamp();
                    }

                    bool restored = WriteRampToDevice(deviceName, savedBaseline);
                    if (restored)
                    {
                        ModifiedDevices.Remove(deviceName);
                    }
                    return restored;
                }

                IntPtr hdc = NativeMethods.CreateDC(null, deviceName, null, IntPtr.Zero);
                if (hdc == IntPtr.Zero)
                    return false;

                try
                {
                    NativeMethods.RAMP baseline;
                    if (!BaselineRamps.TryGetValue(deviceName, out baseline))
                    {
                        baseline = CaptureOrLinearBaseline(hdc);
                        BaselineRamps[deviceName] = CloneRamp(baseline);
                    }

                    int clampedHwDim = Math.Max(0, Math.Min(MaxHardwareDimPercent, hardwareDimPercent));
                    // Use subtle hardware tint scaling (up to ~35% of user slider) so strict GDI gamma checks don't reject the curve
                    int hwTintIntensity = Math.Max(10, Math.Min(40, tintIntensity / 2));

                    ushort[] outRed;
                    ushort[] outGreen;
                    ushort[] outBlue;
                    DimmingMath.ApplyGammaTransform(
                        baseline.Red,
                        baseline.Green,
                        baseline.Blue,
                        true,
                        clampedHwDim,
                        hexTint,
                        hwTintIntensity,
                        MaxHardwareDimPercent,
                        out outRed,
                        out outGreen,
                        out outBlue);

                    var ramp = new NativeMethods.RAMP
                    {
                        Red = outRed,
                        Green = outGreen,
                        Blue = outBlue
                    };

                    if (NativeMethods.SetDeviceGammaRamp(hdc, ref ramp))
                    {
                        ModifiedDevices.Add(deviceName);
                        return true;
                    }

                    // Fallback: retry brightness-only transform on baseline ICC curve if driver rejects colored ramp
                    DimmingMath.ApplyGammaTransform(
                        baseline.Red,
                        baseline.Green,
                        baseline.Blue,
                        true,
                        clampedHwDim,
                        "000000",
                        60,
                        MaxHardwareDimPercent,
                        out outRed,
                        out outGreen,
                        out outBlue);

                    ramp.Red = outRed;
                    ramp.Green = outGreen;
                    ramp.Blue = outBlue;

                    if (NativeMethods.SetDeviceGammaRamp(hdc, ref ramp))
                    {
                        ModifiedDevices.Add(deviceName);
                        return true;
                    }

                    return false;
                }
                finally
                {
                    NativeMethods.DeleteDC(hdc);
                }
            }
        }

        public static void RestoreAllMonitorsGamma()
        {
            lock (Sync)
            {
                var devicesToRestore = new List<string>(ModifiedDevices);
                for (int i = 0; i < devicesToRestore.Count; i++)
                {
                    string dev = devicesToRestore[i];
                    NativeMethods.RAMP baseline;
                    if (!BaselineRamps.TryGetValue(dev, out baseline))
                    {
                        baseline = CreateLinearRamp();
                    }

                    if (WriteRampToDevice(dev, baseline))
                    {
                        ModifiedDevices.Remove(dev);
                    }
                }
            }
        }

        private static bool WriteRampToDevice(string deviceName, NativeMethods.RAMP ramp)
        {
            IntPtr hdc = NativeMethods.CreateDC(null, deviceName, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero)
                return false;

            try
            {
                var copy = CloneRamp(ramp);
                return NativeMethods.SetDeviceGammaRamp(hdc, ref copy);
            }
            finally
            {
                NativeMethods.DeleteDC(hdc);
            }
        }

        private static NativeMethods.RAMP CaptureOrLinearBaseline(IntPtr hdc)
        {
            var captured = new NativeMethods.RAMP
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            if (NativeMethods.GetDeviceGammaRamp(hdc, ref captured))
            {
                return captured;
            }

            return CreateLinearRamp();
        }

        private static NativeMethods.RAMP CreateLinearRamp()
        {
            var ramp = new NativeMethods.RAMP
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };
            for (int i = 0; i < 256; i++)
            {
                ushort val = (ushort)(i * 256);
                ramp.Red[i] = val;
                ramp.Green[i] = val;
                ramp.Blue[i] = val;
            }
            return ramp;
        }

        private static NativeMethods.RAMP CloneRamp(NativeMethods.RAMP src)
        {
            var copy = new NativeMethods.RAMP
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };
            Array.Copy(src.Red, copy.Red, 256);
            Array.Copy(src.Green, copy.Green, 256);
            Array.Copy(src.Blue, copy.Blue, 256);
            return copy;
        }
    }
}
