using Godot;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;

public partial class WebSocketManager : Node
{
    private const string SERVER_URL = "ws://127.0.0.1:8000/ws/";

    public ClientWebSocket Ws { get; private set; } = new ClientWebSocket();

    [Signal] public delegate void BildErhaltenEventHandler(string senderName, ImageTexture texture);
    [Signal] public delegate void VerbundenEventHandler();

    public async void Verbinden(string userId)
    {
        await Ws.ConnectAsync(
            new System.Uri(SERVER_URL + userId),
            CancellationToken.None);
        GD.Print("WebSocket verbunden!");
        EmitSignal(SignalName.Verbunden);
        _ = WarteAufNachrichten();
    }

    private async System.Threading.Tasks.Task WarteAufNachrichten()
    {
        var buffer = new byte[1024 * 1024 * 2];

        while (Ws.State == WebSocketState.Open)
        {
            try
            {
                var result = await Ws.ReceiveAsync(
                    new System.ArraySegment<byte>(buffer),
                    CancellationToken.None);

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                if (data["type"] == "image")
                {
                    var bytes = System.Convert.FromBase64String(data["image_data"]);
                    var image = new Image();
                    image.LoadPngFromBuffer(bytes);
                    var texture = ImageTexture.CreateFromImage(image);
                    EmitSignal(SignalName.BildErhalten, data["sender"], texture);
                }
            }
            catch (System.Exception e)
            {
                GD.Print("WebSocket Fehler: " + e.Message);
                break;
            }
        }
    }

    public async void SendeBild(string targetId, Image image)
    {
        if (Ws.State != WebSocketState.Open) return;

        var bytes = image.SavePngToBuffer();
        var base64 = System.Convert.ToBase64String(bytes);

        var message = JsonSerializer.Serialize(new
        {
            target_id = targetId,
            image_data = base64
        });

        var buffer = Encoding.UTF8.GetBytes(message);
        await Ws.SendAsync(
            new System.ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }
}