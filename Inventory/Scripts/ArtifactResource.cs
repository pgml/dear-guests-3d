using Godot;
using GDC = Godot.Collections;

public enum ArtifactGrowCondition
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

public struct DeviationPenalty
{
	// in hours
	public double Penalty { get; set; }
	// The tolerance of the penalty
	// penalty will only be applied every step of the tolerance setting from
	// the optimal grow condition
	// e.g.
	//	 brigthness penalty = 1 (hours)
	//	 brightness tolerance = 50 (lumen)
	//	 optimal condition = 200 (lumen)
	//	 the penalty will only be applied if every 50 lumen apart from 200
	//	 1 hours will be added at 250 and 150 lumen
	//	 2 hours will be added at 300 and 100 lumen and so on
	public double Tolerance { get; set; }

	public DeviationPenalty(double penalty, double tolerance)
	{
		Penalty = penalty;
		Tolerance = tolerance;
	}
}

public partial class ArtifactResource : ItemResource
{
	[ExportCategory("Artifact Properties")]
	[Export]
	public GDC.Dictionary<
		ArtifactGrowCondition,
		float
	> OptimalGrowConditions { get; set; } = new();

	[Export]
	public GDC.Dictionary<
		ArtifactGrowCondition,
		// insert value as {penalty:tolerance}
		// see DevianPenaly struct for explanation
		string
	> DeviationPenalties { get; set; }

	[Export]
	public Color ReplicatorGlowColour  { get; set; }

	[Export]
	public float FastestReplicationTime  { get; set; }

	[Export]
	public float GrowPowerConsumption { get; set; } = 0;

	public DeviationPenalty DeviationPenalty(ArtifactGrowCondition condition)
	{
		var (penalty, tolerance) = DeviationPenalties[condition].Split(":") switch {
			var a => (a[0], a[1])
		};
		return new DeviationPenalty(
			(double)penalty.ToFloat(),
			(double)tolerance.ToFloat()
		);
	}
}
