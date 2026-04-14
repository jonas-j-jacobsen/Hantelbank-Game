using Godot;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class FriendManager : Node
{
    private const string SERVER_URL = "https://paper-plane-messenger-server.onrender.com";
    private static readonly System.Net.Http.HttpClient _client = new System.Net.Http.HttpClient();
    private AuthManager _authManager;

    public class Friend
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public bool Online { get; set; }
    }

    public class PendingRequest
    {
        public string UserId { get; set; }
        public string Username { get; set; }
    }

    public class Group
    {
        public string GroupId { get; set; }
        public string Name { get; set; }
        public bool InviteOnly { get; set; }
        public bool Aktiv { get; set; } = true; // NEU
    }

    [Signal] public delegate void AktualisiertEventHandler();
    [Signal] public delegate void FehlerEventHandler(string nachricht);
    [Signal] public delegate void ErfolgEventHandler(string nachricht);

    public List<Friend> Freunde { get; private set; } = new();
    public List<PendingRequest> EingehendeAnfragen { get; private set; } = new();
    public List<PendingRequest> AusgehendeAnfragen { get; private set; } = new();
    public List<PendingRequest> IgnorierteAnfragen { get; private set; } = new();
    public List<Group> Gruppen { get; private set; } = new();

    public override void _Ready()
    {
        _authManager = GetNode<AuthManager>("/root/AuthManager");
        _authManager.Eingeloggt += () => LadeAlles();

        var wsManager = GetNode<WebSocketManager>("/root/WebSocketManager");
        wsManager.FreundOnline += (userId, username) =>
        {
            var freund = Freunde.Find(f => f.UserId == userId);
            if (freund != null) { freund.Online = true; EmitSignal(SignalName.Aktualisiert); }
        };
        wsManager.FreundOffline += (userId) =>
        {
            var freund = Freunde.Find(f => f.UserId == userId);
            if (freund != null) { freund.Online = false; EmitSignal(SignalName.Aktualisiert); }
        };
        wsManager.FreundschaftsAnfrage += (userId, username) =>
        {
            EingehendeAnfragen.Add(new PendingRequest { UserId = userId, Username = username });
            EmitSignal(SignalName.Aktualisiert);
        };
        wsManager.FreundschaftsAngenommen += (userId, username) =>
        {
            AusgehendeAnfragen.RemoveAll(a => a.UserId == userId);
            Freunde.Add(new Friend { UserId = userId, Username = username, Online = true });
            EmitSignal(SignalName.Aktualisiert);
        };
    }

    public async void LadeAlles()
    {
        await LadeFreunde();
        await LadeEingehendeAnfragen();
        await LadeGruppen();
        EmitSignal(SignalName.Aktualisiert);
    }

    private async Task LadeFreunde()
    {
        try
        {
            var response = await _client.GetAsync($"{SERVER_URL}/friends?token={_authManager.Token}");
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            Freunde.Clear();
            foreach (var item in doc.RootElement.EnumerateArray())
                Freunde.Add(new Friend
                {
                    UserId = item.GetProperty("user_id").GetString(),
                    Username = item.GetProperty("username").GetString(),
                    Online = item.GetProperty("online").GetBoolean()
                });
        }
        catch { }
    }

    public async Task<List<Friend>> LadeOnlineFreunde()
    {
        try
        {
            var response = await _client.GetAsync($"{SERVER_URL}/friends/online?token={_authManager.Token}");
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var online = new List<Friend>();
            foreach (var item in doc.RootElement.EnumerateArray())
                online.Add(new Friend
                {
                    UserId = item.GetProperty("user_id").GetString(),
                    Username = item.GetProperty("username").GetString(),
                    Online = true
                });     
            return online;
        }
        catch { return new List<Friend>(); }
    }

    private async Task LadeEingehendeAnfragen()
    {
        try
        {
            var response = await _client.GetAsync($"{SERVER_URL}/friends/pending?token={_authManager.Token}");
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            EingehendeAnfragen.Clear();
            foreach (var item in doc.RootElement.EnumerateArray())
                EingehendeAnfragen.Add(new PendingRequest
                {
                    UserId = item.GetProperty("user_id").GetString(),
                    Username = item.GetProperty("username").GetString()
                });
        }
        catch { }
    }

    private async Task LadeGruppen()
    {
        try
        {
            var response = await _client.GetAsync($"{SERVER_URL}/groups?token={_authManager.Token}");
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            Gruppen.Clear();
            foreach (var item in doc.RootElement.EnumerateArray())
                Gruppen.Add(new Group
                {
                    GroupId = item.GetProperty("group_id").GetString(),
                    Name = item.GetProperty("name").GetString(),
                    InviteOnly = item.GetProperty("invite_only").GetBoolean()
                });
        }
        catch { }
    }

    public async void SucheUndSendeAnfrage(string username)
    {
        try
        {
            // Suchen
            var response = await _client.GetAsync(
                $"{SERVER_URL}/users/search?q={username}&token={_authManager.Token}");
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var results = doc.RootElement;

            if (results.GetArrayLength() == 0)
            {
                EmitSignal(SignalName.Fehler, "Benutzer nicht gefunden");
                return;
            }

            // Ersten Treffer nehmen
            var targetId = results[0].GetProperty("user_id").GetString();
            var targetName = results[0].GetProperty("username").GetString();

            // Anfrage senden
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = _authManager.Token, target_user_id = targetId }),
                Encoding.UTF8, "application/json");
            var sendResponse = await _client.PostAsync($"{SERVER_URL}/friends/request", body);

            if (sendResponse.IsSuccessStatusCode)
            {
                AusgehendeAnfragen.Add(new PendingRequest { UserId = targetId, Username = targetName });
                EmitSignal(SignalName.Erfolg, $"Anfrage an {targetName} gesendet!");
                EmitSignal(SignalName.Aktualisiert);
            }
            else
            {
                var errDoc = JsonDocument.Parse(await sendResponse.Content.ReadAsStringAsync());
                EmitSignal(SignalName.Fehler, errDoc.RootElement.GetProperty("detail").GetString());
            }
        }
        catch { EmitSignal(SignalName.Fehler, "Verbindungsfehler"); }
    }

    public async void AnfrageAnnehmen(string userId)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = _authManager.Token, target_user_id = userId }),
                Encoding.UTF8, "application/json");
            await _client.PostAsync($"{SERVER_URL}/friends/accept", body);
            var anfrage = EingehendeAnfragen.Find(a => a.UserId == userId);
            if (anfrage != null)
            {
                EingehendeAnfragen.Remove(anfrage);
                Freunde.Add(new Friend { UserId = userId, Username = anfrage.Username, Online = false });
            }
            EmitSignal(SignalName.Aktualisiert);
        }
        catch { }
    }

    public async void AnfrageAblehnen(string userId)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = _authManager.Token, target_user_id = userId }),
                Encoding.UTF8, "application/json");
            await _client.PostAsync($"{SERVER_URL}/friends/decline", body);
            var anfrage = EingehendeAnfragen.Find(a => a.UserId == userId);
            if (anfrage != null)
            {
                EingehendeAnfragen.Remove(anfrage);
                IgnorierteAnfragen.Add(anfrage);
            }
            EmitSignal(SignalName.Aktualisiert);
        }
        catch { }
    }

    public async void AnfrageEntIgnorieren(string userId)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = _authManager.Token, target_user_id = userId }),
                Encoding.UTF8, "application/json");
            await _client.PostAsync($"{SERVER_URL}/friends/request", body);
            var anfrage = IgnorierteAnfragen.Find(a => a.UserId == userId);
            if (anfrage != null)
            {
                IgnorierteAnfragen.Remove(anfrage);
                AusgehendeAnfragen.Add(anfrage);
            }
            EmitSignal(SignalName.Aktualisiert);
        }
        catch { }
    }

    public async void AnfrageAbbrechen(string userId)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = _authManager.Token, target_user_id = userId }),
                Encoding.UTF8, "application/json");
            await _client.PostAsync($"{SERVER_URL}/friends/decline", body);
            AusgehendeAnfragen.RemoveAll(a => a.UserId == userId);
            EmitSignal(SignalName.Aktualisiert);
        }
        catch { }
    }

    public async void GruppeErstellen(string name, bool inviteOnly)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = _authManager.Token, name, invite_only = inviteOnly }),
                Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{SERVER_URL}/groups/create", body);
            if (response.IsSuccessStatusCode)
            {
                await LadeGruppen();
                EmitSignal(SignalName.Erfolg, $"Gruppe '{name}' erstellt!");
                EmitSignal(SignalName.Aktualisiert);
            }
        }
        catch { }
    }

    public async void GruppeBetreten(string groupId)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = _authManager.Token, group_id = groupId }),
                Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{SERVER_URL}/groups/join", body);
            if (response.IsSuccessStatusCode)
            {
                await LadeGruppen();
                EmitSignal(SignalName.Aktualisiert);
            }
            else
            {
                var errDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                EmitSignal(SignalName.Fehler, errDoc.RootElement.GetProperty("detail").GetString());
            }
        }
        catch { }
    }

    public async void GruppeVerlassen(string groupId)
    {
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { token = _authManager.Token, group_id = groupId }),
                Encoding.UTF8, "application/json");
            await _client.PostAsync($"{SERVER_URL}/groups/leave", body);
            Gruppen.RemoveAll(g => g.GroupId == groupId);
            EmitSignal(SignalName.Aktualisiert);
        }
        catch { }
    }

    public async Task<List<Group>> SucheGruppen(string name)
    {
        try
        {
            var response = await _client.GetAsync(
                $"{SERVER_URL}/groups/search?q={name}&token={_authManager.Token}");
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var results = new List<Group>();
            foreach (var item in doc.RootElement.EnumerateArray())
                results.Add(new Group
                {
                    GroupId = item.GetProperty("group_id").GetString(),
                    Name = item.GetProperty("name").GetString()
                });
            return results;
        }
        catch { return new List<Group>(); }
    }

    public void GruppeToggleAktiv(string groupId)
    {
        var gruppe = Gruppen.Find(g => g.GroupId == groupId);
        if (gruppe != null)
        {
            gruppe.Aktiv = !gruppe.Aktiv;
            EmitSignal(SignalName.Aktualisiert);
        }
    }
}