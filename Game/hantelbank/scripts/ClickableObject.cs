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
        pInstance.Position = this.Position + new Vector3((float)GD.RandRange(-1.5f,-1.3f),0,1);
        pInstance.setUpNumber(clicksToDo - smallLoop);
        pInstance.setUpText("xXstarke_maus420Xx : +1");


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

        Position = new Vector3(defaultPosition.X, Mathf.Lerp(defaultPosition.Y, defaultPosition.Y + targetY, (float)smallLoop / clicksToDo), defaultPosition.Z);
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