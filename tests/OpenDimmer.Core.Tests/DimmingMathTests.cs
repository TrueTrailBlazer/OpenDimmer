using System;
using System.Collections.Generic;
using OpenDimmer.Models;
using OpenDimmer.Services;
using Xunit;

namespace OpenDimmer.Core.Tests
{
    public class DimmingMathTests
    {
        [Theory]
        [InlineData(-10, 90, 0)]
        [InlineData(0, 90, 0)]
        [InlineData(45, 90, 45)]
        [InlineData(90, 90, 90)]
        [InlineData(100, 90, 90)]
        [InlineData(95, 95, 95)]
        [InlineData(100, 5, 10)] // effective max floor is 10
        public void ClampDimPercent_RespectsBoundsAndSafetyCap(int input, int cap, int expected)
        {
            Assert.Equal(expected, DimmingMath.ClampDimPercent(input, cap));
        }

        [Fact]
        public void GetTintMultipliers_NeutralAndPresetsWork()
        {
            DimmingMath.GetTintMultipliers("000000", 80, out double nr, out double ng, out double nb);
            Assert.Equal(1.0, nr);
            Assert.Equal(1.0, ng);
            Assert.Equal(1.0, nb);

            DimmingMath.GetTintMultipliers("2B1300", 100, out double wr, out double wg, out double wb);
            Assert.Equal(1.0, wr, 3);
            Assert.Equal(0.78, wg, 3);
            Assert.Equal(0.50, wb, 3);
            Assert.True(wr > wg && wg > wb);

            // Custom hex fallback
            DimmingMath.GetTintMultipliers("FF8040", 50, out double cr, out double cg, out double cb);
            Assert.Equal(1.0, cr, 3);
            Assert.True(cr > cg && cg > cb);
        }

        [Theory]
        [InlineData(256)]
        [InlineData(1024)]
        public void ApplyGammaTransform_PreservesOriginalLutAndScalesWarmCorrectly(int lutSize)
        {
            var baseR = new ushort[lutSize];
            var baseG = new ushort[lutSize];
            var baseB = new ushort[lutSize];

            for (int i = 0; i < lutSize; i++)
            {
                ushort val = (ushort)Math.Round(i * 65535.0 / (lutSize - 1));
                baseR[i] = val;
                baseG[i] = val;
                baseB[i] = val;
            }

            // Disabled -> identical copy of original ICC/calibrated LUT
            DimmingMath.ApplyGammaTransform(
                baseR, baseG, baseB,
                enabled: false, dimPercent: 50, hexTint: "2B1300", tintIntensity: 80, maxCap: 90,
                out ushort[] offR, out ushort[] offG, out ushort[] offB);
            Assert.Equal(baseR, offR);
            Assert.Equal(baseG, offG);
            Assert.Equal(baseB, offB);

            // 40% dim + Warm Amber (2B1300) at 100% intensity -> monotonic and red > green > blue
            DimmingMath.ApplyGammaTransform(
                baseR, baseG, baseB,
                enabled: true, dimPercent: 40, hexTint: "2B1300", tintIntensity: 100, maxCap: 90,
                out ushort[] onR, out ushort[] onG, out ushort[] onB);

            int mid = lutSize / 2;
            Assert.True(onR[mid] < baseR[mid]);
            Assert.True(onR[mid] > onG[mid]);
            Assert.True(onG[mid] > onB[mid]);

            for (int i = 1; i < lutSize; i++)
            {
                Assert.True(onR[i] >= onR[i - 1]);
                Assert.True(onG[i] >= onG[i - 1]);
                Assert.True(onB[i] >= onB[i - 1]);
            }
        }

