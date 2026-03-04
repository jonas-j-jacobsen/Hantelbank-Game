using Godot;

public partial class ClickableObject : Area3D
{
    private WindowManager _windowManager;

    float targetY = .5f;
    Vector3 defaultPosition;

    int bigLoop;
    int mediumLoop;
    int smallLoop;

   [Export]
    public PackedScene TestWeight { get; set; }

    [Export]
    public PackedScene clickParticle;


    public override void _Ready()
    {
        Monitoring = true;
        Monitorable = true;

        _windowManager = GetNode<WindowManager>("/root/WindowManager");

        InputEvent += OnInputEvent;

        defaultPosition = Position;
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
                    if(mediumLoop >= maxAmmount)
                    {
                        bigLoop ++;
                        mediumLoop = 0;

                        //add test weights

                        var instance1 = TestWeight.Instantiate<Node3D>();
                        AddChild(instance1);
                        instance1.Position = new Vector3(0.86f+ bigLoop*.03f, 0, 0);

                        var instance2 = TestWeight.Instantiate<Node3D>();
                        AddChild(instance2);
                        instance2.Position = new Vector3(-0.86f - bigLoop * .03f, 0, 0);

                    }
                    
                }

                GD.Print("sL: " + smallLoop, ", mL: " + mediumLoop + ", bL: " + bigLoop);

                Position = new Vector3(defaultPosition.X, Mathf.Lerp(defaultPosition.Y, defaultPosition.Y + targetY, (float)smallLoop/clicksToDo), defaultPosition.Z);
            }
        }
    }
}