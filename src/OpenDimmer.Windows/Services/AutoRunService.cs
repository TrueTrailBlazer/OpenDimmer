using System;
using System.Reflection;
using Microsoft.Win32;

namespace OpenDimmer.Services
{
    public static class AutoRunService
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "OpenDimmer";

        public static void SetAutoRun(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key == null) return;

                    if (enabled)
                    {
                        string exePath = Assembly.GetExecutingAssembly().Location;
                        key.SetValue(ValueName, "\"" + exePath + "\"");
                    }
                    else
                    {
                        if (key.GetValue(ValueName) != null)
                        {
                            key.DeleteValue(ValueName, false);
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry permission errors
            }
        }
    }
}
