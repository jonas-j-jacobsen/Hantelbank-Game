using Godot;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.Http;


public partial class NetworkManager : Node
{
    private const string SERVER_URL = "http://127.0.0.1:8000";
    private const float POLL_INTERVAL = 2f;
    private const float HEARTBEAT_INTERVAL = 10f;

    public string UserId { get; private set; }
    public string Username { get; private set; }
    public string Token { get; private set; }

    private float _pollTimer = 0f;
    private float _heartbeatTimer = 0f;
    private ClickableObject _hantel;

    private bool _isOnline = true ;

    private static readonly System.Net.Http.HttpClient _client = new System.Net.Http.HttpClient();

    public List<OnlinePlayer> OnlineSpieler { get; private set; } = new();
    public List<ClickInfo> LetzteClicks { get; private set; } = new();

    [Signal] public delegate void ClicksErhaltenEventHandler(int anzahl);
    [Signal] public delegate void ClickInfoErhaltenEventHandler();
    [Signal] public delegate void OnlineListeAktualisiertEventHandler();
    [Signal] public delegate void FehlerEventHandler(string nachricht);

    public class OnlinePlayer
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsSupporting { get; set; }
    }

    public class ClickInfo
    {
        public string Username { get; set; }
        public int Clicks { get; set; }
    }

    public class LeaderboardEntry
    {
        public string Username { get; set; }
        public string UserId { get; set; }
        public int BigLoop { get; set; }
        public int MediumLoop { get; set; }
        public int SmallLoop { get; set; }
        public bool IsOnline { get; set; }
    }

    public override void _Ready()
    {
        if (FileAccess.FileExists("user://login.json"))
        {
            using var file = FileAccess.Open("user://login.json", FileAccess.ModeFlags.Read);
            var data = Json.ParseString(file.GetAsText()).AsGodotDictionary();
            UserId = data["user_id"].AsString();
            Username = data["username"].AsString();
            Token = data["token"].AsString();
            GD.Print("Eingeloggt als: " + Username);
        }
    }

    public override void _Process(double delta)
    {
        if (UserId == null || !_isOnline) return;

        _heartbeatTimer += (float)delta;
        if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
        {
            _heartbeatTimer = 0f;
            SendeHeartbeat(
                _hantel?.bigLoop ?? 0,
                _hantel?.mediumLoop ?? 0,
                _hantel?.smallLoop ?? 0
            );
        }

        _pollTimer += (float)delta;
        if (_pollTimer >= POLL_INTERVAL)
        {
            _pollTimer = 0f;
            HoleClicks();
            HoleOnlineSpieler();
        }
    }

    public void SetHantel(ClickableObject hantel) => _hantel = hantel;

    public async void Registrieren(string username)
    {
        GD.Print("1 - Vor Request");
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { username }),
                Encoding.UTF8, "application/json");

            GD.Print("2 - Sende Request");
            var response = await _client.PostAsync(SERVER_URL + "/register", body);
            GD.Print("3 - Antwort erhalten");
            var json = await response.Content.ReadAsStringAsync();
            GD.Print("4 - JSON: " + json);

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (response.IsSuccessStatusCode)
            {
                var data = Json.ParseString(json).AsGodotDictionary();
                UserId = data["user_id"].AsString();
                Token = data["token"].AsString();
                Username = username;
                SpeichereLogin();
                SendeHeartbeat(0, 0, 0);
                GD.Print("Registriert! ID: " + UserId);

                SupportHinzufügen(UserId);
            }
            else
            {
                EmitSignal(SignalName.Fehler, "Username bereits vergeben");
            }
        }
        catch (System.Exception e)
        {
            GD.Print("Registrieren Fehler: " + e.Message);
        }
    }

    public async void SendeHeartbeat(int bigLoop, int mediumLoop, int smallLoop)
    {
        if (UserId == null) return;
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    token = Token,
                    big_loop = bigLoop,
                    medium_loop = mediumLoop,
                    small_loop = smallLoop
                }),
                Encoding.UTF8, "application/json");

            await _client.PostAsync(SERVER_URL + "/heartbeat/" + UserId, body);
        }
        catch (System.Exception e) { GD.Print("Heartbeat Fehler: " + e.Message); }
    }

    public async void SendeClick()
    {
        if (UserId == null) return;
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = Token }),
                Encoding.UTF8, "application/json");

            await _client.PostAsync(SERVER_URL + "/click/" + UserId, body);
        }
        catch (System.Exception e) { GD.Print("Click Fehler: " + e.Message); }
    }

    private async void HoleClicks()
    {
        if (UserId == null) return;

        try
        {
            var response = await _client.GetAsync(SERVER_URL + "/clicks/" + UserId);
            var json = await response.Content.ReadAsStringAsync();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (response.IsSuccessStatusCode)
            {
                var data = Json.ParseString(json).AsGodotDictionary();
                int total = data["total"].AsInt32();
                if (total > 0)
                {
                    LetzteClicks.Clear();
                    foreach (var s in data["senders"].AsGodotArray())
                    {
                        var sd = s.AsGodotDictionary();
                        LetzteClicks.Add(new ClickInfo
                        {
                            Username = sd["username"].AsString(),
                            Clicks = sd["clicks"].AsInt32()
                        });
                    }
                    GD.Print(total);
                    EmitSignal(SignalName.ClicksErhalten, total);
                    EmitSignal(SignalName.ClickInfoErhalten);
                }
            }
        }
        catch (System.Exception e) { GD.Print("Clicks Fehler: " + e.Message); }
    }

    private async void HoleOnlineSpieler()
    {
        if (UserId == null) return;
        try
        {
            var response = await _client.GetAsync(SERVER_URL + "/online/" + UserId);
            var json = await response.Content.ReadAsStringAsync();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (response.IsSuccessStatusCode)
            {
                OnlineSpieler.Clear();
                var data = Json.ParseString(json).AsGodotDictionary();
                foreach (var p in data["players"].AsGodotArray())
                {
                    var pd = p.AsGodotDictionary();
                    OnlineSpieler.Add(new OnlinePlayer
                    {
                        UserId = pd["user_id"].AsString(),
                        Username = pd["username"].AsString(),
                        IsFavorite = pd["is_favorite"].AsBool(),
                        IsSupporting = pd["is_supporting"].AsBool()
                    });
                }
                EmitSignal(SignalName.OnlineListeAktualisiert);
            }
        }
        catch (System.Exception e) { GD.Print("Online Fehler: " + e.Message); }
    }

    public async void SupportHinzufügen(string targetId)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    token = Token,
                    user_id = UserId,
                    target_id = targetId
                }),
                Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(SERVER_URL + "/support/add", body);

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (!response.IsSuccessStatusCode)
                EmitSignal(SignalName.Fehler, "Maximal 5 Spieler");
        }
        catch (System.Exception e) { GD.Print("Support Fehler: " + e.Message); }
    }

    public async void SupportEntfernen(string targetId)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    token = Token,
                    user_id = UserId,
                    target_id = targetId
                }),
                Encoding.UTF8, "application/json");
            await _client.PostAsync(SERVER_URL + "/support/remove", body);
        }
        catch (System.Exception e) { GD.Print("Support Fehler: " + e.Message); }
    }

    public async void FavoritHinzufügen(string targetId)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    token = Token,
                    user_id = UserId,
                    target_id = targetId
                }),
                Encoding.UTF8, "application/json");
            await _client.PostAsync(SERVER_URL + "/favorites/add", body);
        }
        catch (System.Exception e) { GD.Print("Favorit Fehler: " + e.Message); }
    }

    public async void HoleLeaderboard(System.Action<List<LeaderboardEntry>> callback)
    {
        try
        {
            var response = await _client.GetAsync(SERVER_URL + "/leaderboard");
            var json = await response.Content.ReadAsStringAsync();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (response.IsSuccessStatusCode)
            {
                var entries = new List<LeaderboardEntry>();
                var data = Json.ParseString(json).AsGodotDictionary();
                foreach (var e in data["leaderboard"].AsGodotArray())
                {
                    var ed = e.AsGodotDictionary();
                    entries.Add(new LeaderboardEntry
                    {
                        Username = ed["username"].AsString(),
                        UserId = ed["user_id"].AsString(),
                        BigLoop = ed["big_loop"].AsInt32(),
                        MediumLoop = ed["medium_loop"].AsInt32(),
                        SmallLoop = ed["small_loop"].AsInt32(),
                        IsOnline = ed["is_online"].AsBool()
                    });
                }
                callback(entries);
            }
        }
        catch (System.Exception e) { GD.Print("Leaderboard Fehler: " + e.Message); }
    }

    private void SpeichereLogin()
    {
        var data = new Godot.Collections.Dictionary
        {
            { "user_id", UserId },
            { "username", Username },
            { "token", Token }
        };
        using var file = FileAccess.Open("user://login.json", FileAccess.ModeFlags.Write);
        file.StoreString(Json.Stringify(data));
    }
}