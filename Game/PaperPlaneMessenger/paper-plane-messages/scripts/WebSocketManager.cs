using Godot;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;

public partial class WebSocketManager : Node
{
    private const string SERVER_URL = "ws://192.168.2.151:8000/ws/";// "ws://127.0.0.1:8000/ws/";

    public ClientWebSocket Ws { get; private set; } = new ClientWebSocket();

    public List<OnlinePlayer> OnlineSpieler { get; private set; } = new();

    public class OnlinePlayer
    {
        public string UserId { get; set; }
        public string Username { get; set; }
    }

    [Signal] public delegate void BildErhaltenEventHandler(string senderName, ImageTexture texture, float positionY, float velocityX, float velocityY, float rotationZ);
    [Signal] public delegate void VerbundenEventHandler();
    [Signal] public delegate void OnlineListeAktualisiertEventHandler();



   


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
              

                var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                if (root.GetProperty("type").GetString() == "image")
                {
                    var bytes = System.Convert.FromBase64String(
                        root.GetProperty("image_data").GetString());
                    var image = new Image();
                    image.LoadPngFromBuffer(bytes);
                    var texture = ImageTexture.CreateFromImage(image);

                    float posY = root.GetProperty("position_y").GetSingle();
                    float velX = root.GetProperty("velocity_x").GetSingle();
                    float velY = root.GetProperty("velocity_y").GetSingle();
                    float rotZ = root.GetProperty("rotation_z").GetSingle();
                    string sender = root.GetProperty("sender").GetString();

                    EmitSignal(SignalName.BildErhalten, sender, texture, posY, velX, velY, rotZ);
                }
            }
            catch (System.Exception e)
            {
                GD.Print("WebSocket Fehler: " + e.Message);
                break;
            }
        }
    }

    public async void SendeBild(string targetId, Image image, Vector3 position, Vector3 velocity, Vector3 rotation)
    {
        if (Ws.State != WebSocketState.Open) return;

        var bytes = image.SavePngToBuffer();
        var base64 = System.Convert.ToBase64String(bytes);

        var message = JsonSerializer.Serialize(new
        {
            target_id = targetId,
            image_data = base64,
            position_y = position.Y,
            velocity_x = velocity.X,
            velocity_y = velocity.Y,
            rotation_z = rotation.Z
        });


        var buffer = Encoding.UTF8.GetBytes(message);
        await Ws.SendAsync(
            new System.ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }


    // DUMMY - später implementieren
    public void FavoritHinzufügen(string username)
    {
        // TODO
    }

    // DUMMY - später implementieren
    private void AktualisiereOnlineListe()
    {
        // TODO
    }
}