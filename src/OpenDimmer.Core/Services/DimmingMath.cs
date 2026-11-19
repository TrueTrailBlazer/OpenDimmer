using System;
using System.Collections.Generic;
using System.Globalization;
using OpenDimmer.Models;

namespace OpenDimmer.Services
{
    public sealed class TintPreset
    {
        public string Name { get; private set; }
        public string Subtitle { get; private set; }
        public string HexCode { get; private set; }
        public string ShortBadge { get; private set; }
        public string KelvinLabel { get; private set; }
        public double TargetR { get; private set; }
        public double TargetG { get; private set; }
        public double TargetB { get; private set; }
        public string DotHex { get; private set; }

        public TintPreset(
            string name,
            string subtitle,
            string hexCode,
            string shortBadge,
            string kelvinLabel,
            double targetR,
            double targetG,
            double targetB,
            string dotHex)
        {
            Name = name;
            Subtitle = subtitle;
            HexCode = hexCode;
            ShortBadge = shortBadge;
            KelvinLabel = kelvinLabel;
            TargetR = targetR;
            TargetG = targetG;
            TargetB = targetB;
            DotHex = dotHex;
        }
    }

    public static class DimmingMath
    {
        public static readonly TintPreset[] TintPresets = new TintPreset[]
        {
            new TintPreset("Neutral Black", "Default \u2022 6500K", "000000", "Native", "6500K", 1.00, 1.00, 1.00, "#60A5FA"),
            new TintPreset("Warm Amber", "Night Light \u2022 4500K", "2B1300", "4500K", "4500K", 1.00, 0.78, 0.50, "#FFB95F"),
            new TintPreset("Deep Ember", "Low Blue \u2022 2200K", "3A0E00", "2200K", "2200K", 1.00, 0.52, 0.18, "#FF7043"),
            new TintPreset("Soft Sepia", "Reading \u2022 3200K", "22180C", "3200K", "3200K", 1.00, 0.70, 0.42, "#D4A373")
        };

