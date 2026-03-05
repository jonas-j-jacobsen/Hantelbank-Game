using Godot;
using System;

public partial class BaseClickParticle : Node3D
{
	double lifetime = 1.5;
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	[Export]
	Label3D Label;

	public override void _Process(double delta)
	{
		Position += new Vector3(0, 1f * (float)delta, -.5f * (float)delta);
		lifetime -= delta;


		if (lifetime < 0) QueueFree();
    }


	public void setUpNumber(int num)
	{
		if (num != 0)
		{
			Label.Text = num.ToString();
		}
		else
		{
			Label.Text = "STARK!!!";

        }
	}

	public void setUpText(string text)
	{
        Label.Text = text;
		Label.Modulate = new Color(0f,.99f,0.26f, 1);
		Label.FontSize = 18;	
    }
}
