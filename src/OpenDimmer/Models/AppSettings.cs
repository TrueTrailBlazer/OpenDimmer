using System;
using System.Collections.Generic;

namespace OpenDimmer.Models
{
    public sealed class WindowPosition
    {
        public double? Left { get; set; }
        public double? Top { get; set; }
    }

    public sealed class MasterSettings
    {
        public bool On { get; set; }
        public int Dim { get; set; }
        public bool Proportional { get; set; }

        public MasterSettings()
        {
            Dim = 30;
            Proportional = true;
        }
    }

    public sealed class ScreenSettings
    {
        public int Id { get; set; }
        public string DeviceName { get; set; }
        public bool On { get; set; }
        public int Dim { get; set; }
        public string Tint { get; set; }

        public ScreenSettings()
        {
            DeviceName = string.Empty;
            Dim = 30;
            Tint = "000000";
        }
    }

    public sealed class AppSettings
    {
        public WindowPosition Position { get; set; }
        public bool ShowIds { get; set; }
        public bool ShowBoundaries { get; set; }
        public bool AutoRun { get; set; }
        public bool MovableWindow { get; set; }
        public bool StartMinimised { get; set; }
        public bool NonDimmableWindow { get; set; }
        public bool CloseHidesToTray { get; set; }
        public bool ShowInTaskbar { get; set; }
        public bool UseHardwareGamma { get; set; }
        public bool AvoidTaskbarAutoHideLock { get; set; }
        public MasterSettings Master { get; set; }
        public List<ScreenSettings> Screens { get; set; }
        public string LastRun { get; set; }

        public AppSettings()
        {
            Position = new WindowPosition();
            MovableWindow = true;
            NonDimmableWindow = true;
            CloseHidesToTray = true;
            ShowInTaskbar = true;
            UseHardwareGamma = false;
            AvoidTaskbarAutoHideLock = true;
            Master = new MasterSettings();
            Screens = new List<ScreenSettings>();
            LastRun = string.Empty;
        }
    }
}
