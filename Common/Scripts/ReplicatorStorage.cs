using Godot;
using System.Collections.Generic;

public struct ReplicatorContent
{
	public ArtifactResource Artifact;
	public double ReplicationStart;
	public float Progress;
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

	public Replicator Replicator { get; set; } = new();

	public void Add(Replicator replicator, ReplicatorContent content)
	{
		if (Replicators.ContainsKey(Replicator)) {
			Replicators.Add(Replicator, content);
		}
	}

	public void Update(Replicator replicator, ReplicatorContent  content)
	{
		Replicators[Replicator] = content;
	}

	public ReplicatorContent Content(Replicator replicator)
	{
		return Replicators[replicator];
	}


	//public Dictionary<ArtifactGrowCondition, SliderProperties> Settings()
	//{
	//	if (Replicator is null) {
	//		return new();
	//	}
	//	return Replicators[Replicator].Settings;
	//}

	//public ArtifactResource Artifact()
	//{
	//	if (Replicator is null) {
	//		return new();
	//	}
	//	return Replicators[Replicator].Artifact;
	//}

	//public double ReplicationStart()
	//{
	//	if (Replicator is null) {
	//		return -1;
	//	}
	//	return Replicators[Replicator].ReplicationStart;
	//}

	//public float Progress()
	//{
	//	if (Replicator is null) {
	//		return -1;
	//	}
	//	return Replicators[Replicator].Progress;
	//}

	//public bool Has(Replicator replicator)
	//{
	//	if (Replicators.ContainsKey(replicator)) {
	//		Replicator = replicator;
	//		return true;
	//	}
	//	return false;
	//}
}
