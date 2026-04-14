using Godot;

public partial class FreundeUI : DraggablePanel
{
    private FriendManager _friendManager;
    private WebSocketManager _wsManager;

    // Suche
    private LineEdit _sucheInput;
    private Button _sucheButton;
    private Label _sucheStatus;

    // Listen
    private VBoxContainer _freundeListe;
    private VBoxContainer _eingehendeListe;
    private VBoxContainer _ausgehendeListe;

    private Button _closeButton;

    // Gruppen Tab (noch nicht fertig)
    // ...

    public override void _Ready()
    {
        _friendManager = GetNode<FriendManager>("/root/FriendManager");
        _wsManager = GetNode<WebSocketManager>("/root/WebSocketManager");

        _sucheInput = GetNode<LineEdit>("TabContainer/Freunde/VBoxContainer/HBoxContainer/LineEdit");
        _sucheButton = GetNode<Button>("TabContainer/Freunde/VBoxContainer/HBoxContainer/Button");
        _sucheStatus = GetNode<Label>("TabContainer/Freunde/VBoxContainer/SucheStatus");

        _freundeListe = GetNode<VBoxContainer>("TabContainer/Freunde/VBoxContainer/ScrollContainer/VBoxContainer/FreundeListe/ScrollContainer/VBoxContainer");
        _eingehendeListe = GetNode<VBoxContainer>("TabContainer/Freunde/VBoxContainer/ScrollContainer/VBoxContainer/AnfragenListe/ScrollContainer/VBoxContainer");
        _ausgehendeListe = GetNode<VBoxContainer>("TabContainer/Freunde/VBoxContainer/ScrollContainer/VBoxContainer/AusstehendListe/ScrollContainer/VBoxContainer");

        _closeButton = GetNode<Button>("CloseButton");

        _closeButton.Pressed += ToggleSichtbarkeit;

        _sucheButton.Pressed += OnSuchePressed;
        _sucheInput.TextSubmitted += _ => OnSuchePressed();

        _friendManager.Aktualisiert += AktualisiereUI;
        _friendManager.Fehler += (msg) => _sucheStatus.Text = "❌ " + msg;
        _friendManager.Erfolg += (msg) => _sucheStatus.Text = "✓ " + msg;

        // Eingehende Anfrage via WebSocket → Popup
        _wsManager.FreundschaftsAnfrage += OnFreundschaftsAnfrageErhalten;

        Visible = false;
    }

    private void OnFreundschaftsAnfrageErhalten(string userId, string username)
    {
        // Popup im FreundOnlinePopup VBox anzeigen
        var popupContainer = GetNodeOrNull<VBoxContainer>(
            "/root/Main/FriendOnlineUI/Control/VBoxContainer");
        if (popupContainer == null) return;

        var box = new HBoxContainer();
        var label = new Label();
        label.Text = $"📩 {username} möchte dein Freund sein!";
        label.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;

        var annehmen = new Button(); annehmen.Text = "✓";
        var ablehnen = new Button(); ablehnen.Text = "✗";

        var id = userId;
        annehmen.Pressed += () =>
        {
            _friendManager.AnfrageAnnehmen(id);
            box.QueueFree();
        };
        ablehnen.Pressed += () =>
        {
            _friendManager.AnfrageAblehnen(id);
            box.QueueFree();
        };

        box.AddChild(label);
        box.AddChild(annehmen);
        box.AddChild(ablehnen);
        box.Modulate = new Color(1, 1, 1, 0);
        popupContainer.AddChild(box);

        // Einblenden → 15 Sekunden → Ausblenden
        var tween = box.CreateTween();
        tween.TweenProperty(box, "modulate:a", 1f, 0.3f);
        tween.TweenInterval(15f);
        tween.TweenProperty(box, "modulate:a", 0f, 0.5f);
        tween.TweenCallback(Callable.From(() => box.QueueFree()));
    }

    private void OnSuchePressed()
    {
        var username = _sucheInput.Text.Trim();
        if (username == "") return;
        _sucheStatus.Text = "Suche...";
        _friendManager.SucheUndSendeAnfrage(username);
        _sucheInput.Text = "";
    }

    private void AktualisiereUI()
    {
        AktualisiereFreunde();
        AktualisiereEingehend();
        AktualisiereAusgehend();
    }

    private void AktualisiereFreunde()
    {
        LeereListe(_freundeListe);
        var sortiert = new System.Collections.Generic.List<FriendManager.Friend>(_friendManager.Freunde);
        sortiert.Sort((a, b) => b.Online.CompareTo(a.Online));
        foreach (var freund in sortiert)
        {
            var zeile = ErstelleZeile((freund.Online ? "🟢 " : "⚫ ") + freund.Username);
            _freundeListe.AddChild(zeile);
        }
    }

    private void AktualisiereEingehend()
    {
        LeereListe(_eingehendeListe);
        foreach (var anfrage in _friendManager.EingehendeAnfragen)
        {
            var zeile = ErstelleZeile(anfrage.Username);
            var annehmen = new Button(); annehmen.Text = "✓";
            var ablehnen = new Button(); ablehnen.Text = "✗";
            var id = anfrage.UserId;
            annehmen.Pressed += () => _friendManager.AnfrageAnnehmen(id);
            ablehnen.Pressed += () => _friendManager.AnfrageAblehnen(id);
            zeile.AddChild(annehmen);
            zeile.AddChild(ablehnen);
            _eingehendeListe.AddChild(zeile);
        }
    }

    private void AktualisiereAusgehend()
    {
        LeereListe(_ausgehendeListe);
        foreach (var anfrage in _friendManager.AusgehendeAnfragen)
        {
            var zeile = ErstelleZeile(anfrage.Username);
            var abbrechen = new Button(); abbrechen.Text = "✗";
            var id = anfrage.UserId;
            abbrechen.Pressed += () => _friendManager.AnfrageAbbrechen(id);
            zeile.AddChild(abbrechen);
            _ausgehendeListe.AddChild(zeile);
        }
    }

    private HBoxContainer ErstelleZeile(string text)
    {
        var zeile = new HBoxContainer();
        var label = new Label();
        label.Text = text;
        label.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
        zeile.AddChild(label);
        return zeile;
    }

    private void LeereListe(VBoxContainer liste)
    {
        foreach (Node kind in liste.GetChildren())
            kind.QueueFree();
    }

    public void ToggleSichtbarkeit()
    {
        
        Visible = !Visible;
        if (Visible) _friendManager.LadeAlles();
    }
}