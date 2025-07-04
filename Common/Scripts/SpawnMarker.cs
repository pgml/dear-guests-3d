using Godot;

public partial class SpawnMarker : Marker3D
{
	[Export(PropertyHint.File, "*.tscn")]
	public string Creature { get; set; }

	/// <summary>
	/// Hides the marker in game
	/// </summary>
	[Export]
	public bool ShowInGame = false;

	protected CreatureData ActorData;

	private World _world;

	public async override void _Ready()
	{
		_world = GetTree().CurrentScene.FindChild("World") as World;

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		ActorData = GD.Load<CreatureData>(Resources.ActorData);
		Spawn();

		if (!ShowInGame) {
			QueueFree();
		}
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

		GD.PrintRich("[i] -- Spawning ", Creature.GetBaseName(), " at: ", GlobalPosition, "[/i]");

		//var scene = await AsyncLoader.LoadResource<PackedScene>(Creature, "", true);
		//var creature = scene.Instantiate<Node3D>();
		var creature = ResourceLoader.Load<PackedScene>(Creature).Instantiate<Node3D>();
		creature.Position = GlobalPosition;
		//GetTree().CurrentScene.AddChild(creature);
		_world.Viewport.AddChild(creature);
	}
}
