using Godot;
using System.Collections.Generic;

public partial class Locations : Node3D
{
	public static Dictionary<string, LocationArea> LocationAreas {
		get; private set;
	} = new();

	public static Dictionary<string, LocationMarker> LocationMarkers {
		get; private set;
	} = new();

	public override void _Ready()
	{
		foreach (var child in GetChildren()) {
			if (child is Area3D) {
				var location = child as LocationArea;
				LocationAreas.Add(location.LocationName.ToLower(), location);
			}

			if (child is Marker3D) {
				var location = child as LocationMarker;
				LocationMarkers.Add(location.LocationName.ToLower(), location);
			}
		}
	}
}

