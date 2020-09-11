using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Net;

namespace PortPing
{
    class PingResult
    {
        public bool Success { get; set; }
        public long LatencyMs { get; set; }
        public int Ttl { get; set; }
        public ProtocolType Protocol { get; set; }
        public EndPoint Source { get; set; }
        public EndPoint Destination { get; set; }
        public string ErrorMsg { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args?.Length < 2) {
                Console.Write(UsageInfo);
                return;
            }

            var host = args[0];
            var port = int.Parse(args[1]);
            var i = Array.IndexOf(args, @"-t");
            var timeout = i == -1 ? 2000 : int.Parse(args[i + 1]);

            string formatResult(PingResult pingResult) =>
                $@"Time: {pingResult.LatencyMs.ToString().PadLeft(timeout.ToString().Length)}ms; " +
                $@"TTL: {pingResult.Ttl,3}; " +
                $@"Protocol: {pingResult.Protocol}; " +
                $@"{(pingResult.Source != null ? $@"From: {pingResult.Source,21}; " : null)}" +
                $@"{(pingResult.Destination != null ? $@"To: {pingResult.Destination,21}; " : null)}";

            while (true) {
                var result = CheckPort(host, port, timeout);
                if (result.Success)
                    Console.WriteLine($"Connection succeeded. {formatResult(result)}");
                else
                    Console.WriteLine($"Connection failed. {formatResult(result)}Error: {result.ErrorMsg}");

                Thread.Sleep(1000);
            }
        }

        static PingResult CheckPort(string host, int port, int timeout)
        {
            var watch = new Stopwatch();
            var pingResult = new PingResult() { Success = false };
            var client = new TcpClient();

            try {
                watch.Restart();
                var task = client.ConnectAsync(host, port);
                if (task.Wait(timeout)) {//if fails within timeout, task.Wait still returns true.
                    if (client.Connected) {
                        watch.Stop();
                        pingResult.Success = true;
                        pingResult.Destination = client.Client.RemoteEndPoint;
                    }
                    else
                        pingResult.ErrorMsg = @"Unknown error.";
                }
                else
                    pingResult.ErrorMsg = @"Timed out.";
                watch.Stop();
            }
            catch (Exception ex) {
                watch.Stop();
                pingResult.ErrorMsg = getAllMessages(ex);
            }
            finally {
                pingResult.LatencyMs = watch.ElapsedMilliseconds;
                try {
                    pingResult.Ttl = client.Client.Ttl;
                    pingResult.Protocol = client.Client.ProtocolType;
                    pingResult.Source = client.Client.LocalEndPoint;
                }
                catch { }
                client.Close();
            }

            return pingResult;
        }

        private static string getAllMessages(Exception outer)
        {
            if (outer.InnerException == null)
                return $@"{outer.Message}";
            else
                return $@"{outer.Message} ({getAllMessages(outer.InnerException)})";
        }

        private const string UsageInfo = @"
Usage: portping.exe host port [-t timeout]
    host    The hostname or IP address to connect to.
    port    Port number to connect to.
    -t      Timeout in milliseconds to wait for each ping. Default is 2000ms.
";
    }
}
