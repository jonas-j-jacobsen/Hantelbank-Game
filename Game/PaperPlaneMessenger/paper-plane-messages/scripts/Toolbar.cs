using Godot;

public partial class Toolbar : HBoxContainer
{
    private DrawingCanvas _canvas;

    [Export]
    private Color[] _palette = {
        Colors.Black, Colors.White, Colors.Red, Colors.Blue,
        Colors.Green, Colors.Yellow, Colors.Orange, Colors.Purple,
        new Color(0.5f, 0.3f, 0.1f), Colors.Pink, Colors.Cyan, Colors.Gray
    };

    public override void _Ready()
    {
        _canvas = GetNode<DrawingCanvas>("../DrawingCanvas");
        // Farbpalette erstellen
        var paletteContainer = GetNode<HBoxContainer>("Palette");
        foreach (var farbe in _palette)
        {
            var btn = new ColorRect();
            btn.CustomMinimumSize = new Vector2(24, 24);
            btn.Color = farbe;
            paletteContainer.AddChild(btn);

            // Click Handler
            var f = farbe;
            btn.GuiInput += (e) =>
            {
                if (e is InputEventMouseButton mb && mb.Pressed)
                    _canvas.SetzefarBe(f);
            };
        }

        // Pinsel Buttons
        GetNode<Button>("RundButton").Pressed += () =>
            _canvas.SetzePinselTyp(DrawingCanvas.PinselTyp.Rund);
        GetNode<Button>("KreideButton").Pressed += () =>
            _canvas.SetzePinselTyp(DrawingCanvas.PinselTyp.Kreide);
        GetNode<Button>("RadiergummiButton").Pressed += () =>
            _canvas.SetzePinselTyp(DrawingCanvas.PinselTyp.Radiergummi);

        // Pinselgröße
        var slider = GetNode<HSlider>("GrößeSlider");
        slider.ValueChanged += (v) => _canvas.SetzePinselGröße((float)v);
    }
}