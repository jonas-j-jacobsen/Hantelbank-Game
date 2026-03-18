using Godot;

public partial class Toolbar : VBoxContainer
{
    private     DrawingCanvas _canvas;

    public override void _Ready()
    {
        _canvas = GetNode<DrawingCanvas>("/root/Main/PaperUI/CanvasLayer/ControlEasel/DrawingCanvas");
     
        GetNode<Button>("Toolbar/RundButton").Pressed += () =>
            _canvas.SetzePinselTyp(DrawingCanvas.PinselTyp.Rund);
        GetNode<Button>("Toolbar/KreideButton").Pressed += () =>
            _canvas.SetzePinselTyp(DrawingCanvas.PinselTyp.Kreide);
        GetNode<Button>("Toolbar/RadiergummiButton").Pressed += () =>
            _canvas.SetzePinselTyp(DrawingCanvas.PinselTyp.Radiergummi);

        var slider = GetNode<HSlider>("GrößeSlider");
        slider.ValueChanged += (v) => _canvas.SetzePinselGröße((float)v);
    }
}