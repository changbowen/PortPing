using System.Net.Sockets;
using System.Net;

namespace PortPing
{
    public class PingResult
    {
        public bool Success { get; set; }
        public long LatencyMs { get; set; }
        public int Ttl { get; set; }
        public ProtocolType Protocol { get; set; }
        public EndPoint Source { get; set; }
        public EndPoint Destination { get; set; }
        public string ErrorMsg { get; set; }
    }

    public static class Helpers
    {
        public static string GetAllMessages(this Exception ex) =>
            ex.InnerException == null ? ex.Message : $@"{ex.Message} ({ex.InnerException.GetAllMessages()})";

        public static PingResult CheckPort(string host, int port, IPEndPoint source = null, int timeout = 5000)
        {
            var watch = Program.Watch;
            var pingResult = new PingResult() { Success = false };
            TcpClient client = null;

            try {
                client = source == null ? new TcpClient() : new TcpClient(source);
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
                pingResult.ErrorMsg = GetAllMessages(ex);
            }
            finally {
                pingResult.LatencyMs = watch.ElapsedMilliseconds;
                try {
                    pingResult.Ttl = client.Client.Ttl;
                    pingResult.Protocol = client.Client.ProtocolType;
                    pingResult.Source = client.Client.LocalEndPoint;
                }
                catch { }
                client?.Close();
            }

            return pingResult;
        }
    }
}
