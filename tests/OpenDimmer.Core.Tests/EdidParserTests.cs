using System;
using System.Text;
using OpenDimmer.Services;
using Xunit;

namespace OpenDimmer.Core.Tests
{
    public class EdidParserTests
    {
        private static byte[] BuildSyntheticEdid(string pnpId, string? monitorName)
        {
            var edid = new byte[128];
            edid[0] = 0x00;
            for (int i = 1; i <= 6; i++) edid[i] = 0xFF;
            edid[7] = 0x00;

            int c1 = (pnpId[0] - 64) & 0x1F;
            int c2 = (pnpId[1] - 64) & 0x1F;
            int c3 = (pnpId[2] - 64) & 0x1F;
            int packed = (c1 << 10) | (c2 << 5) | c3;
            edid[8] = (byte)((packed >> 8) & 0xFF);
            edid[9] = (byte)(packed & 0xFF);

            if (!string.IsNullOrEmpty(monitorName))
            {
                int off = 54;
                edid[off + 3] = 0xFC;
                byte[] ascii = Encoding.ASCII.GetBytes(monitorName);
                int len = Math.Min(ascii.Length, 13);
                Array.Copy(ascii, 0, edid, off + 5, len);
                if (len < 13) edid[off + 5 + len] = 0x0A;
            }

            return edid;
        }

        [Fact]
        public void ResolveFriendlyName_ParsesLgUltrawideEdid()
        {
            byte[] edid = BuildSyntheticEdid("GSM", "LG ULTRAWIDE");
            string name = EdidParser.ResolveFriendlyName(edid, null, 1, out bool isInternal);

            Assert.Equal("LG Ultrawide", name);
            Assert.False(isInternal);
        }

        [Fact]
        public void ResolveFriendlyName_DetectsBoeInternalPanelWithoutName()
        {
            byte[] edid = BuildSyntheticEdid("BOE", null);
            string name = EdidParser.ResolveFriendlyName(edid, null, 2, out bool isInternal);

            Assert.Equal("Notebook Display", name);
            Assert.True(isInternal);
        }

        [Fact]
        public void ResolveFriendlyName_FallsBackToDisplayIndexOnNullEdid()
        {
            string name = EdidParser.ResolveFriendlyName(null, null, 3, out bool isInternal);

            Assert.Equal("Display 3", name);
            Assert.False(isInternal);
        }
    }
}
