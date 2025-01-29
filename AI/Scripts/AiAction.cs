using Godot;

public partial class AiAction : Node
{
	[Export]
	public string ActionName { get; protected set; }

	protected AiData AiData { get; private set; }
	protected CreatureData CreatureData { get; private set; }
	protected Agent Agent;

	protected Vector2 TargetPosition = Vector2.Inf;

	public async override void _Ready()
	{
		AiData = GD.Load<AiData>(Resources.AiData);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		CreatureData = AiData.CreatureData[GetParent().GetParent().Name];
		Agent = (CreatureData.Controller as AIController).NavigationAgent;
	}

	public virtual bool Execute(ref Goal goal)
	{
		return true;
	}
}
