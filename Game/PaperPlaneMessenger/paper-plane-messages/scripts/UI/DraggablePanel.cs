using Godot;

public partial class DraggablePanel : Control
{
    private bool _dragging = false;
    private Vector2 _dragOffset;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Right)
        {
            if (mb.Pressed && GetGlobalRect().HasPoint(mb.Position))
            {
                if (IstOberstesPanel(mb.Position))
                {
                    _dragging = true;
                    _dragOffset = mb.Position - GlobalPosition;
                }
            }
            else
            {
                _dragging = false;
            }
        }

        if (@event is InputEventMouseMotion mm && _dragging)
        {
            var neuePos = mm.Position - _dragOffset;
            var screenSize = GetViewport().GetVisibleRect().Size;
            var panelSize = Size;

            float minX = -panelSize.X * 2f / 3f;
            float maxX = screenSize.X - panelSize.X / 3f;
            float minY = -panelSize.Y * 2f / 3f;
            float maxY = screenSize.Y - panelSize.Y / 3f;

            GlobalPosition = new Vector2(
                Mathf.Clamp(neuePos.X, minX, maxX),
                Mathf.Clamp(neuePos.Y, minY, maxY)
            );
        }
    }

    private bool IstOberstesPanel(Vector2 mausPos)
    {
        foreach (var node in GetParent().GetChildren())
        {
            if (node is DraggablePanel anderes && anderes != this)
            {
                if (anderes.GetIndex() > GetIndex() && anderes.GetGlobalRect().HasPoint(mausPos))
                    return false;
            }
        }
        return true;
    }
}