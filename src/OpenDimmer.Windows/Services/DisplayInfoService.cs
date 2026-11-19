using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace OpenDimmer.Services
{
    public sealed class MonitorHardwareInfo
    {
        public string FriendlyName { get; set; }
        public string PortBadge { get; set; }
        public string Subtitle { get; set; }
    }

    public static class DisplayInfoService
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayDevices(
            string lpDevice,
            uint iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice,
            uint dwFlags);

        public static MonitorHardwareInfo GetMonitorInfo(Screen screen, int index)
        {
            byte[] edid = null;
            string hwid = null;

            try
            {
                var dd = new DISPLAY_DEVICE();
                dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                if (EnumDisplayDevices(screen.DeviceName, 0, ref dd, 1))
                {
                    // Format: \\?\DISPLAY#GSM76F9#4&3b88f39d&0&UID4145#{guid}
                    if (!string.IsNullOrEmpty(dd.DeviceID))
                    {
                        string[] parts = dd.DeviceID.Split('#');
                        if (parts.Length >= 3)
                        {
                            hwid = parts[1];
                            string instanceId = parts[2];
                            edid = ReadRawEdid(hwid, instanceId);
                        }
                    }
                }
            }
            catch { }

            bool isInternalPanel;
            string friendlyName = EdidParser.ResolveFriendlyName(edid, hwid, index, out isInternalPanel);

            string port = isInternalPanel ? ("eDP-" + index) : (screen.Primary ? ("HDMI-" + index) : ("DP-" + index));
            string role = screen.Primary ? "Primary" : "Extended";

            return new MonitorHardwareInfo
            {
                FriendlyName = friendlyName,
                PortBadge = port,
                Subtitle = port + " \u00B7 " + role
            };
        }

        private static byte[] ReadRawEdid(string hwid, string instanceId)
        {
            try
            {
                string regPath = @"SYSTEM\CurrentControlSet\Enum\DISPLAY\" + hwid + @"\" + instanceId + @"\Device Parameters";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath, false))
                {
                    if (key == null) return null;
                    return key.GetValue("EDID") as byte[];
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
