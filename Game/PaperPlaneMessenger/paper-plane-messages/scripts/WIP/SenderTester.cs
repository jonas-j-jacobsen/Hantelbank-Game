using Godot;
using System;

public partial class SenderTester : Area3D
{

    WebSocketManager _webSocketManager;

    [Export]
    Texture2D _meinImage;

    public override void _Ready()
    {
        _webSocketManager = GetNode<WebSocketManager>("/root/WebSocketManager");
        InputEvent += OnInputEvent;
    }


    private void OnInputEvent(Node camera, InputEvent inputEvent,
                           Vector3 eventPosition, Vector3 normal, long shapeIdx)
    {
        if (inputEvent is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {

                _webSocketManager.SendeBild("01", _meinImage.GetImage());

            }
        }
    }

}
