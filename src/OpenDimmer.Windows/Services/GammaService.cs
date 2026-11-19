using System;
using System.Windows.Forms;

namespace OpenDimmer.Services
{
    public static class GammaService
    {
        // Windows GDI SetDeviceGammaRamp allows dimming down to ~50% (factor 0.50).
        // In hybrid mode, we apply up to 45% dimming via hardware LUT (which dims the Start Menu,
        // Taskbar, system popups, and hardware mouse cursor), and the remaining percentage via overlay.
        public const int MaxHardwareDimPercent = 45;

        public static bool ApplyMonitorGamma(string deviceName, bool enabled, int hardwareDimPercent, string hexTint)
        {
            if (string.IsNullOrEmpty(deviceName))
                return false;

            IntPtr hdc = NativeMethods.CreateDC(null, deviceName, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero)
                return false;

            try
            {
                double brightnessFactor = 1.0;
                double rTint = 1.0, gTint = 1.0, bTint = 1.0;

                if (enabled && hardwareDimPercent > 0)
                {
                    int clamped = Math.Max(0, Math.Min(MaxHardwareDimPercent, hardwareDimPercent));
                    brightnessFactor = 1.0 - (clamped / 100.0);

                    if (!string.IsNullOrEmpty(hexTint) && hexTint.Length == 6 && hexTint != "000000")
                    {
                        try
                        {
                            byte r = Convert.ToByte(hexTint.Substring(0, 2), 16);
                            byte g = Convert.ToByte(hexTint.Substring(2, 2), 16);
                            byte b = Convert.ToByte(hexTint.Substring(4, 2), 16);
                            // Subtle tint blend on hardware ramp (85% base + 15% tint)
                            rTint = 0.85 + 0.15 * (r / 255.0);
                            gTint = 0.85 + 0.15 * (g / 255.0);
                            bTint = 0.85 + 0.15 * (b / 255.0);
                        }
                        catch { }
                    }
                }

                var ramp = new NativeMethods.RAMP
                {
                    Red = new ushort[256],
                    Green = new ushort[256],
                    Blue = new ushort[256]
                };

                for (int i = 0; i < 256; i++)
                {
                    int baseVal = i * 256;
                    ramp.Red[i] = (ushort)Math.Max(0, Math.Min(65535, (int)(baseVal * brightnessFactor * rTint)));
                    ramp.Green[i] = (ushort)Math.Max(0, Math.Min(65535, (int)(baseVal * brightnessFactor * gTint)));
                    ramp.Blue[i] = (ushort)Math.Max(0, Math.Min(65535, (int)(baseVal * brightnessFactor * bTint)));
                }

                return NativeMethods.SetDeviceGammaRamp(hdc, ref ramp);
            }
            finally
            {
                NativeMethods.DeleteDC(hdc);
            }
        }

        public static void RestoreAllMonitorsGamma()
        {
            foreach (var screen in Screen.AllScreens)
            {
                ApplyMonitorGamma(screen.DeviceName, false, 0, "000000");
            }
        }
    }
}
