using Godot;
using System.Collections.Generic;

public partial class AI : Creature
{
	[Export]
	public string CharacterName { get; set; }

	[Export]
	public Node ActionsParent { get; set; }

	public AiData AiData { get; private set; }
	public Dictionary<string, AiAction> Actions { get;  private set; } = new();

	private AIController _controller;

	public async override void _Ready()
	{
		base._Ready();

		CreatureData = new() {
			Node = this
		};

		AiData = GD.Load<AiData>(Resources.AiData);
		AiData.CreatureData.Add(GetParent().GetParent().Name, CreatureData);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_controller = CreatureData.Controller as AIController;
		_controller.NavigationAgent.Initialise();

		Actions = _actions();
	}

	public override void _PhysicsProcess(double delta) {}

	private Dictionary<string, AiAction> _actions()
	{
		Dictionary<string, AiAction> actions = new();
		foreach (AiAction action in ActionsParent.GetChildren()) {
			_controller.NavigationAgent.AddAction(action);
			actions.Add(action.ActionName, action);
		}
		return actions;
	}
}
