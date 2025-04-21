using Godot;

public partial class SpawnMarker : Marker3D
{
	[Export(PropertyHint.File, "*.tscn")]
	public string Creature { get; set; }

	protected CreatureData ActorData;

	public async override void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		ActorData = GD.Load<CreatureData>(Resources.ActorData);
		Spawn();
		QueueFree();
	}

	/// <summary>
	/// Spawns `Creature` at current position
	/// </summary>
	public void Spawn()
	{
		// @todo check this in a non shitty way
		if (ActorData.Parent is not null && Creature == Resources.Actor) {
			return;
		}

		GD.PrintS(" -- Spawning", Creature.GetBaseName(), "at:", GlobalPosition);

		//var scene = await AsyncLoader.LoadResource<PackedScene>(Creature, "", true);
		//var creature = scene.Instantiate<Node3D>();
		var creature = ResourceLoader.Load<PackedScene>(Creature).Instantiate<Node3D>();
		creature.Position = GlobalPosition;
		GetTree().CurrentScene.AddChild(creature);
	}
}
