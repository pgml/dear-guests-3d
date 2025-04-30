using Godot;
using static Godot.GD;

public partial class Component : Node3D
{
	// resources
	protected CreatureData ActorData;
	protected Inventory ActorInventory;

	protected static AudioLibrary AudioLibrary { get {
		return GD.Load<AudioLibrary>(Resources.AudioLibrary);
	}}

	protected AudioInstance AudioInstance { get {
		return AudioLibrary.CreateAudioInstance("Replicator", this);
	}}
	//protected SceneManager SceneManager;
	//protected QuickBar Quickbar;

	protected Controller Controller;
	protected CreatureData CreatureData { get; set; }

	protected World World { get; set; }

	public async override void _Ready()
	{
		//SceneManager = GetNode<SceneManager>(Resources.SceneManager);

		ActorData = Load<CreatureData>(Resources.ActorData);
		ActorInventory = GD.Load<Inventory>(Resources.ActorInventory);
		//AudioLibrary = Load<AudioLibrary>(Resources.AudioLibrary);
		//Quickbar = Load<QuickBar>(Resources.QuickBar);
		World = GetTree().CurrentScene.FindChild("World") as World;

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
