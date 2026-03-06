using Godot;

public partial class Papierflugzeug : RigidBody3D
{
    [Export] public float Schub = 5f;
    [Export] public float Auftrieb = 2f;
    [Export] public float Luftwiderstand = 0.5f;
    [Export] public float Nickrate = 3f;
    [Export] public float WurfStärke = 10f;


    private bool _außerhalbBildschirm = false;
    private float _außerhalbTimer = 0f;
    private const float TIMEOUT = 5f;



    private bool _gehalten = false;
    private Camera3D _kamera;

    [Export] 
    public NodePath MeshPfad; 
    private Node3D _meshGimbal;
    


    private float _schwankTimer = 0f;
    private float _letzterSchwankWinkel = 0f;


    WebSocketManager _webSocketManager;

    [Export]
    Texture2D _meinImage;

    private bool _amBoden = false;


   

    public override void _Ready()
    { 
        GravityScale = 1f;
        LinearVelocity = new Vector3(Schub, 0, 0);
        AxisLockAngularX = true;
        AxisLockAngularY = true;
        AxisLockLinearZ = true;

        _meshGimbal = GetNode<Node3D>(MeshPfad);

        _kamera = GetViewport().GetCamera3D();
        _webSocketManager = GetNode<WebSocketManager>("/root/WebSocketManager");


        ContactMonitor = true;
        MaxContactsReported = 4;

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

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


    private void OnBodyEntered(Node body)
    {
        if (body is StaticBody3D)
        {
            _amBoden = true;
           
        }
    }

    private void OnBodyExited(Node body)
    {
        if (body is StaticBody3D)
        {
            _amBoden = false;
            
        }
    }

    public override void _Process(double delta)
    {
        if (_meshGimbal == null) return;

        #region In screen check
        var screenPos = _kamera.UnprojectPosition(GlobalPosition);
        var screenSize = GetViewport().GetVisibleRect().Size;

        bool rechts = screenPos.X > screenSize.X;
        bool links = screenPos.X < 0;
        bool oben = screenPos.Y < 0;
        bool unten = screenPos.Y > screenSize.Y;

        bool außerhalb = rechts || links || oben || unten;

        if (rechts && !_außerhalbBildschirm)
        {
            RechtenRandVerlassen();
        }

        if (außerhalb)
        {

            _außerhalbBildschirm = true;
            _außerhalbTimer += (float)delta;

            if (_außerhalbTimer >= TIMEOUT)
            {
                _außerhalbTimer = 0f;
                TimeoutAußerhalb();
            }
        }
        else
        {
            _außerhalbBildschirm = false;
            _außerhalbTimer = 0f;
        }
        #endregion

        float geschwindigkeit = LinearVelocity.Length();

        if (_gehalten)
        {
            _meshGimbal.Rotation = new Vector3(0, 0, 0);
            return;
        }

        if (geschwindigkeit < 0.5f)
        {
            float zielNeigung = _letzterSchwankWinkel >= 0 ?
                Mathf.DegToRad(45f) : Mathf.DegToRad(-45f);
            _meshGimbal.Rotation = new Vector3(
                Mathf.LerpAngle(_meshGimbal.Rotation.X, zielNeigung, 0.1f),
                0, 0);
            return;
        }

        float schwankStärke = Mathf.Clamp(1f - (geschwindigkeit / Schub), 0f, 1f);
        schwankStärke *= 0.3f;

        _schwankTimer += (float)delta * (2f + schwankStärke * 3f);
        float schwankWinkel = Mathf.Sin(_schwankTimer) * schwankStärke;
        _letzterSchwankWinkel = schwankWinkel;

        _meshGimbal.Rotation = new Vector3(schwankWinkel, 0, 0);


       
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

            // Nur rotieren wenn schnell genug
            if (LinearVelocity.Length() > 0.5f)
            {
                float halteWinkel = Mathf.Atan2(LinearVelocity.Y, LinearVelocity.X);
                float halteDifferenz = halteWinkel - Rotation.Z;

                while (halteDifferenz > Mathf.Pi) halteDifferenz -= Mathf.Tau;
                while (halteDifferenz < -Mathf.Pi) halteDifferenz += Mathf.Tau;

                AngularVelocity = new Vector3(0, 0, halteDifferenz * Nickrate * 10f);
            }
            else
            {
                AngularVelocity = Vector3.Zero;
            }
            return;
        }

        float f = (float)delta;
        float geschwindigkeit = LinearVelocity.Length();

        if (_amBoden)//geschwindigkeit < 0.5f)
        {
            AngularVelocity = Vector3.Zero;
            //LinearVelocity = Vector3.Zero; // NEU - stoppt auch das Rutschen
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


    private void RechtenRandVerlassen()
    {

        _webSocketManager.SendeBild("01", _meinImage.GetImage(), Position, LinearVelocity, Rotation);
        QueueFree();
    }


    private void TimeoutAußerhalb()
    {

        Position = new Vector3(0,0,0);
    }


}