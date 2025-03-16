// ReplicatorStorage is a Godot resource file that holds data
// of all the replicators currently present in the game

using Godot;
using System.Collections.Generic;

public class ReplicatorContent
{
	public ArtifactResource Artifact;
	public double ReplicationStart = 0;
	public double ReplicationPause = 0;
	public double Progress = 0;
	public Dictionary<
		ArtifactGrowCondition,
		SliderProperties
	> Settings { get; set; }

	public ReplicatorContent(
		ArtifactResource artifact,
		double replicationStart,
		double replicationPause,
		double progress,
		Dictionary<ArtifactGrowCondition, SliderProperties> settings)
	{
		Artifact = artifact;
		ReplicationStart = replicationStart;
		ReplicationPause = replicationPause;
		Progress = progress;
		Settings = settings;
	}
}

public partial class ReplicatorStorage : Resource
{
	public Dictionary<
		Replicator,
		ReplicatorContent
	> Replicators { get; set; } = new();

	public void Add(Replicator replicator, ReplicatorContent content)
	{
		if (!Replicators.ContainsKey(replicator)) {
			Replicators.Add(replicator, content);
		}
	}

	public void Update(Replicator replicator, ReplicatorContent content)
	{
		Replicators[replicator] = content;
	}

	public void Clear(Replicator replicator)
	{
		Replicators[replicator] = new ReplicatorContent(new(), 0, 0, 0, new());
	}

	/// <summary>
	/// Just a shorter way of ReplicatorStorage.Replicators.ContainsKey(replicator);
	/// </summary>
	public bool Has(Replicator replicator)
	{
		return Replicators.ContainsKey(replicator);
	}
}
