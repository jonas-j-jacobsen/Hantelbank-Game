using Godot;
using System.Collections.Generic;

public partial class EmpfängerListe : VBoxContainer
{
    private WebSocketManager _networkManager;
    private ItemList _liste;
    private LineEdit _sucheInput;

    public override void _Ready()
    {
        _networkManager = GetNode<WebSocketManager>("/root/WebSocketManager");
        _liste = GetNode<ItemList>("Liste");
        _sucheInput = GetNode<LineEdit>("SucheInput");

        GetNode<Button>("HinzufügenButton").Pressed += OnHinzufügenPressed;

        // Online Spieler laden
        _networkManager.OnlineListeAktualisiert += AktualisiereOnlineListe;
        AktualisiereOnlineListe();
    }

    private void AktualisiereOnlineListe()
    {
        _liste.Clear();
        foreach (var spieler in _networkManager.OnlineSpieler)
        {
            int idx = _liste.AddItem(spieler.Username);
            _liste.SetItemMetadata(idx, spieler.UserId);
        }
    }

    private void OnHinzufügenPressed()
    {
        var username = _sucheInput.Text.Trim();
        if (username == "") return;
    //    _networkManager.FavoritHinzufügen(username);
        _sucheInput.Text = "";
    }

    public List<string> GetAusgewählteIds()
    {
        var ids = new List<string>();
        foreach (int idx in _liste.GetSelectedItems())
            ids.Add(_liste.GetItemMetadata(idx).AsString());
        return ids;
    }
}