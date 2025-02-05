using Godot;

public partial class UiControl : Control
{
	public bool IsOpen { get; set; } = false;

	public async override void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	public override void _Process(double delta)
	{
		if (IsOpen) {
			ActorData().Direction = Vector3.Zero;
		}
	}

	protected CreatureData ActorData()
	{
		return GD.Load<CreatureData>(Resources.ActorData);
	}
}
