using Godot;

public partial class RechteWand : StaticBody3D
{
    public override void _Ready()
    {
        var kamera = GetViewport().GetCamera3D();
        var screenSize = GetViewport().GetVisibleRect().Size;

        var from = kamera.ProjectRayOrigin(new Vector2(screenSize.X, screenSize.Y / 2));
        var richtung = kamera.ProjectRayNormal(new Vector2(screenSize.X, screenSize.Y / 2));
        float t = (0f - from.Z) / richtung.Z;
        var weltPos = from + richtung * t;

        Position = new Vector3(weltPos.X, Position.Y, 0f);
        
    }
}