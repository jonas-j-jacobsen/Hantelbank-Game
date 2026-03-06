using Godot;

public partial class PlaneTester : RigidBody3D
{
    [Export] public float Schub = 5f;
    [Export] public float Auftrieb = 2f;
    [Export] public float Luftwiderstand = 0.5f;
    [Export] public float Nickrate = 3f;
    [Export] public float WurfStärke = 10f;

    private bool _gehalten = false;
    private Camera3D _kamera;

    [Export] public NodePath MeshPfad; 
    private Node3D _mesh;
    
    private float _schwankTimer = 0f;
    private float _letzterSchwankWinkel = 0f;  

    public override void _Ready()
    { 
        GravityScale = 1f;
        LinearVelocity = new Vector3(Schub, 0, 0);
        AxisLockAngularX = true;
        AxisLockAngularY = true;
        AxisLockLinearZ = true;

        _mesh = GetNode<Node3D>(MeshPfad);

        _kamera = GetViewport().GetCamera3D();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    // Prüfen ob Flieger geklickt wurde
                    var from = _kamera.ProjectRayOrigin(mouseButton.Position);
                    var to = from + _kamera.ProjectRayNormal(mouseButton.Position) * 1000f;
                    var spaceState = GetWorld3D().DirectSpaceState;
                    var query = PhysicsRayQueryParameters3D.Create(from, to);
                    var result = spaceState.IntersectRay(query);

                    if (result.Count > 0 && result["collider"].AsGodotObject() == this)
                    {
                        _gehalten = true;
                        GravityScale = 0f;
                        LinearVelocity = Vector3.Zero;
                        AngularVelocity = Vector3.Zero;
                    }
                }
                else
                {
                    if (_gehalten)
                    {
                        _gehalten = false;
                        GravityScale = 1f;
                    }
                }
            }
        }
    }


    public override void _Process(double delta)
    {
        if (_mesh == null) return;

        float geschwindigkeit = LinearVelocity.Length();

        if (_gehalten)
        {
            _mesh.Rotation = new Vector3(0, 0, 0);
            return;
        }

        if (geschwindigkeit < 0.5f)
        {
            float zielNeigung = _letzterSchwankWinkel >= 0 ?
                Mathf.DegToRad(45f) : Mathf.DegToRad(-45f);
            _mesh.Rotation = new Vector3(
                Mathf.LerpAngle(_mesh.Rotation.X, zielNeigung, 0.1f),
                0, 0);
            return;
        }

        float schwankStärke = Mathf.Clamp(1f - (geschwindigkeit / Schub), 0f, 1f);
        schwankStärke *= 0.3f;

        _schwankTimer += (float)delta * (2f + schwankStärke * 3f);
        float schwankWinkel = Mathf.Sin(_schwankTimer) * schwankStärke;
        _letzterSchwankWinkel = schwankWinkel;

        _mesh.Rotation = new Vector3(schwankWinkel, 0, 0);
    }


    public override void _PhysicsProcess(double delta)
    {
        if (_gehalten)
        {
            var mausPos = GetViewport().GetMousePosition();
            var from = _kamera.ProjectRayOrigin(mausPos);
            var richtung = _kamera.ProjectRayNormal(mausPos);

            float t = (GlobalPosition.Z - from.Z) / richtung.Z;
            var zielPos = from + richtung * t;
            zielPos.Z = GlobalPosition.Z;

            LinearVelocity = (zielPos - GlobalPosition) * 10f;

            if (LinearVelocity.Length() > 0.1f)
            {
                float halteWinkel = Mathf.Atan2(LinearVelocity.Y, LinearVelocity.X); // umbenannt
                float halteDifferenz = halteWinkel - Rotation.Z; // umbenannt

                while (halteDifferenz > Mathf.Pi) halteDifferenz -= Mathf.Tau;
                while (halteDifferenz < -Mathf.Pi) halteDifferenz += Mathf.Tau;

                AngularVelocity = new Vector3(0, 0, halteDifferenz * Nickrate * 10f);
            }
            return;
        }

        float f = (float)delta;
        float geschwindigkeit = LinearVelocity.Length();

        if (geschwindigkeit < 0.5f)
        {
            AngularVelocity = Vector3.Zero;
            return;
        }

        // Auftrieb
        Vector3 auftriebsRichtung = Transform.Basis.Y;
        ApplyCentralForce(auftriebsRichtung * Auftrieb * geschwindigkeit * f);

        // Luftwiderstand
        ApplyCentralForce(-LinearVelocity * Luftwiderstand * f);

        // Zielwinkel
        float zielWinkel = Mathf.Atan2(LinearVelocity.Y, LinearVelocity.X);
        float winkelDifferenz = zielWinkel - Rotation.Z;

        while (winkelDifferenz > Mathf.Pi) winkelDifferenz -= Mathf.Tau;
        while (winkelDifferenz < -Mathf.Pi) winkelDifferenz += Mathf.Tau;

        AngularVelocity = new Vector3(0, 0, winkelDifferenz * Nickrate * 10f);
    }
}