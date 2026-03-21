using Godot;
using System.Collections.Generic;

public partial class UIManager : CanvasLayer
{
    private List<Control> _panels = new();
    private Vector2 _ursprung = Vector2.Zero;

    private bool _istOffen = false;

    public override void _Ready()
    {
        // Alle DraggablePanel Kinder einsammeln
        foreach (var node in GetChildren())
        {
            if (node is Control panel)
            {
                _panels.Add(panel);
                panel.Visible = false;
                panel.Scale = Vector2.Zero;
                panel.PivotOffset = panel.Size / 2f;
            }
        }
    }

    // Ursprung setzen bevor geöffnet wird (Position des angeklickten Objekts)
    public void SetzeUrsprung(Vector3 weltPos)
    {
        var kamera = GetViewport().GetCamera3D();
        _ursprung = kamera.UnprojectPosition(weltPos);
        GD.Print("Ursprung gesetzt: " + _ursprung);
    }



    public void AllesÖffnen()
    {
        if (_istOffen) return;
        _istOffen = true;
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