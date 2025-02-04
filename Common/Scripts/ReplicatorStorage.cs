using Godot;
using System.Collections.Generic;

public struct ReplicatorContent
{
	public ArtifactResource Artifact;
	public double ReplicationStart;
	public float Progress;

	public ReplicatorContent(
		ArtifactResource artifact,
		double replicationStart,
		float progress
	)
	{
		Artifact = artifact;
		ReplicationStart = replicationStart;
		Progress = progress;
	}
}

public partial class ReplicatorStorage : Resource
{
	public Dictionary<Replicator, ReplicatorContent> Replicators { get; set; } = new();
}
