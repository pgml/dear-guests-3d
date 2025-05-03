using Godot;
using System;

public partial class FpsCounter : Label
{
	public override void _PhysicsProcess(double delta)
	{
		Text = $"{Engine.GetFramesPerSecond().ToString()} fps";
	}
}
