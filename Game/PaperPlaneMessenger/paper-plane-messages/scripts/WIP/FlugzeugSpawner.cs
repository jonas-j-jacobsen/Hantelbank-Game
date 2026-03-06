using Godot;

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
        // Linken Bildschirmrand bei Y=Mitte projizieren auf Z=0 Ebene
        var screenMitte = new Vector2(0, GetViewport().GetVisibleRect().Size.Y / 2);
        var from = _kamera.ProjectRayOrigin(screenMitte);
        var richtung = _kamera.ProjectRayNormal(screenMitte);

        // Auf Z=0 Ebene projizieren
        float t = (0f - from.Z) / richtung.Z;
        var weltPos = from + richtung * t;

        var flieger = FlugzeugScene.Instantiate<RigidBody3D>();
        GetTree().CurrentScene.AddChild(flieger);

        flieger.GlobalPosition = new Vector3(weltPos.X - 1f, positionY, 0f);
        flieger.LinearVelocity = new Vector3(velocityX, velocityY, 0);
        flieger.Rotation = new Vector3(0, 0, rotationZ);

        var mesh = flieger.GetNode<MeshInstance3D>("MeshInstance3D");
        var material = new StandardMaterial3D();
        material.AlbedoTexture = texture;
        mesh.MaterialOverride = material;

        GD.Print($"Flieger gespawnt bei: {flieger.GlobalPosition}");
    }
}