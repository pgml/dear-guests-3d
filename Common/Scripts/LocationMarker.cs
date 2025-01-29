using Godot;

public partial class LocationMarker : Marker3D, Location
{
	[Export]
	public string LocationName { get; set; }
}

