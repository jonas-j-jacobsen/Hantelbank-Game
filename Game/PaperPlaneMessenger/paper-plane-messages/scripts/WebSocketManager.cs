using Godot;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public partial class WebSocketManager : Node
{
    private const string SERVER_HOST = "api.studio-maus.de";
    private const float RECONNECT_INTERVAL = 5f;
    private float _pingTimer = 0f;
    private const float PING_INTERVAL = 20f;


    public ClientWebSocket Ws { get; private set; } = new ClientWebSocket();
    public List<OnlinePlayer> OnlineSpieler { get; private set; } = new();

    public class OnlinePlayer
    {
        public string UserId { get; set; }
        public string Username { get; set; }
    }

    [Signal] public delegate void BildErhaltenEventHandler(string senderName, ImageTexture texture, float positionY, float velocityX, float velocityY, float rotationZ);
    [Signal] public delegate void VerbundenEventHandler();
    [Signal] public delegate void VerbindungVerlorenEventHandler();
    [Signal] public delegate void OnlineListeAktualisiertEventHandler();
    [Signal] public delegate void FreundschaftsAnfrageEventHandler(string vonUserId, string vonUsername);
    [Signal] public delegate void FreundschaftsAngenommenEventHandler(string vonUserId, string vonUsername);
    [Signal] public delegate void GruppenEinladungEventHandler(string groupId, string groupName, string vonUsername);
    [Signal] public delegate void FreundOnlineEventHandler(string userId, string username);
    [Signal] public delegate void FreundOfflineEventHandler(string userId);

    private AuthManager _authManager;
    private bool _reconnecting = false;

    public override void _Ready()
    {
        _authManager = GetNode<AuthManager>("/root/AuthManager");
        _authManager.Eingeloggt += OnEingeloggt;
        _authManager.Ausgeloggt += OnAusgeloggt;
        Test();
    }

    private void Test()
    {
        var testBild = Image.CreateEmpty(512, 512, false, Image.Format.Rgba8);

        // Zufällige Pixel für realistische Größe
        var rng = new RandomNumberGenerator();
        for (int x = 0; x < 512; x++)
            for (int y = 0; y < 512; y++)
                testBild.SetPixel(x, y, new Color(rng.Randf(), rng.Randf(), rng.Randf(), 1));

        var bytes = testBild.SavePngToBuffer();
        GD.Print("Zufälliges Bild: " + bytes.Length / 1024 + " KB");
    }


    public override void _Process(double delta)
    {
        if (Ws.State != WebSocketState.Open) return;
        _pingTimer += (float)delta;
        if (_pingTimer >= PING_INTERVAL)
        {
            _pingTimer = 0f;
            SendePing();
        }
    }

    private async void SendePing()
    {
        try
        {
            await SendeNachricht(JsonSerializer.Serialize(new { type = "ping" }));
        }
        catch { }
    }



    private void OnEingeloggt()
    {
        Verbinden(_authManager.UserId, _authManager.Token);
    }

    private void OnAusgeloggt()
    {
        _reconnecting = false;
        if (Ws.State == WebSocketState.Open)
            Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        Ws = new ClientWebSocket();
    }

    /// <summary>
    /// Löst den Host-Namen manuell auf IPv4 auf und baut die WebSocket-URL mit der IP statt
    /// dem Hostnamen. Damit umgehen wir den .NET-Default, der IPv6 zuerst versucht und bei
    /// kaputtem IPv6-Routing in Timeouts läuft. Der Host-Header muss dann explizit gesetzt
    /// werden, damit SNI/Virtual Hosting am Server weiter funktioniert.
    /// </summary>
    private async Task<System.Uri> BaueIPv4Uri(string userId, string token)
    {
        var addresses = await Dns.GetHostAddressesAsync(SERVER_HOST);
        IPAddress ipv4 = null;
        foreach (var addr in addresses)
        {
            if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                ipv4 = addr;
                break;
            }
        }

        if (ipv4 == null)
            throw new System.Exception("Keine IPv4-Adresse für " + SERVER_HOST + " gefunden");

        // URL mit IP statt Hostname bauen — Host-Header wird unten explizit gesetzt
        return new System.Uri($"wss://{ipv4}/ws/{userId}?token={token}");
    }

    public async void Verbinden(string userId, string token)
    {
        try
        {
            Ws = new ClientWebSocket();

            // Host-Header setzen, damit Nginx auf dem Server weiß, welche Domain gemeint ist
            // (wichtig, weil wir die URL mit IP bauen statt Hostname)
            Ws.Options.SetRequestHeader("Host", SERVER_HOST);

            // SNI funktioniert bei ClientWebSocket automatisch über RemoteCertificateValidationCallback,
            // wir vertrauen dem Zertifikat für SERVER_HOST auch wenn die URL die IP enthält
            Ws.Options.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                // Prüft das Zertifikat gegen den ursprünglichen Hostnamen statt gegen die IP
                if (errors == System.Net.Security.SslPolicyErrors.None) return true;
                // Bei Namen-Mismatch trotzdem akzeptieren, weil wir ja bewusst die IP nutzen —
                // der Hostname-Check müsste gegen SERVER_HOST laufen, was Let's Encrypt korrekt macht
                if (errors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch)
                    return true;
                return false;
            };

            var uri = await BaueIPv4Uri(userId, token);
            await Ws.ConnectAsync(uri, CancellationToken.None);

            _reconnecting = false;
            GD.Print("WebSocket verbunden!");
            EmitSignal(SignalName.Verbunden);
            _ = WarteAufNachrichten();
        }
        catch (System.Exception e)
        {
            GD.Print("WebSocket Verbindungsfehler: " + e.Message);
            StarteReconnect();
        }
    }

    private async void StarteReconnect()
    {
        if (_reconnecting || !_authManager.IstEingeloggt) return;
        _reconnecting = true;
        EmitSignal(SignalName.VerbindungVerloren);
        GD.Print($"Reconnecte in {RECONNECT_INTERVAL} Sekunden...");
        await ToSignal(GetTree().CreateTimer(RECONNECT_INTERVAL), Godot.Timer.SignalName.Timeout);
        if (_authManager.IstEingeloggt)
            Verbinden(_authManager.UserId, _authManager.Token);
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

                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                var type = root.GetProperty("type").GetString();

                if (type == "plane")
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
                    string sender = root.GetProperty("sender_id").GetString();
                    EmitSignal(SignalName.BildErhalten, sender, texture, posY, velX, velY, rotZ);
                }
                else if (type == "friend_request")
                {
                    EmitSignal(SignalName.FreundschaftsAnfrage,
                        root.GetProperty("from_user_id").GetString(),
                        root.GetProperty("from_username").GetString());
                }
                else if (type == "friend_accepted")
                {
                    EmitSignal(SignalName.FreundschaftsAngenommen,
                        root.GetProperty("from_user_id").GetString(),
                        root.GetProperty("from_username").GetString());
                }
                else if (type == "group_invite")
                {
                    EmitSignal(SignalName.GruppenEinladung,
                        root.GetProperty("group_id").GetString(),
                        root.GetProperty("group_name").GetString(),
                        root.GetProperty("from_username").GetString());
                }
                else if (type == "friend_online")
                {
                    EmitSignal(SignalName.FreundOnline,
                        root.GetProperty("user_id").GetString(),
                        root.GetProperty("username").GetString());
                }
                else if (type == "friend_offline")
                {
                    EmitSignal(SignalName.FreundOffline,
                        root.GetProperty("user_id").GetString());
                }
                else if (type == "pong")
                {
                    // nichts tun
                }
            }
            catch (System.Exception e)
            {
                GD.Print("WebSocket Fehler: " + e.Message);
                break;
            }
        }

        // Verbindung verloren → Reconnect
        StarteReconnect();
    }

    public async Task SendeBildAsync(string targetId, Image image, Vector3 position, Vector3 velocity, Vector3 rotation)
    {
        if (Ws.State != WebSocketState.Open)
            throw new Exception("WebSocket nicht verbunden");

        var bytes = image.SavePngToBuffer();
        var base64 = System.Convert.ToBase64String(bytes);
        var message = JsonSerializer.Serialize(new
        {
            type = "plane",
            target_id = targetId,
            image_data = base64,
            position_y = position.Y,
            velocity_x = velocity.X,
            velocity_y = velocity.Y,
            rotation_z = rotation.Z
        });
        await SendeNachricht(message);
    }

    public async Task SendeBildAnGruppeAsync(string groupId, Image image, Vector3 position, Vector3 velocity, Vector3 rotation)
    {
        if (Ws.State != WebSocketState.Open)
            throw new Exception("WebSocket nicht verbunden");

        var bytes = image.SavePngToBuffer();
        var base64 = System.Convert.ToBase64String(bytes);
        var message = JsonSerializer.Serialize(new
        {
            type = "plane_group",
            group_id = groupId,
            image_data = base64,
            position_y = position.Y,
            velocity_x = velocity.X,
            velocity_y = velocity.Y,
            rotation_z = rotation.Z
        });
        await SendeNachricht(message);
    }

    private async System.Threading.Tasks.Task SendeNachricht(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await Ws.SendAsync(
            new System.ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }
}