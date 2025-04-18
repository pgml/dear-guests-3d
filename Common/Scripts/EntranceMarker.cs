using Godot;
//using static Godot.GD;
using System.Collections.Generic;

public partial class EntranceMarker : Marker3D
{
	[Export(PropertyHint.File, "*.tscn")]
	public string FromScene;

	[Export(PropertyHint.Enum, "Up,Right,Down,Left")]
	public string FacingDirection = "Up";

	public Dictionary<string, Vector2> Directions = new() {
		{ "Up", new Vector2(0, -1) },
		{ "Right", new Vector2(1, 0) },
		{ "Down", new Vector2(0, 1) },
		{ "Left", new Vector2(-1, 0) },
	};

	public Vector2 GetFacingDirection()
	{
		return Directions[FacingDirection];
	}
}

