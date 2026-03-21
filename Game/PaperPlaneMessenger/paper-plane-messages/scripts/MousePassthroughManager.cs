using Godot;
using System;

public partial class MousePassthroughManager : Node3D
{
    private WindowManager _windowManager;
    private Control[] _uiElemente;

    public override void _Ready()
    {
        _windowManager = GetNode<WindowManager>("/root/WindowManager");

        _uiElemente = new Control[]
    {
        GetNodeOrNull<Control>("/root/Main/PaperUI/CanvasLayer/ControlEasel"),
        GetNodeOrNull<Control>("/root/Main/PaperUI/CanvasLayer/ControlToolbox"),
        GetNodeOrNull<Control>("/root/Main/PaperUI/CanvasLayer/ControlActions"),
        GetNodeOrNull<Control>("/root/Main/CanvasLayer/LoginPanel"),
        GetNodeOrNull<Control>("/root/Main/CanvasLayer/UsernamePanel"),
        GetNodeOrNull<Control>("/root/Main/CanvasLayer/LadePanel")

    };
        GD.Print(_uiElemente.Length);   
        
    }


    public override void _Process(double delta)
    {

        var mousePos = _windowManager.GetMausPosition();
        var camera = GetViewport().GetCamera3D();


        // Prüfen ob Maus über einem UI Element ist
        foreach (var ui in _uiElemente)
        {
            if (ui != null && ui.Visible && ui.GetGlobalRect().HasPoint(mousePos))
            {
                _windowManager.SetClickThrough(false);
                return;
            }
        }


        var spaceState = GetWorld3D().DirectSpaceState;
        var from = camera.ProjectRayOrigin(mousePos);
        var to = from + camera.ProjectRayNormal(mousePos) * 1000f;

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = 1;
        query.CollideWithAreas = true;
        var result = spaceState.IntersectRay(query);

        bool mausÜberObjekt = result.Count > 0;
        _windowManager.SetClickThrough(!mausÜberObjekt);
    }
}
