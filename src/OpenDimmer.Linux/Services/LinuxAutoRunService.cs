using System;
using System.IO;
using System.Text;

namespace OpenDimmer.Linux.Services
{
    public static class LinuxAutoRunService
    {
        private static string DesktopFilePath
        {
            get
            {
                string configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrEmpty(configDir))
                {
                    string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    configDir = Path.Combine(home, ".config");
                }
                return Path.Combine(configDir, "autostart", "opendimmer.desktop");
            }
        }

        public static void SetAutoRun(bool enabled)
        {
            try
            {
                string path = DesktopFilePath;
                if (!enabled)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    return;
                }

                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string? exe = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exe)) return;

                string execLine;
                if (Path.GetFileName(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                {
                    string dllPath = Path.Combine(AppContext.BaseDirectory, "OpenDimmer.dll");
                    execLine = $"\"{exe}\" \"{dllPath}\"";
                }
                else
                {
                    execLine = $"\"{exe}\"";
                }

                string content = string.Join("\n", new[]
                {
                    "[Desktop Entry]",
                    "Type=Application",
                    "Name=OpenDimmer",
                    "Comment=Multi-monitor brightness & Night Light controller",
                    "Exec=" + execLine,
                    "Terminal=false",
                    "X-GNOME-Autostart-enabled=true",
                    ""
                });

                File.WriteAllText(path, content, Encoding.UTF8);
            }
            catch
            {
                // Ignore XDG autostart write errors
            }
        }
    }
}
