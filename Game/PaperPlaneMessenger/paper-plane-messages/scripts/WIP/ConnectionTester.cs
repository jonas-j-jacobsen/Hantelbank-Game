using Godot;
using System;

public partial class ConnectionTester : Area3D
{


    WebSocketManager _webSocketManager;

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

                _webSocketManager.Verbinden("01");

            }
        }
    }

}
