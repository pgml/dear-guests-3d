using Godot;
using System.Collections.Generic;

public struct ReplicatorContent
{
	public ArtifactResource Artifact;
	public double ReplicationStart = 0;
	public float Progress = 0;
	public Dictionary<ArtifactGrowCondition, SliderProperties> Settings { get; set; }

	public ReplicatorContent(
		ArtifactResource artifact,
		double replicationStart,
		float progress,
		Dictionary<ArtifactGrowCondition, SliderProperties> settings
	)
	{
		Artifact = artifact;
		ReplicationStart = replicationStart;
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

	public void Update(Replicator replicator, ReplicatorContent  content)
	{
		Replicators[replicator] = content;
	}

	/// <summary>
	/// Just a shorter way of ReplicatorStorage.Replicators.ContainsKey(replicator);
	/// </summary>
	public bool Has(Replicator replicator)
	{
		return Replicators.ContainsKey(replicator);
	}
}
