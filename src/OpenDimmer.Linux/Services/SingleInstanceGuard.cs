using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OpenDimmer.Linux.Services
{
    public sealed class SingleInstanceGuard : IDisposable
    {
        private readonly string _socketPath;
        private readonly Socket? _serverSocket;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public bool IsPrimaryInstance { get; }

        public SingleInstanceGuard(Action onWakeup)
        {
            string runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") ?? Path.GetTempPath();
            _socketPath = Path.Combine(runtimeDir, "opendimmer-linux-ipc.sock");

            if (TrySignalExistingInstance(_socketPath))
            {
                IsPrimaryInstance = false;
                return;
            }

            try
            {
                if (File.Exists(_socketPath))
                {
                    File.Delete(_socketPath);
                }

                _serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                _serverSocket.Bind(new UnixDomainSocketEndPoint(_socketPath));
                _serverSocket.Listen(4);
                IsPrimaryInstance = true;

                var thread = new Thread(() => ListenLoop(onWakeup))
                {
                    IsBackground = true,
                    Name = "OpenDimmer_IPC_Listener"
                };
                thread.Start();
            }
            catch
            {
                // Fallback if socket creation fails: still allow running
                IsPrimaryInstance = true;
            }
        }

        private static bool TrySignalExistingInstance(string socketPath)
        {
            if (!File.Exists(socketPath)) return false;

            try
            {
                using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                client.Connect(new UnixDomainSocketEndPoint(socketPath));
                byte[] msg = Encoding.UTF8.GetBytes("SHOW\n");
                client.Send(msg);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ListenLoop(Action onWakeup)
        {
            if (_serverSocket == null) return;

            var buf = new byte[128];
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    using Socket conn = _serverSocket.Accept();
                    int n = conn.Receive(buf);
                    if (n > 0)
                    {
                        string cmd = Encoding.UTF8.GetString(buf, 0, n).Trim();
                        if (string.Equals(cmd, "SHOW", StringComparison.OrdinalIgnoreCase))
                        {
                            onWakeup();
                        }
                    }
                }
                catch
                {
                    if (_cts.IsCancellationRequested) break;
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _serverSocket?.Close(); } catch { }
            try { _serverSocket?.Dispose(); } catch { }
            if (IsPrimaryInstance)
            {
                try
                {
                    if (File.Exists(_socketPath))
                    {
                        File.Delete(_socketPath);
                    }
                }
                catch { }
            }
        }
    }
}
