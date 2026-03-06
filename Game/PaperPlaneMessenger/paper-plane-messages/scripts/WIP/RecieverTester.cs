using Godot;
using System;

public partial class RecieverTester : Sprite3D
{
    
    
    // BildAnzeige.cs auf dem Sprite3D
    public override void _Ready()
    {
        var ws = GetNode<WebSocketManager>("/root/WebSocketManager");
        ws.BildErhalten += (sender, texture) =>
        {
            Texture = texture;
            GD.Print("Bild von: " + sender);
        };
    }

}