        [Fact]
        public void ApplyMasterChange_SupportsAbsoluteAndProportionalModes()
        {
            var settings = new AppSettings();
            settings.Screens.Add(new ScreenSettings { Id = 1, DeviceName = "eDP-1", Dim = 20, On = true });
            settings.Screens.Add(new ScreenSettings { Id = 2, DeviceName = "HDMI-1", Dim = 50, On = true });

            // Proportional mode preserves offset (+15 delta)
            settings.Master.Proportional = true;
            settings.Master.Dim = 35;
            DimmingMath.ApplyMasterChange(settings, masterOn: true, newMasterDim: 50, previousMasterDim: 35);
            Assert.Equal(50, settings.Master.Dim);
            Assert.Equal(35, settings.Screens[0].Dim);
            Assert.Equal(65, settings.Screens[1].Dim);

            // Clamping at MaxDimPercent (90)
            DimmingMath.ApplyMasterChange(settings, masterOn: true, newMasterDim: 90, previousMasterDim: 50);
            Assert.Equal(75, settings.Screens[0].Dim);
            Assert.Equal(90, settings.Screens[1].Dim);

            // Non-proportional mode syncs all screens to exact Master value
            settings.Master.Proportional = false;
            DimmingMath.ApplyMasterChange(settings, masterOn: true, newMasterDim: 40, previousMasterDim: 90);
            Assert.Equal(40, settings.Screens[0].Dim);
            Assert.Equal(40, settings.Screens[1].Dim);
        }

        [Fact]
        public void ApplyQuickPreset_AppliesAbsoluteValueEvenWhenProportionalEnabled()
        {
            var settings = new AppSettings();
            settings.Master.Proportional = true;
            settings.Master.Dim = 30;
            settings.Screens.Add(new ScreenSettings { Id = 1, DeviceName = "eDP-1", Dim = 15, On = false });
            settings.Screens.Add(new ScreenSettings { Id = 2, DeviceName = "HDMI-1", Dim = 70, On = true });

            DimmingMath.ApplyQuickPreset(settings, 50);
            Assert.True(settings.Master.On);
            Assert.Equal(50, settings.Master.Dim);
            Assert.True(settings.Screens[0].On);
            Assert.Equal(50, settings.Screens[0].Dim);
            Assert.True(settings.Screens[1].On);
            Assert.Equal(50, settings.Screens[1].Dim);

            // Off (0%) disables overlays while preserving last active Dim level
            DimmingMath.ApplyQuickPreset(settings, 0);
            Assert.False(settings.Master.On);
            Assert.False(settings.Screens[0].On);
            Assert.False(settings.Screens[1].On);
            Assert.Equal(50, settings.Screens[0].Dim);
        }

        [Fact]
        public void CountActiveBehaviorSettings_CountsCorrectly()
        {
            var settings = new AppSettings();
            // Defaults: CloseHidesToTray=true, ShowInTaskbar=true (+ Windows defaults: ExcludeFromCapture, AvoidTaskbarAutoHideLock, NonDimmableWindow, MovableWindow)
            Assert.Equal(2, DimmingMath.CountActiveBehaviorSettings(settings, includeWindowsOnly: false));
            Assert.Equal(6, DimmingMath.CountActiveBehaviorSettings(settings, includeWindowsOnly: true));

            settings.AutoRun = true;
            settings.Master.Proportional = true;
            Assert.Equal(4, DimmingMath.CountActiveBehaviorSettings(settings, includeWindowsOnly: false));
            Assert.Equal(8, DimmingMath.CountActiveBehaviorSettings(settings, includeWindowsOnly: true));
        }

        [Fact]
        public void ToWin32ColorRef_EncodesBbGgRrByteOrder()
        {
            Assert.Equal((uint)0x0000132B, DimmingMath.ToWin32ColorRef(0x2B, 0x13, 0x00));
            Assert.Equal((uint)0x00332211, DimmingMath.ToWin32ColorRef(0x11, 0x22, 0x33));
            Assert.Equal((uint)0x00000000, DimmingMath.ToWin32ColorRef(0x00, 0x00, 0x00));
        }

