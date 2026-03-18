using Godot;
using System.Collections.Generic;

public partial class PalettenCarousel : HBoxContainer
{
    [Signal] public delegate void FarbeGewähltEventHandler(Color farbe);

    private List<FarbPalette> _paletten = new();
    private int _aktuellerIndex = 0;
    private Control _holder;
    private bool _animiert = false;

    private DrawingCanvas _canvas;

    public override void _Ready()
    {
        _canvas = GetNode<DrawingCanvas>("/root/Main/PaperUI/CanvasLayer/ControlEasel/DrawingCanvas");

        _holder = GetNode<Control>("PalettenHolder");
        _holder.ClipContents = true;

        GetNode<Button>("ZurückButton").Pressed += () => Navigiere(-1);
        GetNode<Button>("VorButton").Pressed += () => Navigiere(1);

        foreach (var node in _holder.GetChildren())
        {
            if (node is FarbPalette palette)
            {
                palette.FarbeGewählt += (f) => _canvas.SetzefarBe(f);
                _paletten.Add(palette);
            }
        }

        CallDeferred(MethodName.Init);
        _holder.Resized += Init;
    }

    private void Init()
    {
        for (int i = 0; i < _paletten.Count; i++)
        {
            _paletten[i].Position = new Vector2(i * _holder.Size.X, 0);
            _paletten[i].Visible = i == 0;
        }
        _paletten[0].Position = Vector2.Zero;
    }

    private void Navigiere(int richtung)
    {
        if (_paletten.Count == 0 || _animiert) return;
        int neuerIndex = (_aktuellerIndex + richtung + _paletten.Count) % _paletten.Count;
        Slide(_aktuellerIndex, neuerIndex, richtung);
        _aktuellerIndex = neuerIndex;
    }

    private void Slide(int von, int nach, int richtung)
    {
        _animiert = true;
        float breite = _holder.Size.X;

        var aktuelle = _paletten[von];
        var nächste = _paletten[nach];

        // Nächste Palette außerhalb positionieren
        nächste.Position = new Vector2(richtung * breite, 0);
        nächste.Visible = true;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(aktuelle, "position", new Vector2(-richtung * breite, 0), 0.3f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(nächste, "position", Vector2.Zero, 0.3f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            aktuelle.Visible = false;
            _animiert = false;
        }));
    }

    private void AktualisiereAnzeige(int index)
    {
        for (int i = 0; i < _paletten.Count; i++)
        {
            _paletten[i].Visible = i == index;
            _paletten[i].Position = Vector2.Zero;
        }
    }
}