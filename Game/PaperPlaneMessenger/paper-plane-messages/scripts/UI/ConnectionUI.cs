using Godot;

public partial class ConnectionUI : CanvasLayer
{
    private AuthManager _authManager;
    private WebSocketManager _wsManager;
    private DraggablePanel _loginPanel;
    private DraggablePanel _usernamePanel;
    private DraggablePanel _ladePanel;
    private Button _loginButton;
    private LineEdit _usernameInput;
    private Button _bestätigenButton;
    private Label _fehlerLabel;
    private Label _ladeLabel;

    public override void _Ready()
    {
        _authManager = GetNode<AuthManager>("/root/AuthManager");
        _wsManager = GetNode<WebSocketManager>("/root/WebSocketManager");
        _loginPanel = GetNode<DraggablePanel>("LoginPanel");
        _usernamePanel = GetNode<DraggablePanel>("UsernamePanel");
        _ladePanel = GetNode<DraggablePanel>("LadePanel");
        _loginButton = GetNode<Button>("LoginPanel/LoginButton");
        _usernameInput = GetNode<LineEdit>("UsernamePanel/UsernameInput");
        _bestätigenButton = GetNode<Button>("UsernamePanel/BestätigenButton");
        _fehlerLabel = GetNode<Label>("UsernamePanel/FehlerLabel");
        _ladeLabel = GetNode<Label>("LadePanel/LadeLabel");

        _loginButton.Pressed += OnLoginPressed;
        _bestätigenButton.Pressed += OnBestätigenPressed;
        _authManager.Eingeloggt += OnEingeloggt;
        _authManager.UsernameWählen += OnUsernameWählen;
        _authManager.Fehler += OnFehler;
        _authManager.ValidierungFertig += OnValidierungFertig;
        _wsManager.Verbunden += OnVerbunden;
        _wsManager.VerbindungVerloren += OnVerbindungVerloren;

        // Wenn beim Start ein gespeicherter Token validiert wird:
        // Lade-Panel zeigen statt Login-Panel, damit User nicht irritiert ist.
        if (_authManager.ValidierungLäuft)
        {
            ZeigePanel(_ladePanel);
            _ladeLabel.Text = "Prüfe Login...";
        }
        else
        {
            ZeigePanel(_loginPanel);
        }
    }

    private void OnLoginPressed()
    {
        _loginButton.Disabled = true;
        ZeigePanel(_ladePanel);
        _ladeLabel.Text = "Warte auf Google Login...";
        _authManager.StarteLogin();
    }

    private void OnUsernameWählen()
    {
        ZeigePanel(_usernamePanel);
        _fehlerLabel.Text = "";
        _usernameInput.GrabFocus();
    }

    private void OnBestätigenPressed()
    {
        var username = _usernameInput.Text.Trim();
        if (username == "") return;
        _bestätigenButton.Disabled = true;
        _fehlerLabel.Text = "";
        _authManager.Registrieren(username);
    }

    private void OnEingeloggt()
    {
        ZeigePanel(_ladePanel);
        _ladeLabel.Text = "Verbinde mit Server...";
    }

    private void OnValidierungFertig(bool erfolgreich)
    {
        if (!erfolgreich)
        {
            // Token war ungültig oder Netzwerkfehler → User muss sich einloggen
            _loginButton.Disabled = false;
            ZeigePanel(_loginPanel);
        }
        // Bei erfolgreich: OnEingeloggt hat schon das Lade-Panel gezeigt.
    }

    private void OnVerbunden()
    {
        Visible = false;
        GetNode<WindowManager>("/root/WindowManager").SetClickThrough(true);
    }

    private void OnVerbindungVerloren()
    {
        Visible = true;
        ZeigePanel(_ladePanel);
        _ladeLabel.Text = "Verbindung verloren, reconnecte...";
    }

    private void OnFehler(string nachricht)
    {
        _bestätigenButton.Disabled = false;
        _fehlerLabel.Text = nachricht;
    }

    private void ZeigePanel(Control panel)
    {
        _loginPanel.Visible = panel == _loginPanel;
        _usernamePanel.Visible = panel == _usernamePanel;
        _ladePanel.Visible = panel == _ladePanel;
        GetNode<WindowManager>("/root/WindowManager").SetClickThrough(false);
    }
}