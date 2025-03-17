using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ReplicationProcess contains information about a replication process
/// </summary>
public class ReplicationProcess
{
	public ArtifactResource Artifact;
	public double ReplicationStart = 0;
	public double ReplicationPause = 0;
	public double Progress = 0;
	public Dictionary<
		ArtifactGrowCondition,
		SliderProperties
	> Settings { get; set; }
	public bool IsComplete = false;
	public bool InProgress = false;

	//private double _remainingTime = 0;

	/// <summary>
	/// Returns the time when a replication process was started
	/// </summary>
	public double StartTime()
	{
		var DateTime = GD.Load<DateTime>(Resources.DateTime);
		double startTime = DateTime.TimeStamp();

		if (ReplicationStart > 0) {
			startTime = ReplicationStart;
		}

		//if (_content.ReplicationPause > 0) {
		//	startTime = _content.ReplicationPause;
		//}

		return startTime;
	}

	/// <summary>
	/// Returns the replication process start time as a string
	/// </summary>
	public string StartTimeString()
	{
		var DateTime = GD.Load<DateTime>(Resources.DateTime);
		return DateTime.TimeStampToDateTimeString(ReplicationStart);
	}

	/// <summary>
	/// Returns the time when a replication process should end
	/// </summary>
	public double EndTime()
	{
		var DateTime = GD.Load<DateTime>(Resources.DateTime);
		return DateTime.TimeStamp(ApproxEndDateTime());
	}

	/// <summary>
	/// Returns the time when a replication process should end as a string
	/// </summary>
	public string EndTimeString()
	{
		return EndTime().ToString();
	}

	/// <summary>
	/// Calculate the approximate time until the replication will be finished
	/// based on the artifact grow conditions<br />
	/// Penalties will be given dependent on the deviation penalties
	/// set in the artifact
	/// </summary>
	public System.DateTime ApproxEndDateTime()
	{
		var DateTime = GD.Load<DateTime>(Resources.DateTime);
		double deviationPenalty = 0;

		foreach (var (condition, value) in Artifact.OptimalGrowConditions) {
			if (!Settings.ContainsKey(condition)) {
				continue;
			}

			var artifactDeviationPenalty = Artifact.DeviationPenalty(condition);
			double individualPenalty = artifactDeviationPenalty.Penalty;
			double deviationTolerance = artifactDeviationPenalty.Tolerance;
			float optimalCondition = Artifact.OptimalGrowConditions[condition];

			SliderProperties properties = Settings[condition];
			double diffFromOptimal = Mathf.Abs(properties.Value - optimalCondition);

			individualPenalty *= diffFromOptimal / deviationTolerance;
			deviationPenalty += Mathf.FloorToInt(individualPenalty);
		}

		return DateTime
			.TimeStampToDateTime(StartTime())
			.AddHours(deviationPenalty + Artifact.FastestReplicationTime);
	}

	/// <summary>
	/// Returns the duration until the replication is finished
	/// </summary>
	public int RemainingTime()
	{
		var DateTime = GD.Load<DateTime>(Resources.DateTime);
		//if (Artifact is null || !_replicatorStorage.Has(this)) {
		if (Artifact is null) {
			return 0;
		}

		System.TimeSpan remainingTimeSpan = ApproxEndDateTime()
			.Subtract(DateTime.Now());

		int remainingTime = (int)Math.Round(remainingTimeSpan.TotalHours);
		if (!IsComplete && remainingTime == 0) {
			remainingTime = 1;
		}
		else if (IsComplete && remainingTime <= 0) {
			remainingTime = 0;
		}

		return remainingTime;
	}
}
