namespace OpenDimmer.Models
{
    public sealed class MonitorDescriptor
    {
        public int Index { get; set; }
        public string DeviceName { get; set; }
        public string FriendlyName { get; set; }
        public string PortBadge { get; set; }
        public string Subtitle { get; set; }
        public bool IsPrimary { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public MonitorDescriptor()
        {
            DeviceName = string.Empty;
            FriendlyName = string.Empty;
            PortBadge = string.Empty;
            Subtitle = string.Empty;
        }
    }
}
