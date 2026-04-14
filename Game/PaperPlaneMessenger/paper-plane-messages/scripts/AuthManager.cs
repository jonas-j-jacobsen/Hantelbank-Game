using Godot;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public partial class AuthManager : Node
{
    private const string SERVER_URL = "https://api.studio-maus.de";
    private const float POLL_INTERVAL = 2f;

    public string Token { get; private set; }
    public string UserId { get; private set; }
    public string Username { get; private set; }
    public bool IstEingeloggt => Token != null;

    private string _loginId;
    private float _pollTimer = 0f;
    private bool _pollt = false;
    private static readonly System.Net.Http.HttpClient _client = new System.Net.Http.HttpClient();

    [Signal] public delegate void EingeloggtEventHandler();
    [Signal] public delegate void UsernameWählenEventHandler();
    [Signal] public delegate void AusgeloggtEventHandler();
    [Signal] public delegate void FehlerEventHandler(string nachricht);
    [Signal] public delegate void UsernameGeändertEventHandler(string neuerUsername);
    [Signal] public delegate void ServerWirdGestartetEventHandler();
    [Signal] public delegate void ServerBereitEventHandler();

    public override void _Ready()
    {
        if (FileAccess.FileExists("user://login.json"))
        {
            using var file = FileAccess.Open("user://login.json", FileAccess.ModeFlags.Read);
            var data = Json.ParseString(file.GetAsText()).AsGodotDictionary();
            Token = data["token"].AsString();
            UserId = data["user_id"].AsString();
            Username = data["username"].AsString();
            GD.Print("Gespeicherter Login geladen: " + Username);
            ValidiereToken();
        }
    }

    public override void _Process(double delta)
    {
        if (!_pollt) return;
        _pollTimer += (float)delta;
        if (_pollTimer >= POLL_INTERVAL)
        {
            _pollTimer = 0f;
            Poll();
        }
    }

    private async void ValidiereToken()
    {
        try
        {
            var response = await _client.GetAsync($"{SERVER_URL}/auth/me?token={Token}");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                Username = doc.RootElement.GetProperty("username").GetString();
                SpeichereLogin();
                EmitSignal(SignalName.Eingeloggt);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                GD.Print("Token abgelaufen, neu einloggen");
                LöscheLogin();
                EmitSignal(SignalName.Ausgeloggt);
            }
        }
        catch (Exception e)
        {
            GD.Print("Token Validierung Fehler: " + e.Message);
            // Bei Verbindungsfehler trotzdem einloggen
            EmitSignal(SignalName.Eingeloggt);
        }
    }

    public async void StarteLogin()
    {
        _loginId = Guid.NewGuid().ToString();

        // Server aufwecken bevor Browser öffnet
        EmitSignal(SignalName.ServerWirdGestartet);
        await WeckeServer();
        EmitSignal(SignalName.ServerBereit);

        var url = $"{SERVER_URL}/auth/login?login_id={_loginId}";
        OS.ShellOpen(url);
        _pollt = true;
        _pollTimer = 0f;
        GD.Print("Browser geöffnet, warte auf Login...");
    }

    private async Task WeckeServer()
    {
        try
        {
            for (int i = 0; i < 10; i++)
            {
                var response = await _client.GetAsync($"{SERVER_URL}/ping");
                if (response.IsSuccessStatusCode) return;
                await Task.Delay(3000);
            }
        }
        catch { }
    }

    private async void Poll()
    {
        try
        {
            var response = await _client.GetAsync($"{SERVER_URL}/auth/poll/{_loginId}");
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var status = root.GetProperty("status").GetString();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (status == "ok")
            {
                _pollt = false;
                Token = root.GetProperty("token").GetString();
                await LadeUserInfo();
                SpeichereLogin();
                EmitSignal(SignalName.Eingeloggt);
            }
            else if (status == "choose_username")
            {
                _pollt = false;
                EmitSignal(SignalName.UsernameWählen);
            }
        }
        catch (Exception e)
        {
            GD.Print("Poll Fehler: " + e.Message);
        }
    }

    public async void Registrieren(string username)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = _loginId, username }),
                Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{SERVER_URL}/auth/register", body);
            var json = await response.Content.ReadAsStringAsync();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (response.IsSuccessStatusCode)
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                Token = root.GetProperty("token").GetString();
                UserId = root.GetProperty("user_id").GetString();
                Username = root.GetProperty("username").GetString();
                SpeichereLogin();
                EmitSignal(SignalName.Eingeloggt);
            }
            else
            {
                var doc = JsonDocument.Parse(json);
                var fehler = doc.RootElement.GetProperty("detail").GetString();
                EmitSignal(SignalName.Fehler, fehler);
            }
        }
        catch (Exception e)
        {
            GD.Print("Registrierung Fehler: " + e.Message);
            EmitSignal(SignalName.Fehler, "Verbindungsfehler");
        }
    }

    public async void ÄndereUsername(string neuerUsername)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = Token, username = neuerUsername }),
                Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{SERVER_URL}/auth/username", body);
            var json = await response.Content.ReadAsStringAsync();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (response.IsSuccessStatusCode)
            {
                Username = neuerUsername;
                SpeichereLogin();
                EmitSignal(SignalName.UsernameGeändert, Username);
            }
            else
            {
                var doc = JsonDocument.Parse(json);
                var fehler = doc.RootElement.GetProperty("detail").GetString();
                EmitSignal(SignalName.Fehler, fehler);
            }
        }
        catch (Exception e)
        {
            GD.Print("Username Änderung Fehler: " + e.Message);
            EmitSignal(SignalName.Fehler, "Verbindungsfehler");
        }
    }

    public void Ausloggen()
    {
        LöscheLogin();
        EmitSignal(SignalName.Ausgeloggt);
    }

    private async Task LadeUserInfo()
    {
        try
        {
            var response = await _client.GetAsync($"{SERVER_URL}/auth/me?token={Token}");
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            UserId = root.GetProperty("user_id").GetString();
            Username = root.GetProperty("username").GetString();
        }
        catch (Exception e)
        {
            GD.Print("UserInfo Fehler: " + e.Message);
        }
    }

    private void LöscheLogin()
    {
        Token = null;
        UserId = null;
        Username = null;
        if (FileAccess.FileExists("user://login.json"))
            DirAccess.RemoveAbsolute("user://login.json");
    }

    private void SpeichereLogin()
    {
        using var file = FileAccess.Open("user://login.json", FileAccess.ModeFlags.Write);
        file.StoreString(Json.Stringify(new Godot.Collections.Dictionary
        {
            ["token"] = Token,
            ["user_id"] = UserId,
            ["username"] = Username
        }));
    }
}