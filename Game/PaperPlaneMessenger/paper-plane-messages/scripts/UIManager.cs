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
        // Pivot auf Ursprung setzen damit es von dort aufgeht
        panel.PivotOffset = _ursprung - panel.GlobalPosition;
        panel.Scale = Vector2.Zero;
        panel.Visible = true;

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(panel, "scale", Vector2.One, 0.4f);
        tween.TweenCallback(Callable.From(() =>
        {
            // Pivot nach Öffnen zurück zur Mitte
            panel.PivotOffset = panel.Size / 2f;
        }));
    }

    public void SchließePanel(Control panel)
    {
        panel.PivotOffset = _ursprung - panel.GlobalPosition;

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(panel, "scale", Vector2.Zero, 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            panel.Visible = false;
        }));
    }
}