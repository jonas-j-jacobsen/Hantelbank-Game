using Godot;

public partial class Papierblock : Area3D
{
    private UIManager _uiManager;
    private Camera3D _kamera;
    private bool _gehalten = false;
    private Vector3 _dragOffset;
    
    public override void _Ready()
    {
        _uiManager = GetNode<UIManager>("/root/Main/PaperUI/CanvasLayer");
        _kamera = GetViewport().GetCamera3D();
        InputEvent += OnInputEvent;
    }

    private void OnInputEvent(Node camera, InputEvent inputEvent,
        Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (inputEvent is InputEventMouseButton mb)
        {
            if (mb.Pressed)
            {
                if (mb.ButtonIndex == MouseButton.Left)
                {
                    _uiManager.SetzeUrsprung(GlobalPosition);
                    _uiManager.AllesÖffnen();
                }
                else if (mb.ButtonIndex == MouseButton.Right)
                {
                    _gehalten = true;
                    // Offset zwischen Block und Mausposition speichern
                    var mausPos = GetViewport().GetMousePosition();
                    var from = _kamera.ProjectRayOrigin(mausPos);
                    var richtung = _kamera.ProjectRayNormal(mausPos);
                    float t = (GlobalPosition.Z - from.Z) / richtung.Z;
                    var weltMausPos = from + richtung * t;
                    _dragOffset = GlobalPosition - weltMausPos;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                _gehalten = false;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!_gehalten) return;

        var mausPos = GetViewport().GetMousePosition();
        var from = _kamera.ProjectRayOrigin(mausPos);
        var richtung = _kamera.ProjectRayNormal(mausPos);
        float t = (GlobalPosition.Z - from.Z) / richtung.Z;
        var zielPos = from + richtung * t;
        zielPos.X += _dragOffset.X;
        zielPos.Y += _dragOffset.Y;

        // Bildschirmränder in Weltkoordinaten berechnen
        var screenSize = GetViewport().GetVisibleRect().Size;

        var fromLinks = _kamera.ProjectRayOrigin(new Vector2(0, screenSize.Y / 2));
        var richtungLinks = _kamera.ProjectRayNormal(new Vector2(0, screenSize.Y / 2));
        float tLinks = (GlobalPosition.Z - fromLinks.Z) / richtungLinks.Z;
        float minX = (fromLinks + richtungLinks * tLinks).X;

        var fromRechts = _kamera.ProjectRayOrigin(new Vector2(screenSize.X, screenSize.Y / 2));
        var richtungRechts = _kamera.ProjectRayNormal(new Vector2(screenSize.X, screenSize.Y / 2));
        float tRechts = (GlobalPosition.Z - fromRechts.Z) / richtungRechts.Z;
        float maxX = (fromRechts + richtungRechts * tRechts).X;

        var fromOben = _kamera.ProjectRayOrigin(new Vector2(screenSize.X / 2, 0));
        var richtungOben = _kamera.ProjectRayNormal(new Vector2(screenSize.X / 2, 0));
        float tOben = (GlobalPosition.Z - fromOben.Z) / richtungOben.Z;
        float maxY = (fromOben + richtungOben * tOben).Y;

        var fromUnten = _kamera.ProjectRayOrigin(new Vector2(screenSize.X / 2, screenSize.Y));
        var richtungUnten = _kamera.ProjectRayNormal(new Vector2(screenSize.X / 2, screenSize.Y));
        float tUnten = (GlobalPosition.Z - fromUnten.Z) / richtungUnten.Z;
        float minY = (fromUnten + richtungUnten * tUnten).Y;

        GlobalPosition = new Vector3(
            Mathf.Clamp(zielPos.X, minX, maxX),
            Mathf.Clamp(zielPos.Y, minY, maxY),
            GlobalPosition.Z
        );
    }
}