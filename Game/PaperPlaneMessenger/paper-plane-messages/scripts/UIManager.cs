using Godot;
using System.Collections.Generic;

public partial class UIManager : CanvasLayer
{
    private List<Control> _panels = new();
    private Vector2 _ursprung = Vector2.Zero;

    private bool _istOffen = false;

    private DrawingCanvas _drawingCanvas;
    private Label _senderLabel;

    public override void _Ready()
    {
        // Alle DraggablePanel Kinder einsammeln
        foreach (var node in GetChildren())
        {
            if (node is DraggablePanel panel && panel is not EmpfängerAuswahl && panel is not FreundeUI )
            {
                _panels.Add(panel);
                panel.Visible = false;
                panel.Scale = Vector2.Zero;
            }
        }

        _drawingCanvas = GetNode<DrawingCanvas>("ControlEasel/DrawingCanvas");
        _senderLabel = GetNode<Label>("ControlEasel/DrawingCanvas/HBoxContainerSender/SenderLabel");
    }

    // Ursprung setzen bevor geöffnet wird (Position des angeklickten Objekts)
    public void SetzeUrsprung(Vector3 weltPos)
    {
        var kamera = GetViewport().GetCamera3D();
        _ursprung = kamera.UnprojectPosition(weltPos);
    }



    public void AllesÖffnen()
    {
        if (_istOffen) return;
        _istOffen = true;

        var authManager = GetNode<AuthManager>("/root/AuthManager");
        _senderLabel.Text = authManager.Username;

        foreach (var panel in _panels)
            ÖffnePanel(panel);
    }

    public void AllesÖffnen(Image img, string senderName)
    {
        if (_istOffen) return;
        _istOffen = true;

        _drawingCanvas.SetzeImage(img);
        var authManager = GetNode<AuthManager>("/root/AuthManager");
        _senderLabel.Text = senderName ?? authManager.Username;

        foreach (var panel in _panels)
            ÖffnePanel(panel);
    }

    public void AllesSchließen()
    {
        if (!_istOffen) return;
        _istOffen = false;
        foreach (var panel in _panels)
            SchließePanel(panel);
    }

    public void ÖffnePanel(Control panel)
    {
        var zielPosition = panel.Position;

        panel.Scale = Vector2.Zero;
        panel.Position = _ursprung;
        panel.PivotOffset = Vector2.Zero;
        panel.Visible = true;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(panel, "scale", Vector2.One, 0.4f);
        tween.TweenProperty(panel, "position", zielPosition, 0.4f);
    }


    public void SchließePanel(Control panel)
    {
        var localUrsprung = _ursprung - panel.GetGlobalRect().Position;
        panel.PivotOffset = localUrsprung;

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(panel, "scale", Vector2.Zero, 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            panel.Visible = false;
            panel.PivotOffset = panel.Size / 2f;
        }));
    }
}