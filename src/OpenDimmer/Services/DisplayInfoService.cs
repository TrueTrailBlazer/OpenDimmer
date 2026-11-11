using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
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
            string friendlyName = null;
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
                            friendlyName = ReadEdidMonitorName(hwid, instanceId);
                        }
                    }
                }
            }
            catch { }

            bool isInternalPanel = IsKnownInternalPanel(hwid, friendlyName);
            if (string.IsNullOrEmpty(friendlyName))
            {
                friendlyName = isInternalPanel
                    ? "Notebook Display"
                    : ("Display " + index);
            }
            else
            {
                friendlyName = FormatMonitorName(friendlyName);
            }

            string port = isInternalPanel ? ("eDP-" + index) : (screen.Primary ? ("HDMI-" + index) : ("DP-" + index));
            string role = screen.Primary ? "Primary" : "Extended";

            return new MonitorHardwareInfo
            {
                FriendlyName = friendlyName,
                PortBadge = port,
                Subtitle = port + " · " + role
            };
        }

        private static bool IsKnownInternalPanel(string hwid, string edidName)
        {
            if (!string.IsNullOrEmpty(edidName))
                return false;

            if (string.IsNullOrEmpty(hwid))
                return false;

            string u = hwid.ToUpperInvariant();
            return u.StartsWith("CMN") || u.StartsWith("AUO") || u.StartsWith("BOE")
                || u.StartsWith("LGD") || u.StartsWith("SDC") || u.StartsWith("SHP")
                || u.StartsWith("NVT") || u.StartsWith("IVO");
        }

        private static string FormatMonitorName(string raw)
        {
            string trimmed = raw.Trim();
            if (trimmed.Length == 0) return trimmed;

            // Keep short brand acronyms uppercase (LG, DELL, HP, AOC, ASUS, BENQ) and title-case rest
            string[] words = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i].ToUpperInvariant();
                if (w == "LG" || w == "HP" || w == "AOC" || w == "MSI" || w == "4K" || w == "HDR")
                {
                    words[i] = w;
                }
                else if (w.Length > 2)
                {
                    words[i] = w.Substring(0, 1) + w.Substring(1).ToLowerInvariant();
                }
            }
            return string.Join(" ", words);
        }

        private static string ReadEdidMonitorName(string hwid, string instanceId)
        {
            try
            {
                string regPath = @"SYSTEM\CurrentControlSet\Enum\DISPLAY\" + hwid + @"\" + instanceId + @"\Device Parameters";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath, false))
                {
                    if (key == null) return null;
                    byte[] edid = key.GetValue("EDID") as byte[];
                    if (edid == null || edid.Length < 128) return null;

                    int[] offsets = new[] { 54, 72, 90, 108 };
                    foreach (int off in offsets)
                    {
                        if (edid[off] == 0x00 && edid[off + 1] == 0x00 && edid[off + 2] == 0x00 && edid[off + 3] == 0xFC)
                        {
                            var sb = new StringBuilder();
                            for (int j = 5; j < 18; j++)
                            {
                                byte b = edid[off + j];
                                if (b == 0x0A || b == 0x00) break;
                                if (b >= 32 && b <= 126) sb.Append((char)b);
                            }
                            string res = sb.ToString().Trim();
                            if (res.Length > 0) return res;
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