        public static TintPreset FindTintPreset(string hexTint)
        {
            string normalized = string.IsNullOrEmpty(hexTint)
                ? "000000"
                : hexTint.Trim().ToUpperInvariant();

            for (int i = 0; i < TintPresets.Length; i++)
            {
                if (string.Equals(TintPresets[i].HexCode, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return TintPresets[i];
                }
            }

            return TintPresets[0];
        }

        public static string GetNextTintCode(string currentHex)
        {
            string normalized = string.IsNullOrEmpty(currentHex)
                ? "000000"
                : currentHex.Trim().ToUpperInvariant();

            int currentIndex = 0;
            for (int i = 0; i < TintPresets.Length; i++)
            {
                if (string.Equals(TintPresets[i].HexCode, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % TintPresets.Length;
            return TintPresets[nextIndex].HexCode;
        }

        public static int ClampDimPercent(int dim, int maxCap)
        {
            int effectiveMax = Math.Max(10, Math.Min(95, maxCap > 0 ? maxCap : 90));
            return Math.Max(0, Math.Min(effectiveMax, dim));
        }

        public static int ClampTintIntensity(int intensity)
        {
            if (intensity <= 0)
            {
                return 60;
            }
            return Math.Max(10, Math.Min(100, intensity));
        }

        public static void GetTintMultipliers(
            string hexTint,
            int tintIntensity,
            out double rMult,
            out double gMult,
            out double bMult)
        {
            rMult = 1.0;
            gMult = 1.0;
            bMult = 1.0;

            if (string.IsNullOrEmpty(hexTint))
            {
                return;
            }

            string normalized = hexTint.Trim().ToUpperInvariant();
            if (normalized == "000000")
            {
                return;
            }

            double t = ClampTintIntensity(tintIntensity) / 100.0;

            for (int i = 0; i < TintPresets.Length; i++)
            {
                if (string.Equals(TintPresets[i].HexCode, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    rMult = 1.0 + (TintPresets[i].TargetR - 1.0) * t;
                    gMult = 1.0 + (TintPresets[i].TargetG - 1.0) * t;
                    bMult = 1.0 + (TintPresets[i].TargetB - 1.0) * t;
                    return;
                }
            }

            if (normalized.Length == 6)
            {
                byte rawR, rawG, rawB;
                if (byte.TryParse(normalized.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rawR)
                    && byte.TryParse(normalized.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rawG)
                    && byte.TryParse(normalized.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rawB))
                {
                    double maxCh = Math.Max(1.0, Math.Max((double)rawR, Math.Max((double)rawG, (double)rawB)));
                    double targetR = rawR / maxCh;
                    double targetG = rawG / maxCh;
                    double targetB = rawB / maxCh;

                    rMult = 1.0 + (targetR - 1.0) * t;
                    gMult = 1.0 + (targetG - 1.0) * t;
                    bMult = 1.0 + (targetB - 1.0) * t;
                }
            }
        }

        public static void ApplyGammaTransform(
            ushort[] baseRed,
            ushort[] baseGreen,
            ushort[] baseBlue,
            bool enabled,
            int dimPercent,
            string hexTint,
            int tintIntensity,
            int maxCap,
            out ushort[] outRed,
            out ushort[] outGreen,
            out ushort[] outBlue)
        {
            if (baseRed == null) throw new ArgumentNullException("baseRed");
            if (baseGreen == null) throw new ArgumentNullException("baseGreen");
            if (baseBlue == null) throw new ArgumentNullException("baseBlue");

            int size = baseRed.Length;
            if (baseGreen.Length != size || baseBlue.Length != size)
            {
                throw new ArgumentException("RGB gamma LUT arrays must have equal length.");
            }

            outRed = new ushort[size];
            outGreen = new ushort[size];
            outBlue = new ushort[size];

            int clampedDim = ClampDimPercent(dimPercent, maxCap);
            bool isNeutral = string.IsNullOrEmpty(hexTint)
                || string.Equals(hexTint.Trim(), "000000", StringComparison.OrdinalIgnoreCase);

            if (!enabled || (clampedDim == 0 && isNeutral))
            {
                Array.Copy(baseRed, outRed, size);
                Array.Copy(baseGreen, outGreen, size);
                Array.Copy(baseBlue, outBlue, size);
                return;
            }

            double brightnessFactor = 1.0 - (clampedDim / 100.0);
            double rMult, gMult, bMult;
            GetTintMultipliers(hexTint, tintIntensity, out rMult, out gMult, out bMult);

            for (int i = 0; i < size; i++)
            {
                outRed[i] = ClampUShort(baseRed[i] * brightnessFactor * rMult);
                outGreen[i] = ClampUShort(baseGreen[i] * brightnessFactor * gMult);
                outBlue[i] = ClampUShort(baseBlue[i] * brightnessFactor * bMult);
            }
        }

        private static ushort ClampUShort(double value)
        {
            if (value <= 0.0) return 0;
            if (value >= 65535.0) return 65535;
            return (ushort)Math.Round(value);
        }

        public static void ApplyMasterChange(
            AppSettings settings,
            bool masterOn,
            int newMasterDim,
            int previousMasterDim)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            int maxCap = settings.MaxDimPercent > 0 ? settings.MaxDimPercent : 90;
            int clamped = ClampDimPercent(newMasterDim, maxCap);
            int delta = clamped - previousMasterDim;

            settings.Master.On = masterOn;
            settings.Master.Dim = clamped;

            for (int i = 0; i < settings.Screens.Count; i++)
            {
                ScreenSettings scr = settings.Screens[i];
                scr.On = masterOn;
                int targetDim = settings.Master.Proportional
                    ? ClampDimPercent(scr.Dim + delta, maxCap)
                    : clamped;
                scr.Dim = targetDim;
            }
        }

        public static void ApplyQuickPreset(AppSettings settings, int presetDim)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            int maxCap = settings.MaxDimPercent > 0 ? settings.MaxDimPercent : 90;
            int clamped = ClampDimPercent(presetDim, maxCap);
            bool turnOn = clamped > 0;

            settings.Master.On = turnOn;
            if (turnOn)
            {
                settings.Master.Dim = clamped;
            }

            for (int i = 0; i < settings.Screens.Count; i++)
            {
                ScreenSettings scr = settings.Screens[i];
                scr.On = turnOn;
                if (turnOn)
                {
                    scr.Dim = clamped;
                }
            }
        }

        public static int CountActiveBehaviorSettings(AppSettings settings, bool includeWindowsOnly)
        {
            if (settings == null) return 0;

            int count = 0;
            if (settings.Master.Proportional) count++;
            if (settings.CloseHidesToTray) count++;
            if (settings.ShowInTaskbar) count++;
            if (settings.StartMinimised) count++;
            if (settings.AutoRun) count++;

            if (includeWindowsOnly)
            {
                if (settings.ShowIds) count++;
                if (settings.ShowBoundaries) count++;
                if (settings.UseHardwareGamma) count++;
                if (settings.ExcludeFromCapture) count++;
                if (settings.AvoidTaskbarAutoHideLock) count++;
                if (settings.NonDimmableWindow) count++;
                if (settings.MovableWindow) count++;
            }

            return count;
        }

        public static void ComputeOverlayColorAndAlpha(
            bool enabled,
            int dimPercent,
            string hexTint,
            int tintIntensity,
            int maxCap,
            out byte r,
            out byte g,
            out byte b,
            out byte alpha)
        {
            r = 0;
            g = 0;
            b = 0;
            alpha = 0;

            if (!enabled)
            {
                return;
            }

            int clampedDim = ClampDimPercent(dimPercent, maxCap);
            if (clampedDim <= 0)
            {
                return;
            }

            double clampedIntensity = ClampTintIntensity(tintIntensity) / 100.0;
            if (!string.IsNullOrEmpty(hexTint))
            {
                string trimmed = hexTint.Trim();
                if (trimmed.Length == 6 && !string.Equals(trimmed, "000000", StringComparison.OrdinalIgnoreCase))
                {
                    byte baseR, baseG, baseB;
                    if (byte.TryParse(trimmed.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out baseR)
                        && byte.TryParse(trimmed.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out baseG)
                        && byte.TryParse(trimmed.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out baseB))
                    {
                        double scale = clampedIntensity * 1.4;
                        r = (byte)Math.Max(0, Math.Min(255, (int)(baseR * scale)));
                        g = (byte)Math.Max(0, Math.Min(255, (int)(baseG * scale)));
                        b = (byte)Math.Max(0, Math.Min(255, (int)(baseB * scale)));
                    }
                }
            }

            alpha = (byte)Math.Max(0, Math.Min(255, (int)Math.Round((clampedDim / 100.0) * 255.0)));
        }

        public static uint ToWin32ColorRef(byte r, byte g, byte b)
        {
            return (uint)(r | (g << 8) | (b << 16));
        }

        public static int GetNextMaxDimCap(int currentCap)
        {
            int[] caps = new int[] { 80, 85, 90, 95 };
            for (int i = 0; i < caps.Length; i++)
            {
                if (caps[i] == currentCap)
                {
                    return caps[(i + 1) % caps.Length];
                }
            }
            return 90;
        }

        public static bool IsWindowVisibleOnMonitors(
            double left,
            double top,
            double width,
            double height,
            IList<MonitorDescriptor> monitors)
        {
            if (monitors == null || monitors.Count == 0)
            {
                return true;
            }

            if (double.IsNaN(left) || double.IsNaN(top) || double.IsNaN(width) || double.IsNaN(height)
                || width <= 0.0 || height <= 0.0)
            {
                return false;
            }

            for (int i = 0; i < monitors.Count; i++)
            {
                MonitorDescriptor m = monitors[i];
                if (m.Width <= 0 || m.Height <= 0)
                {
                    continue;
                }

                double overlapX = Math.Min(left + width, m.X + m.Width) - Math.Max(left, (double)m.X);
                double overlapY = Math.Min(top + height, m.Y + m.Height) - Math.Max(top, (double)m.Y);
                if (overlapX >= 40.0 && overlapY >= 40.0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
