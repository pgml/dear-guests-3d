using Godot;
using GDC = Godot.Collections;
using System;
using System.Collections.Generic;


[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public partial class AbbrAttribute : Attribute
{
	public string Abbreviation { get; }

	public AbbrAttribute(string abbr)
	{
		Abbreviation = abbr;
	}
}

public enum ArtifactGrowCondition
{
	// in percent
	[Abbr("HUM")]
	Humidity,
	// in bar
	[Abbr("P")]
	Pressure,
	// in lumen
	[Abbr("BRT")]
	Brightness,
	// in km/s
	[Abbr("WVEL")]
	WindVelocity,
	// in C°
	[Abbr("TEMP")]
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
	public bool IsSynthetic { get; set; } = false;

	[Export]
	public ArtifactResource ReplicaOf { get; set; }

	[Export(PropertyHint.Range, "0, 100")]
	public int Degradation { get; set; } = 0;

	[Export(PropertyHint.Range, "0, 100")]
	public int DegradationFactorPerReplication { get; set; } = 0;

	[ExportGroup("Replication Properties")]
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
		if (!DeviationPenalties.ContainsKey(condition)) {
			return new();
		}

		var (penalty, tolerance) = DeviationPenalties[condition].Split(":") switch {
			var a => (a[0], a[1])
		};

		return new DeviationPenalty(
			(double)penalty.ToFloat(),
			(double)tolerance.ToFloat()
		);
	}

	public List<string> RequiredConditions()
	{
		List<string> requiredConditions = new();

		foreach (var (condition, value) in OptimalGrowConditions) {
			ArtifactGrowCondition cond = condition;
			string abbr = Tools.GetCustomAttribute<
				AbbrAttribute,
				ArtifactGrowCondition
			>(cond)?.Abbreviation;
			requiredConditions.Add(abbr);
		}

		return requiredConditions;
	}
}
