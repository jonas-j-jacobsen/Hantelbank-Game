using Godot;
using System.Collections.Generic;

public partial class MousePassthroughManager : Node3D
{
    private WindowManager _windowManager;
    private List<Control> _uiElemente = new();
    private bool _gesperrt = false;

    public override void _Ready()
    {
        _windowManager = GetNode<WindowManager>("/root/WindowManager");

        RegistriereUI(GetNodeOrNull<Control>("/root/Main/PaperUI/CanvasLayer/ControlEasel"));
        RegistriereUI(GetNodeOrNull<Control>("/root/Main/PaperUI/CanvasLayer/ControlToolbox"));
        RegistriereUI(GetNodeOrNull<Control>("/root/Main/PaperUI/CanvasLayer/ControlActions"));
        RegistriereUI(GetNodeOrNull<Control>("/root/Main/CanvasLayer/LoginPanel"));
        RegistriereUI(GetNodeOrNull<Control>("/root/Main/CanvasLayer/UsernamePanel"));
        RegistriereUI(GetNodeOrNull<Control>("/root/Main/CanvasLayer/LadePanel"));
        RegistriereUI(GetNodeOrNull<Control>("/root/Main/CanvasLayer/ConnectionsPanel"));
        RegistriereUI(GetNodeOrNull<Control>("/root/Main/PaperUI/CanvasLayer/ControlRecipients"));
        RegistriereUI(GetNodeOrNull<Control>("/root/Main/PaperUI/CanvasLayer/FriendsPanel"));
    }

    public void RegistriereUI(Control ui)
    {
        if (ui != null && !_uiElemente.Contains(ui))
            _uiElemente.Add(ui);
    }

    public void EntferneUI(Control ui)
    {
        _uiElemente.Remove(ui);
    }

    public void PassthroughSperren()
    {
        _gesperrt = true;
        _windowManager.SetClickThrough(false);
    }

    public void PassthroughEntsperren()
    {
        _gesperrt = false;
    }

    public override void _Process(double delta)
    {

        if (_gesperrt) return;

        var mousePos = _windowManager.GetMausPosition();

        foreach (var ui in _uiElemente)
        {
            if (ui != null && ui.Visible && ui.GetGlobalRect().HasPoint(mousePos))
            {
                _windowManager.SetClickThrough(false);
                return;
            }
        }

        var camera = GetViewport().GetCamera3D();
        var spaceState = GetWorld3D().DirectSpaceState;
        var from = camera.ProjectRayOrigin(mousePos);
        var to = from + camera.ProjectRayNormal(mousePos) * 1000f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = 1;
        query.CollideWithAreas = true;
        var result = spaceState.IntersectRay(query);

        _windowManager.SetClickThrough(result.Count == 0);
    }
}