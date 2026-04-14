using System;
using System.Net.Http;

/// <summary>
/// Zentrale Factory für HttpClient-Instanzen, die explizit IPv4 nutzen.
/// Grund: .NET's HttpClient versucht per Default zuerst IPv6 und fällt erst nach
/// langem Timeout auf IPv4 zurück. Wenn der Client ein kaputtes IPv6-Routing hat
/// (z.B. Telekom mit defektem Router), führt das zu 10+ Sekunden Wartezeit pro Request.
/// Diese Factory erzwingt IPv4 und umgeht das Problem.
/// </summary>
public static class HttpClientFactory
{
    public static HttpClient CreateIPv4Client(TimeSpan? timeout = null)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new System.Net.Sockets.Socket(
                    System.Net.Sockets.AddressFamily.InterNetwork,
                    System.Net.Sockets.SocketType.Stream,
                    System.Net.Sockets.ProtocolType.Tcp);
                socket.NoDelay = true;
                try
                {
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                    return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        return new HttpClient(handler)
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(10)
        };
    }
}