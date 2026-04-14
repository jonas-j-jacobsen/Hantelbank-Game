using Godot;
using System.Net.Http;
using System.Net;

public partial class TestHttp : Node
{
    public override async void _Ready()
    {
        GD.Print("Starte Request...");

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

        var client = new System.Net.Http.HttpClient(handler);
        client.Timeout = System.TimeSpan.FromSeconds(10);
        try
        {
            var response = await client.GetAsync("https://api.studio-maus.de/ping");
            var text = await response.Content.ReadAsStringAsync();
            GD.Print($"Antwort: {text}");
        }
        catch (System.Exception e)
        {
            GD.Print($"Fehler: {e.Message}");
        }
    }
}