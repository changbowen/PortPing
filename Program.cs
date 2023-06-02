using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;
using static PortPing.Helpers;

namespace PortPing
{
    internal class Program
    {
        internal static Stopwatch Watch = new();

        static void Main(string[] args)
        {
            if (args == null || args.Length == 0 || !args[0].Contains(':')) {
                Console.WriteLine(UsageInfo);
                return;
            }

            // parse arguments
            string arg_host;
            int arg_port;
            // optional arguments
            var arg_timeout = 5000;
            var arg_interval = 1000;
            IPEndPoint arg_source = null;

            try {
                string[] _sa; int _i;

                _sa = args[0].Split(':');
                arg_host = _sa[0];
                arg_port = int.Parse(_sa[1]);

                _i = Array.IndexOf(args, @"-t");
                if (_i > 0) arg_timeout = int.Parse(args[_i + 1]);

                _i = Array.IndexOf(args, @"-i");
                if (_i > 0) arg_interval = int.Parse(args[_i + 1]);

                _i = Array.IndexOf(args, @"-s");
                if (_i > 0) {
                    _sa = args[_i + 1].Split(':');
                    arg_source = new IPEndPoint(IPAddress.Parse(_sa[0]), _sa.Length > 1 ? int.Parse(_sa[1]) : 0);
                    if (!NetworkInterface.GetAllNetworkInterfaces().Where(ii => ii.OperationalStatus == OperationalStatus.Up)
                        .SelectMany(ii => ii.GetIPProperties().UnicastAddresses).Any(ii => ii.Address.Equals(arg_source.Address)))
                        throw new ArgumentException(@"Source address is invalid.");
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error parsing arguments.\r\n{ex.GetAllMessages()}");
                return;
            }

            string formatResult(PingResult pingResult) =>
                $@"Time: {pingResult.LatencyMs.ToString().PadLeft(arg_timeout.ToString().Length)} ms; " +
                $@"{(pingResult.Ttl != default ? $@"TTL: {pingResult.Ttl,3}; " : null)}" +
                $@"{(pingResult.Protocol != default ? $@"Protocol: {pingResult.Protocol}; " : null)}" +
                $@"{(pingResult.Source != null ? $@"From: {pingResult.Source,21}; " : null)}" +
                $@"{(pingResult.Destination != null ? $@"To: {pingResult.Destination,21}; " : null)}";

            int received = 0, lost = 0;
            float? minMs = null, maxMs = null, avgLatMs = null;
            float totalLatSec = 0f;
            bool run = true;

            Console.CancelKeyPress += (sender, e) => { e.Cancel = true; run = false; };

            while (run) {
                var result = CheckPort(arg_host, arg_port, arg_source, arg_timeout);
                if (result.Success) {
                    Console.WriteLine($"Connection succeeded. {formatResult(result)}");
                    received += 1;
                    minMs = Math.Min(result.LatencyMs, minMs ?? float.PositiveInfinity);
                    maxMs = Math.Max(result.LatencyMs, maxMs ?? 0f);
                    totalLatSec += result.LatencyMs / 1000f;
                    avgLatMs = totalLatSec / received * 1000f;
                }
                else {
                    Console.WriteLine($"Connection failed. {formatResult(result)}Error: {result.ErrorMsg}");
                    lost += 1;
                }

                Thread.Sleep(arg_interval);
            }

            Console.WriteLine($@"
Ping statistics to {arg_host} on port {arg_port}{(arg_source != null ? $@" from {arg_source}" : string.Empty)}:
    Sent: {received + lost}, Received: {received}, Lost: {lost}
    Minimum: {minMs ?? 0:0.#} ms, Maximum: {maxMs ?? 0:0.#} ms, Average: {avgLatMs ?? 0:0.#} ms
");
        }

        private const string UsageInfo = @"
Usage: portping.exe host:port [-t timeout] [-s source[:port]]
    host:port           The hostname / IP address and port to connect to.
    -t timeout          Timeout in milliseconds to wait for each ping. Default is 5000ms.
    -s source[:port]    IP address of the source interface with optional port to use.
    -i interval         Interval in milliseconds to wait between each ping. Default is 1000ms.
";
    }

}