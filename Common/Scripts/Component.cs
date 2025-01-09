using Godot;
using static Godot.GD;

public partial class Component : Node
{
	// resources
	protected ActorData ActorData;
	//protected AudioLibrary AudioLibrary;
	//protected SceneManager SceneManager;
	//protected QuickBar Quickbar;

	protected dynamic Controller;

	public override void _Ready()
	{
		//SceneManager = GetNode<SceneManager>(Resources.SceneManager);

		ActorData = Load<ActorData>(Resources.ActorData);
		//AudioLibrary = Load<AudioLibrary>(Resources.AudioLibrary);
		//Quickbar = Load<QuickBar>(Resources.QuickBar);

		Controller = _getController();
	}

	private dynamic _getController()
	{
		var controller = GetParent().GetParent().GetNode<CharacterBody3D>("Controller");

		if (controller is Controller) {
			controller = controller as Controller;
		}
		//else if (controller is AIController) {
		//	controller = controller as AIController;
		//}

		return controller;
	}
}

