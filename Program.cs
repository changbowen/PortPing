using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace PortPing
{
    class PingResult
    {
        public bool Success { get; set; }
        public long LatencyMs { get; set; }
        public string Message { get; set; }
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

            while (true) {
                var result = CheckPort(host, port, timeout);
                Console.WriteLine($@"{(result.Success ? @"Connection succeeded. " : @"Connection failed. ")}{result.Message}");
                Thread.Sleep(1000);
            }
        }

        static PingResult CheckPort(string host, int port, int timeout)
        {
            var watch = new Stopwatch();
            var pingResult = new PingResult() { Success = false };

            string getTime() => watch.ElapsedMilliseconds.ToString().PadLeft(timeout.ToString().Length);
            TcpClient client = null;

            try {
                client = new TcpClient();
                watch.Restart();
                var task = client.ConnectAsync(host, port);
                if (task.Wait(timeout)) {//if fails within timeout, task.Wait still returns true.
                    if (client.Connected) {
                        watch.Stop();
                        pingResult.Success = true;
                        pingResult.Message = 
                            $@"Time: {getTime()}ms; " +
                            $@"TTL: {client.Client.Ttl,3}; " +
                            $@"Protocol: {client.Client.ProtocolType}; " +
                            $@"From: {client.Client.LocalEndPoint,21}; " +
                            $@"To: {client.Client.RemoteEndPoint,21}";
                    }
                    else
                        pingResult.Message = $@"Time: {getTime()}ms; Unknown error.";
                }
                else
                    pingResult.Message = @"Timed out.";
                watch.Stop();
            }
            catch (Exception ex) {
                watch.Stop();
                pingResult.Message = $@"Time: {getTime()}ms; {getAllMessages(ex)}";
            }
            finally {
                pingResult.LatencyMs = watch.ElapsedMilliseconds;
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
