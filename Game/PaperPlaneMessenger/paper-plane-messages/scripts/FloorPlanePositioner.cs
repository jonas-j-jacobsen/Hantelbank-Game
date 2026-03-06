using Godot;
using System;

public partial class FloorPlanePositioner : StaticBody3D
{
    public override void _Ready()
    {
        var kamera = GetViewport().GetCamera3D();
        var screenSize = GetViewport().GetVisibleRect().Size;

        var from = kamera.ProjectRayOrigin(new Vector2(screenSize.X / 2, screenSize.Y));
        var richtung = kamera.ProjectRayNormal(new Vector2(screenSize.X / 2, screenSize.Y));
        float t = (0f - from.Z) / richtung.Z;
        var weltPos = from + richtung * t;

        GlobalPosition = new Vector3(GlobalPosition.X, weltPos.Y, 0f);
    }
}