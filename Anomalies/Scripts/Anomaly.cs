using Godot;

public partial class Anomaly : Node3D
{
	protected World World { get; set; }
	protected CreatureData ActorData;

	public async override void _Ready()
	{
		World = GetTree().CurrentScene.FindChild("World") as World;

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		ActorData = GD.Load<CreatureData>(Resources.ActorData);
	}
}
