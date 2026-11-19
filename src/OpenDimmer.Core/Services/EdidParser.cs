using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDimmer.Services
{
    public static class EdidParser
    {
        public static string ParseMonitorName(byte[] edid)
        {
            if (edid == null || edid.Length < 128)
            {
                return null;
            }

            int[] offsets = new int[] { 54, 72, 90, 108 };
            for (int idx = 0; idx < offsets.Length; idx++)
            {
                int off = offsets[idx];
                if (off + 17 >= edid.Length)
                {
                    continue;
                }

                if (edid[off] == 0x00 && edid[off + 1] == 0x00 && edid[off + 2] == 0x00 && edid[off + 3] == 0xFC)
                {
                    var sb = new StringBuilder();
                    for (int j = 5; j < 18; j++)
                    {
                        byte b = edid[off + j];
                        if (b == 0x0A || b == 0x00)
                        {
                            break;
                        }
                        if (b >= 32 && b <= 126)
                        {
                            sb.Append((char)b);
                        }
                    }

                    string res = sb.ToString().Trim();
                    if (res.Length > 0)
                    {
                        return res;
                    }
                }
            }

            return null;
        }

        public static string ParseManufacturerPnpId(byte[] edid)
        {
            if (edid == null || edid.Length < 10)
            {
                return null;
            }

            int raw = (edid[8] << 8) | edid[9];
            int c1 = ((raw >> 10) & 0x1F) + 64;
            int c2 = ((raw >> 5) & 0x1F) + 64;
            int c3 = (raw & 0x1F) + 64;

            if (c1 < 'A' || c1 > 'Z' || c2 < 'A' || c2 > 'Z' || c3 < 'A' || c3 > 'Z')
            {
                return null;
            }

            return new string(new char[] { (char)c1, (char)c2, (char)c3 });
        }

        public static bool IsKnownInternalPanel(string hwidOrPnp, string edidName)
        {
            if (!string.IsNullOrEmpty(edidName))
            {
                return false;
            }

            if (string.IsNullOrEmpty(hwidOrPnp))
            {
                return false;
            }

            string u = hwidOrPnp.ToUpperInvariant();
            return u.StartsWith("CMN") || u.StartsWith("AUO") || u.StartsWith("BOE")
                || u.StartsWith("LGD") || u.StartsWith("SDC") || u.StartsWith("SHP")
                || u.StartsWith("NVT") || u.StartsWith("IVO");
        }

        public static string FormatMonitorName(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return string.Empty;
            }

            string trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                return trimmed;
            }

            string[] rawWords = trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var words = new List<string>(rawWords.Length);
            for (int i = 0; i < rawWords.Length; i++)
            {
                string w = rawWords[i].ToUpperInvariant();
                if (w == "LG" || w == "HP" || w == "AOC" || w == "MSI" || w == "4K" || w == "HDR")
                {
                    words.Add(w);
                }
                else if (w.Length > 2)
                {
                    words.Add(w.Substring(0, 1) + w.Substring(1).ToLowerInvariant());
                }
                else
                {
                    words.Add(rawWords[i]);
                }
            }

            return string.Join(" ", words.ToArray());
        }

        public static string ResolveFriendlyName(
            byte[] edid,
            string fallbackHwid,
            int index,
            out bool isInternalPanel)
        {
            string edidName = ParseMonitorName(edid);
            string pnp = ParseManufacturerPnpId(edid);
            string hwid = !string.IsNullOrEmpty(fallbackHwid) ? fallbackHwid : pnp;

            isInternalPanel = IsKnownInternalPanel(hwid, edidName);
            if (!string.IsNullOrEmpty(edidName))
            {
                return FormatMonitorName(edidName);
            }

            return isInternalPanel ? "Notebook Display" : ("Display " + index);
        }
    }
}
