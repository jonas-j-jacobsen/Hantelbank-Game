using Godot;
using System;
using System.Text;

public partial class NetworkManager : Node
{
    private const int PORT = 7777;
    private const int MAX_PLAYERS = 10;

    public bool IstVerbunden => Multiplayer.MultiplayerPeer != null &&
                                 Multiplayer.MultiplayerPeer.GetConnectionStatus() ==
                                 MultiplayerPeer.ConnectionStatus.Connected;

    public string Host()
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(PORT, MAX_PLAYERS);
        Multiplayer.MultiplayerPeer = peer;

        // Lokale IP als Code kodieren
        string ip = IP.GetLocalAddresses()[0];
        string code = IpZuCode(ip);
        GD.Print("Server gestartet! Code: " + code);
        return code;
    }

    public void Join(string code)
    {
        string ip = CodeZuIp(code);
        GD.Print("Verbinde mit: " + ip);
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(ip, PORT);
        Multiplayer.MultiplayerPeer = peer;
    }

    // IP zu kurzem Code z.B. "192.168.1.5" → "C0A80105"
    private string IpZuCode(string ip)
    {
        var parts = ip.Split('.');
        var sb = new StringBuilder();
        foreach (var part in parts)
            sb.Append(int.Parse(part).ToString("X2"));
        return sb.ToString();
    }

    private string CodeZuIp(string code)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 4; i++)
        {
            if (i > 0) sb.Append('.');
            sb.Append(Convert.ToInt32(code.Substring(i * 2, 2), 16));
        }
        return sb.ToString();
    }
}