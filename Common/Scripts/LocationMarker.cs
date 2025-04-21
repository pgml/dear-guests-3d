using Godot;

public partial class LocationMarker : Marker3D, Location
{
	[Export]
	public string LocationName { get; set; }

	/// <summary>
	/// Hides the marker in game
	/// </summary>
	[Export]
	public bool Hide = true;

	public override void _Ready()
	{
		if (Hide) {
			QueueFree();
		}
	}
}
