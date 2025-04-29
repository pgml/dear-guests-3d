using Godot;

public partial class Anomaly : Node3D
{
	protected World World { get; set; }

	public async override void _Ready()
	{
		World = GetTree().CurrentScene.FindChild("World") as World;
	}
}
