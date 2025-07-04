using Godot;

public partial class Component : Node3D
{
	// resources
	protected Inventory ActorInventory;

	protected static AudioLibrary AudioLibrary { get {
		return GD.Load<AudioLibrary>(Resources.AudioLibrary);
	}}

	protected AudioInstance AudioInstance { get {
		return AudioLibrary.CreateAudioInstance("Misc", this);
	}}

	protected Controller Controller;
	// alias of ActorData, too lazy to rename it erverywhere else
	protected CreatureData CreatureData;
	protected WorldData WorldData;

	public async override void _Ready()
	{
		//AudioLibrary = Load<AudioLibrary>(Resources.AudioLibrary);
		ActorInventory = GD.Load<Inventory>(Resources.ActorInventory);
		WorldData = GD.Load<WorldData>(Resources.WorldData);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		foreach (var child in GetParent().GetParent().GetChildren()) {
			if (child is CharacterBody3D body) {
				Controller = body as Controller;
				break;
			}
		}

		if (Controller is null) {
			return;
		}

		CreatureData = Controller.CreatureData;
	}
}
