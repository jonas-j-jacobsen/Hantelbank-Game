using Godot;
using System.Collections.Generic;

public partial class EmpfängerAuswahl : DraggablePanel
{
    private FriendManager _friendManager;
    private FreundeUI _freundeUI;

    private VBoxContainer _freundeListe;
    private Button _freundeBestätigenButton;
    private string _gewählterFreundId = null;
    private string _gewählterFreundName = null;

    private VBoxContainer _gruppenListe;
    private Button _gruppenBestätigenButton;
    private string _gewählteGruppeId = null;
    private string _gewählteGruppeName = null;

    private Button _schliessenButton;
    private Button _freundeHinzufügenButton;
    private LineEdit _sucheInput;

    private List<FriendManager.Friend> _alleFreunde = new();
    private List<FriendManager.Group> _alleGruppen = new();

    [Signal] public delegate void EmpfängerGewähltEventHandler(string id, bool istGruppe, string name);

    public override void _Ready()
    {
        _friendManager = GetNode<FriendManager>("/root/FriendManager");
        _freundeUI = GetNode<FreundeUI>("/root/Main/PaperUI/CanvasLayer/FriendsPanel");

        _freundeListe = GetNode<VBoxContainer>("VBoxContainer/TabContainer/Freunde/ScrollContainer/VBoxContainer");
        _freundeBestätigenButton = GetNode<Button>("VBoxContainer/TabContainer/Freunde/Button");
        _gruppenListe = GetNode<VBoxContainer>("VBoxContainer/TabContainer/Gruppen/ScrollContainer/VBoxContainer");
        _gruppenBestätigenButton = GetNode<Button>("VBoxContainer/TabContainer/Gruppen/Button");
        _schliessenButton = GetNode<Button>("VBoxContainer/HBoxContainer/ButtonClose");
        _freundeHinzufügenButton = GetNode<Button>("VBoxContainer/HBoxContainer/ButtonAdd");
        _sucheInput = GetNode<LineEdit>("VBoxContainer/SucheInput");

        _freundeBestätigenButton.Pressed += OnFreundBestätigt;
        _gruppenBestätigenButton.Pressed += OnGruppeBestätigt;
        _schliessenButton.Pressed += () => Visible = false;
        _freundeHinzufügenButton.Pressed += () => _freundeUI.ToggleSichtbarkeit();
        _sucheInput.TextChanged += OnSucheGeändert;

        _freundeBestätigenButton.Disabled = true;
        _gruppenBestätigenButton.Disabled = true;


        var authManager = GetNode<AuthManager>("/root/AuthManager");
        GetNode<Label>("VBoxContainer/HBoxContainer/UserName").Text = authManager.Username;

        Visible = false;
    }

    public async void Öffne()
    {
        _gewählterFreundId = null;
        _gewählterFreundName = null;
        _gewählteGruppeId = null;
        _gewählteGruppeName = null;
        _freundeBestätigenButton.Disabled = true;
        _gruppenBestätigenButton.Disabled = true;
        _sucheInput.Text = "";

        Visible = true;

        // Online Freunde und Gruppen laden
        _alleFreunde = await _friendManager.LadeOnlineFreunde();
        _alleGruppen = _friendManager.Gruppen.FindAll(g => g.Aktiv);

        AktualisiereFreundeListe("");
        AktualisiereGruppenListe("");
    }

    private void OnSucheGeändert(string text)
    {
        AktualisiereFreundeListe(text);
        AktualisiereGruppenListe(text);
    }

    private void AktualisiereFreundeListe(string filter)
    {
        LeereListe(_freundeListe);
        _gewählterFreundId = null;
        _freundeBestätigenButton.Disabled = true;

        var gefiltert = _alleFreunde.FindAll(f =>
            f.Username.ToLower().Contains(filter.ToLower()));

        if (gefiltert.Count > 0)
        {
            var buttonGroup = new ButtonGroup();
            foreach (var freund in gefiltert)
            {
                var zeile = new HBoxContainer();
                var label = new Label();
                label.Text = "🟢 " + freund.Username;
                label.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;

                var btn = new CheckBox();
                btn.ButtonGroup = buttonGroup;
                var id = freund.UserId;
                var name = freund.Username;
                btn.Pressed += () =>
                {
                    _gewählterFreundId = id;
                    _gewählterFreundName = name;
                    _freundeBestätigenButton.Disabled = false;
                };

                zeile.AddChild(label);
                zeile.AddChild(btn);
                _freundeListe.AddChild(zeile);
            }
        }
        else
        {
            var label = new Label();
            label.Text = filter == "" ? "Keine Freunde online" : "Keine Ergebnisse";
            _freundeListe.AddChild(label);
        }
    }

    private void AktualisiereGruppenListe(string filter)
    {
        LeereListe(_gruppenListe);
        _gewählteGruppeId = null;
        _gruppenBestätigenButton.Disabled = true;

        var gefiltert = _alleGruppen.FindAll(g =>
            g.Name.ToLower().Contains(filter.ToLower()));

        if (gefiltert.Count > 0)
        {
            var buttonGroup = new ButtonGroup();
            foreach (var gruppe in gefiltert)
            {
                var zeile = new HBoxContainer();
                var label = new Label();
                label.Text = gruppe.Name + (gruppe.InviteOnly ? " 🔒" : "");
                label.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;

                var btn = new CheckBox();
                btn.ButtonGroup = buttonGroup;
                var id = gruppe.GroupId;
                var name = gruppe.Name;
                btn.Pressed += () =>
                {
                    _gewählteGruppeId = id;
                    _gewählteGruppeName = name;
                    _gruppenBestätigenButton.Disabled = false;
                };

                zeile.AddChild(label);
                zeile.AddChild(btn);
                _gruppenListe.AddChild(zeile);
            }
        }
        else
        {
            var label = new Label();
            label.Text = filter == "" ? "Keine Gruppen vorhanden" : "Keine Ergebnisse";
            _gruppenListe.AddChild(label);
        }
    }

    private void OnFreundBestätigt()
    {
        if (_gewählterFreundId == null) return;
        EmitSignal(SignalName.EmpfängerGewählt, _gewählterFreundId, false, _gewählterFreundName);
        Visible = false;
    }

    private void OnGruppeBestätigt()
    {
        if (_gewählteGruppeId == null) return;
        EmitSignal(SignalName.EmpfängerGewählt, _gewählteGruppeId, true, _gewählteGruppeName);
        Visible = false;
    }

    private void LeereListe(VBoxContainer liste)
    {
        foreach (Node kind in liste.GetChildren())
            kind.QueueFree();
    }
}
