using Godot;

public partial class FarbPalette : Control
{
    [Signal] public delegate void FarbeGewähltEventHandler(Color farbe);

    public override void _Ready()
    {
        var grid = GetNode<GridContainer>("GridContainer");
        foreach (var node in grid.GetChildren())
        {
            if (node is Button btn)
            {
                btn.Pressed += () =>
                {
                    EmitSignal(SignalName.FarbeGewählt, btn.Modulate);
                };
            }
        }
    }
}