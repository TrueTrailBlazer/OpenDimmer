using System;
using System.Runtime.InteropServices;

namespace OpenDimmer.Linux.Services
{
    internal static class X11Native
    {
        private const string LibX11 = "libX11.so.6";
        private const string LibXrandr = "libXrandr.so.2";

        public const ushort RR_Connected = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct XRRScreenResources
        {
            public nuint timestamp;
            public nuint configTimestamp;
            public int ncrtc;
            public IntPtr crtcs;
            public int noutput;
            public IntPtr outputs;
            public int nmode;
            public IntPtr modes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XRROutputInfo
        {
            public nuint timestamp;
            public nuint crtc;
            public IntPtr name;
            public int nameLen;
            public nuint mm_width;
            public nuint mm_height;
            public ushort connection;
            public ushort subpixel_order;
            public int ncrtc;
            public IntPtr crtcs;
            public int nclone;
            public IntPtr clones;
            public int nmode;
            public int npreferred;
            public IntPtr modes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XRRCrtcInfo
        {
            public nuint timestamp;
            public int x;
            public int y;
            public uint width;
            public uint height;
            public nuint mode;
            public int noutput;
            public IntPtr outputs;
            public ushort rotation;
            public int npossible;
            public IntPtr possible;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XRRCrtcGamma
        {
            public int size;
            public IntPtr red;
            public IntPtr green;
            public IntPtr blue;
        }

        [DllImport(LibX11)]
        public static extern IntPtr XOpenDisplay(string? displayName);

        [DllImport(LibX11)]
        public static extern int XCloseDisplay(IntPtr display);

        [DllImport(LibX11)]
        public static extern nuint XDefaultRootWindow(IntPtr display);

        [DllImport(LibX11)]
        public static extern int XFlush(IntPtr display);

        [DllImport(LibXrandr)]
        public static extern IntPtr XRRGetScreenResourcesCurrent(IntPtr display, nuint window);

        [DllImport(LibXrandr)]
        public static extern void XRRFreeScreenResources(IntPtr resources);

        [DllImport(LibXrandr)]
        public static extern nuint XRRGetOutputPrimary(IntPtr display, nuint window);

        [DllImport(LibXrandr)]
        public static extern IntPtr XRRGetOutputInfo(IntPtr display, IntPtr resources, nuint output);

        [DllImport(LibXrandr)]
        public static extern void XRRFreeOutputInfo(IntPtr outputInfo);

        [DllImport(LibXrandr)]
        public static extern IntPtr XRRGetCrtcInfo(IntPtr display, IntPtr resources, nuint crtc);

        [DllImport(LibXrandr)]
        public static extern void XRRFreeCrtcInfo(IntPtr crtcInfo);

        [DllImport(LibXrandr)]
        public static extern int XRRGetCrtcGammaSize(IntPtr display, nuint crtc);

        [DllImport(LibXrandr)]
        public static extern IntPtr XRRGetCrtcGamma(IntPtr display, nuint crtc);

        [DllImport(LibXrandr)]
        public static extern IntPtr XRRAllocGamma(int size);

        [DllImport(LibXrandr)]
        public static extern void XRRSetCrtcGamma(IntPtr display, nuint crtc, IntPtr gamma);

        [DllImport(LibXrandr)]
        public static extern void XRRFreeGamma(IntPtr gamma);
    }
}
