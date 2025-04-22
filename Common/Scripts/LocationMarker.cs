using Godot;

public partial class LocationMarker : Marker3D, Location
{
	[Export]
	public string LocationName { get; set; }

	/// <summary>
	/// Hides the marker in game
	/// </summary>
	[Export]
	public bool ShowInGame = false;

	public override void _Ready()
	{
		if (!ShowInGame) {
			QueueFree();
		}
	}
}
