using Godot;

public partial class UiControl : Control
{
	public bool IsOpen { get; set; } = false;
	private CreatureData _actorData;

	public async override void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		_actorData = GD.Load<CreatureData>(Resources.ActorData);
	}

	public override void _Process(double delta)
	{
		if (IsOpen) {
			_actorData.Direction = Vector3.Zero;
		}
	}
}
