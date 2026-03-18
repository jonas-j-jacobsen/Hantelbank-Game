using Godot;

public partial class Papierblock : Area3D
{
    private UIManager _uiManager;

    public override void _Ready()
    {
        _uiManager = GetNode<UIManager>("/root/Main/PaperUI/CanvasLayer");
        InputEvent += OnInputEvent;
    }

    private void OnInputEvent(Node camera, InputEvent inputEvent,
        Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (inputEvent is InputEventMouseButton mb &&
            mb.ButtonIndex == MouseButton.Left && mb.Pressed)
        {
            _uiManager.SetzeUrsprung(GlobalPosition);
            _uiManager.AllesÖffnen();
        }
    }
}