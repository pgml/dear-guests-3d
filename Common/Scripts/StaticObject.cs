using Godot;

public partial class StaticObject : Node3D
{
	[Export]
	public MeshInstance3D SunShadowMesh { get; set; }

	protected World World;
	protected DirectionalLight3D Sun;

	public override void _Ready()
	{
		World = GetTree().Root.GetNode<World>("Scene/World");
		Sun = World.Sun;
	}

	public override void _Process(double delta)
	{
		SunShadowMesh.RotationDegrees = new Vector3(0, Sun.RotationDegrees.X, 0);
	}
}
