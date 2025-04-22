using Godot;
//using static Godot.GD;
using System.Collections.Generic;

public partial class EntranceMarker : Marker3D
{
	[Export(PropertyHint.File, "*.tscn")]
	public string FromScene;

	[Export(PropertyHint.Enum, "Up,Right,Down,Left")]
	public string FacingDirection = "Up";

	/// <summary>
	/// Hides the marker in game
	/// </summary>
	[Export]
	public bool ShowInGame = false;

	public Dictionary<string, Vector2> Directions = new() {
		{ "Up", Vector2.Up },
		{ "Right", Vector2.Right },
		{ "Down", Vector2.Down },
		{ "Left", Vector2.Left },
	};

	public override void _Ready()
	{
		if (!ShowInGame) {
			QueueFree();
		}
	}

	public Vector2 GetFacingDirection()
	{
		return Directions[FacingDirection];
	}
}