        [Fact]
        public void ComputeOverlayColorAndAlpha_HandlesDisabledNeutralAndWarmTint()
        {
            DimmingMath.ComputeOverlayColorAndAlpha(
                enabled: false, dimPercent: 60, hexTint: "2B1300", tintIntensity: 60, maxCap: 90,
                out byte dr, out byte dg, out byte db, out byte da);
            Assert.Equal(0, da);
            Assert.Equal(0, dr);

            DimmingMath.ComputeOverlayColorAndAlpha(
                enabled: true, dimPercent: 50, hexTint: "000000", tintIntensity: 60, maxCap: 90,
                out byte nr, out byte ng, out byte nb, out byte na);
            Assert.Equal(0, nr);
            Assert.Equal(0, ng);
            Assert.Equal(0, nb);
            Assert.Equal(128, na);

            DimmingMath.ComputeOverlayColorAndAlpha(
                enabled: true, dimPercent: 100, hexTint: "2B1300", tintIntensity: 80, maxCap: 90,
                out byte wr, out byte wg, out byte wb, out byte wa);
            Assert.Equal((byte)Math.Round(0.90 * 255), wa);
            Assert.True(wr > wg && wg > wb);
        }

        [Fact]
        public void IsWindowVisibleOnMonitors_DetectsOffScreenAndPartialOverlap()
        {
            var monitors = new List<MonitorDescriptor>
            {
                new MonitorDescriptor { X = 0, Y = 0, Width = 1920, Height = 1080 },
                new MonitorDescriptor { X = 1920, Y = 0, Width = 2560, Height = 1080 }
            };

            Assert.True(DimmingMath.IsWindowVisibleOnMonitors(100, 100, 386, 468, monitors));
            Assert.True(DimmingMath.IsWindowVisibleOnMonitors(2200, 300, 386, 468, monitors));
            // Partially hanging off edge with >= 40px visible remains visible
            Assert.True(DimmingMath.IsWindowVisibleOnMonitors(-300, 100, 386, 468, monitors));

            // Simulate unplugging secondary monitor (2560x1080 at X=1920): saved window is now off-screen
            var singleMonitor = new List<MonitorDescriptor> { monitors[0] };
            Assert.False(DimmingMath.IsWindowVisibleOnMonitors(2200, 300, 386, 468, singleMonitor));
            // Only 10px visible (< 40px threshold) should reset
            Assert.False(DimmingMath.IsWindowVisibleOnMonitors(1910, 300, 386, 468, singleMonitor));
        }

        [Fact]
        public void GetNextMaxDimCap_CyclesAcrossSafetyLimits()
        {
            Assert.Equal(80, DimmingMath.GetNextMaxDimCap(95));
            Assert.Equal(85, DimmingMath.GetNextMaxDimCap(80));
            Assert.Equal(90, DimmingMath.GetNextMaxDimCap(85));
            Assert.Equal(95, DimmingMath.GetNextMaxDimCap(90));
            Assert.Equal(90, DimmingMath.GetNextMaxDimCap(77));
        }

        [Fact]
        public void ApplyGammaTransform_PreservesNonLinearIccCurveOnRestore()
        {
            var iccR = new ushort[256];
            var iccG = new ushort[256];
            var iccB = new ushort[256];
            for (int i = 0; i < 256; i++)
            {
                double norm = i / 255.0;
                iccR[i] = (ushort)Math.Round(Math.Pow(norm, 1.15) * 65535.0);
                iccG[i] = (ushort)Math.Round(Math.Pow(norm, 1.05) * 65535.0);
                iccB[i] = (ushort)Math.Round(Math.Pow(norm, 0.95) * 65535.0);
            }

            DimmingMath.ApplyGammaTransform(
                iccR, iccG, iccB,
                enabled: true, dimPercent: 30, hexTint: "000000", tintIntensity: 60, maxCap: 45,
                out ushort[] dimR, out ushort[] dimG, out ushort[] dimB);
            Assert.True(dimR[128] < iccR[128]);
            Assert.InRange(dimR[128] / (double)iccR[128], 0.68, 0.72);

            DimmingMath.ApplyGammaTransform(
                iccR, iccG, iccB,
                enabled: false, dimPercent: 30, hexTint: "000000", tintIntensity: 60, maxCap: 45,
                out ushort[] restR, out ushort[] restG, out ushort[] restB);
            Assert.Equal(iccR, restR);
            Assert.Equal(iccG, restG);
            Assert.Equal(iccB, restB);
        }
    }
}
