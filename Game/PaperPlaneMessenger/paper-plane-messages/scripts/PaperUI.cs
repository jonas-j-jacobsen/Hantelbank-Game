using Godot;
using System.Collections.Generic;

public partial class PaperUI : Control
{
    // Referenzen
    private DrawingCanvas _canvas;
    private Label _senderLabel;
    private ItemList _empfängerListe;
    private Tween _tween;

    // Zustand
    public bool IstOffen { get; private set; } = false;
    private string _absender = null;
    private Papierflugzeug _quellflieger = null;

    [Signal] public delegate void GeschlossenEventHandler();
    [Signal] public delegate void GefaltetEventHandler(Image bild, Godot.Collections.Array empfänger);

    public override void _Ready()
    {
        _canvas = GetNode<DrawingCanvas>("DrawingCanvas");
        _senderLabel = GetNode<Label>("SenderLabel");
        _empfängerListe = GetNode<ItemList>("EmpfängerListe/Liste");
        GD.Print(_empfängerListe);

        GetNode<Button>("FaltenButton").Pressed += OnFaltenPressed;
        GetNode<Button>("SpeichernButton").Pressed += OnSpeichernPressed;
        GetNode<Button>("LadenButton").Pressed += OnLadenPressed;
        GetNode<Button>("SchliessenButton").Pressed += OnSchliessenPressed;

        Visible = false;
        Scale = Vector2.Zero;
    }

    // Öffnen von Papierblock
    public void ÖffneNeu()
    {
        _absender = null;
        _quellflieger = null;
        _canvas.Leeren();
        _senderLabel.Text = "Original";
        Öffne();
    }

    // Öffnen von Papierflieger
    public void ÖffneVonFlieger(Papierflugzeug flieger)
    {
        _quellflieger = flieger;
        _absender = flieger.SenderId;
        _canvas.SetzeImage(flieger._image);
        _senderLabel.Text = _absender ?? "Original";

        // Flieger verstecken
        flieger.Visible = false;
        Öffne();
    }

    private void Öffne()
    {
        Visible = true;
        IstOffen = true;

        // Entfaltungs-Animation
        Scale = Vector2.Zero;
        _tween = CreateTween();
        _tween.SetEase(Tween.EaseType.Out);
        _tween.SetTrans(Tween.TransitionType.Back);
        _tween.TweenProperty(this, "scale", Vector2.One, 0.4f);

        // Click-Through deaktivieren solange UI offen
        GetNode<WindowManager>("/root/WindowManager").SetClickThrough(false);
    }

    private void Schliesse()
    {
        IstOffen = false;

        // Zusammenfalt-Animation
        _tween = CreateTween();
        _tween.SetEase(Tween.EaseType.In);
        _tween.SetTrans(Tween.TransitionType.Back);
        _tween.TweenProperty(this, "scale", Vector2.Zero, 0.3f);
        _tween.TweenCallback(Callable.From(() =>
        {
            Visible = false;
            if (_quellflieger != null)
                _quellflieger.QueueFree();
            GetNode<WindowManager>("/root/WindowManager").SetClickThrough(true);
            EmitSignal(SignalName.Geschlossen);
        }));
    }

    private void OnFaltenPressed()
    {
        var empfänger = new Godot.Collections.Array();
        for (int i = 0; i < _empfängerListe.ItemCount; i++)
        {
            if (_empfängerListe.IsSelected(i))
                empfänger.Add(_empfängerListe.GetItemText(i));
        }

        if (empfänger.Count == 0)
        {
            GD.Print("Kein Empfänger ausgewählt!");
            return;
        }

        EmitSignal(SignalName.Gefaltet, _canvas.GetImage(), empfänger);
        Schliesse();
    }

    private void OnSchliessenPressed() => Schliesse();

    private void OnSpeichernPressed()
    {
        var image = _canvas.GetImage();
        var path = "user://zeichnung.png";
        image.SavePng(path);
        GD.Print("Gespeichert: " + path);
    }

    private void OnLadenPressed()
    {
        var path = "user://zeichnung.png";
        if (FileAccess.FileExists(path))
        {
            var image = Image.LoadFromFile(path);
            _canvas.SetzeImage(image);
        }
    }
}