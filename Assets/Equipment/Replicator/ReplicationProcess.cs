using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ReplicationProcess contains information about a replication process
/// </summary>
public class ReplicationProcess
{
	public ArtifactResource Artifact;

	/// <summary>
	/// The timestamp when the replication process started
	/// </summary>
	public double TimeStart = 0;

	/// <summary>
	/// The remaing time of a replication process
	/// </summary>
	public double TimeRemaining = 0;

	/// <summary>
	/// The progress at the time a replication process was paused
	/// </summary>
	public double ProgressPaused = 0;
	public double TimePaused = 0;


	/// <summary>
	/// The current percentage of progress
	/// </summary>
	public double Progress = 0;

	public bool InProgress = false;
	public bool IsComplete = false;
	public bool IsPaused = false;

	/// <summary>
	/// The replicator's settingss'
	/// </summary>
	public Dictionary<
		ArtifactGrowCondition,
		SliderProperties
	> Settings { get; set; }

	/// <summary>
	/// Returns the time when a replication process was started
	/// </summary>
	public double StartTime()
	{
		var DateTime = GD.Load<DateTime>(Resources.DateTime);
		double startTime = DateTime.TimeStamp();

		if (TimeStart > 0) {
			startTime = TimeStart;
		}

		if (TimePaused > 0) {
			startTime += TimePaused - DateTime.TimeStamp();
		}

		return startTime;
	}

	/// <summary>
	/// Returns the replication process start time as a string
	/// </summary>
	public string StartTimeString()
	{
		var DateTime = GD.Load<DateTime>(Resources.DateTime);
		return DateTime.TimeStampToDateTimeString(TimeStart);
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
