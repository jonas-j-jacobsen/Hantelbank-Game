using Godot;
using System;

public partial class Globe : Node3D
{
	[Export]
	Node3D _globe;
    [Export]
    Node3D _cloud;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_globe.Rotate(new Vector3(0,1,0), (float)delta * .05f);
        _cloud.Rotate(new Vector3(0, 1, 0), (float)delta * .06f);
    }
}
