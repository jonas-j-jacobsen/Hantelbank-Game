using Godot;
using System;

public partial class MousePassthroughManager : Node3D
{
    private WindowManager _windowManager;


    public override void _Ready()
    {
        _windowManager = GetNode<WindowManager>("/root/WindowManager");
    }


    public override void _Process(double delta)
    {
        var mousePos = _windowManager.GetMausPosition();
        var camera = GetViewport().GetCamera3D();


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
