using Godot;
using System.Drawing;

public partial class FlugzeugSpawner : Node
{
    [Export] 
    public PackedScene FlugzeugScene; // Im Inspector auf Papierflieger Scene zeigen
    private WebSocketManager _wsManager;
    private Camera3D _kamera;

    public override void _Ready()
    {
        _wsManager = GetNode<WebSocketManager>("/root/WebSocketManager");
        _kamera = GetViewport().GetCamera3D();

        _wsManager.BildErhalten += OnBildErhalten;

    }

   


    private void OnBildErhalten(string senderName, ImageTexture texture,
    float positionY, float velocityX, float velocityY, float rotationZ)
    {
        var screenMitte = new Vector2(0, GetViewport().GetVisibleRect().Size.Y / 2);
        var from = _kamera.ProjectRayOrigin(screenMitte);
        var richtung = _kamera.ProjectRayNormal(screenMitte);
        float t = (0f - from.Z) / richtung.Z;
        var weltPos = from + richtung * t;

        var flieger = FlugzeugScene.Instantiate<Papierflugzeug>(); // NEU
        GetTree().CurrentScene.AddChild(flieger);

        flieger.GlobalPosition = new Vector3(weltPos.X - 1f, positionY, 0f);
        flieger.LinearVelocity = new Vector3(velocityX, velocityY, 0);
        flieger.Rotation = new Vector3(0, 0, rotationZ);
        flieger._image = texture.GetImage(); // NEU
        flieger.SenderId = senderName;       // NEU
    }
}