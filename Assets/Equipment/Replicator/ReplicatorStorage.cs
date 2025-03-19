// ReplicatorStorage is a Godot resource file that holds data
// of all the replicators currently present in the game

using Godot;
using System.Collections.Generic;

public partial class ReplicatorStorage : Resource
{
	public Dictionary<
		Replicator,
		ReplicationProcess
	> Replicators { get; set; } = new();

	public void Add(Replicator replicator, ReplicationProcess content)
	{
		if (!Replicators.ContainsKey(replicator)) {
			Replicators.Add(replicator, content);
		}
	}

	public void Update(Replicator replicator, ReplicationProcess content)
	{
		Replicators[replicator] = content;
	}

	public void Clear(Replicator replicator)
	{
		Replicators[replicator] = new();
	}

	/// <summary>
	/// Just a shorter way of ReplicatorStorage.Replicators.ContainsKey(replicator);
	/// </summary>
	public bool Has(Replicator replicator)
	{
		return Replicators.ContainsKey(replicator);
	}
}
