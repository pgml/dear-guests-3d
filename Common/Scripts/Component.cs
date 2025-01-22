using Godot;
using static Godot.GD;

public partial class Component : Node
{
	// resources
	protected CreatureData ActorData;
	protected AudioLibrary AudioLibrary;
	//protected SceneManager SceneManager;
	//protected QuickBar Quickbar;

	protected Controller Controller;
	protected CreatureData CreatureData { get; set; }

	public async override void _Ready()
	{
		//SceneManager = GetNode<SceneManager>(Resources.SceneManager);

		ActorData = Load<CreatureData>(Resources.ActorData);
		AudioLibrary = Load<AudioLibrary>(Resources.AudioLibrary);
		//Quickbar = Load<QuickBar>(Resources.QuickBar);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Controller = GetParent().GetParent().GetNode<CharacterBody3D>("Controller") as Controller;
		CreatureData = Controller.CreatureData;
	}
}
