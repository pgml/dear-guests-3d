using Godot;
using GDC = Godot.Collections;

public enum ArtifactGrowConditions
{
	// in percent
	Humidity,
	// in bar
	Pressure,
	// in lumen
	Brightness,
	// in km/s
	WindVelocity,
	// in C°
	Temperature
}

public partial class ArtifactResource : ItemResource
{
	[ExportCategory("Artifact Properties")]
	[Export]
	public GDC.Dictionary<ArtifactGrowConditions, float> GrowConditions { get; set; }

	[Export]
	public Color ReplicatorGlowColour  { get; set; }

	[Export]
	public float ReplicationTime  { get; set; }

	[Export]
	public float GrowPowerConsumption { get; set; } = 0;
}
