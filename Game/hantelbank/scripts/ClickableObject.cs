using Godot;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

public partial class ClickableObject : Area3D
{
    private WindowManager _windowManager;
    private NetworkManager _networkManager;

    float targetY = .5f;
    Vector3 defaultPosition;

    public int bigLoop;
    public int mediumLoop;
    public int smallLoop;

    [Export]
    public Node3D _heart;

    [Export] public float BobHeight = 0.2f;      // Wie hoch er schwebt
    [Export] public float BobSpeed = 1.5f;        // Wie schnell er schwebt
    [Export] public float RotationAmount = 0.05f; // Wie stark er sich dreht
    [Export] public float RotationSpeed = 0.8f;   // Wie schnell er sich dreht

    private Vector3 _startPosition;
    private float _timeOffset;




    [Export]
    public PackedScene TestWeight { get; set; }

    [Export]
    public PackedScene clickParticle;

    public override void _Ready()
    {
        Monitoring = true;
        Monitorable = true;

        _windowManager = GetNode<WindowManager>("/root/WindowManager");
        _networkManager = GetNode<NetworkManager>("/root/NetworkManager");

        _networkManager.SetHantel(this);

        _networkManager.ClickInfoErhalten += () => fetchOnlineClicks();

        //Test
        _networkManager.Registrieren("testBoy");
 


        InputEvent += OnInputEvent;

        defaultPosition = Position;


        _startPosition = Position;
        // Zufälliger Startpunkt damit mehrere Ballons nicht synchron sind
        _timeOffset = (float)GD.RandRange(0f, Mathf.Tau);


        var timer = new Timer();
        AddChild(timer);
        timer.WaitTime = 15.0f;
        timer.OneShot = true; // nur einmal feuern
        timer.Timeout += startDelayed;
        timer.Start();

    }


    void startDelayed()
    {
        //testing
        var timer = new Timer();
        AddChild(timer);
        timer.WaitTime = 0.3f;
        timer.Timeout += processFriendClick;
        timer.Start();
    }
 
    public override void _Process(double delta)
    {
        #region trackMouse
        var mousePos = GetViewport().GetMousePosition();
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
        #endregion


        float time = (float)Time.GetTicksMsec() / 1000f + _timeOffset;

        // Auf/Ab Bewegung
        float bobOffset = Mathf.Sin(time * BobSpeed) * BobHeight;
        _heart.Position = _startPosition + new Vector3(0, bobOffset, 0);

        // Leichte Rotation (X und Z für organisches Schaukeln)
        _heart.Rotation = new Vector3(
            Mathf.Sin(time * RotationSpeed * 0.7f) * RotationAmount,
            Rotation.Y, // Y-Rotation unangetastet lassen
            Mathf.Sin(time * RotationSpeed) * RotationAmount
        );


    }



    private void OnInputEvent(Node camera, InputEvent inputEvent,
                               Vector3 eventPosition, Vector3 normal, long shapeIdx)
    {
        if (inputEvent is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {

                processClick(eventPosition);
                _networkManager.SendeClick();
            }
        }
    }

    private void processClick(Vector3 eventPosition)
    {
        smallLoop++;
        int maxAmmount = 10 + (int)Mathf.Pow(bigLoop, 1.5);
        int clicksToDo = maxAmmount - mediumLoop;

        var pInstance = clickParticle.Instantiate<BaseClickParticle>();
        GetTree().Root.AddChild(pInstance);
        pInstance.Position = eventPosition;
        pInstance.setUpNumber(clicksToDo - smallLoop);


        if (smallLoop >= clicksToDo)
        {
            smallLoop = 0;
            mediumLoop++;
            if (mediumLoop >= maxAmmount)
            {
                bigLoop++;
                mediumLoop = 0;

                //add test weights

                var instance1 = TestWeight.Instantiate<Node3D>();
                AddChild(instance1);
                instance1.Position = new Vector3(1.23f + bigLoop * .03f, 0, 0);

                var instance2 = TestWeight.Instantiate<Node3D>();
                AddChild(instance2);
                instance2.Position = new Vector3(-1.23f - bigLoop * .03f, 0, 0);

            }

        }

        //GD.Print("sL: " + smallLoop, ", mL: " + mediumLoop + ", bL: " + bigLoop);

        Position = new Vector3(defaultPosition.X, Mathf.Lerp(defaultPosition.Y, defaultPosition.Y + targetY, (float)smallLoop / clicksToDo), defaultPosition.Z);
    }



    private void processFriendClick()
    {
        smallLoop++;
        int maxAmmount = 10 + (int)Mathf.Pow(bigLoop, 1.5);
        int clicksToDo = maxAmmount - mediumLoop;

        var pInstance = clickParticle.Instantiate<BaseClickParticle>();
        GetTree().Root.AddChild(pInstance);
        pInstance.Position = this.Position + new Vector3((float)GD.RandRange(-.1f,.1f),_heart.Position.Y,-3);
        pInstance.setUpNumber(clicksToDo - smallLoop);
        pInstance.setUpText("xXmausXx denkt an dich");

        _heart.Scale += new Vector3(.001f, .001f, .001f);

        if (smallLoop >= clicksToDo)
        {
            smallLoop = 0;
            mediumLoop++;
            if (mediumLoop >= maxAmmount)
            {
                bigLoop++;
                mediumLoop = 0;


            }

        }

        //GD.Print("sL: " + smallLoop, ", mL: " + mediumLoop + ", bL: " + bigLoop);

        //new Vector3(defaultPosition.X, Mathf.Lerp(defaultPosition.Y, defaultPosition.Y + targetY, (float)smallLoop / clicksToDo), defaultPosition.Z);
    }


    private void fetchOnlineClicks()
    {
        GD.Print("fetched");
        foreach (var info in _networkManager.LetzteClicks)
        {
            GD.Print($"{info.Username} hat {info.Clicks} Clicks geschickt!");
            // Hier später UI-Element anzeigen
        }
    }
}