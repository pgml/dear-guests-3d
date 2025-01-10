using Godot;

public partial class LightSource : StaticObject
{
	[Export]
	public bool TimeBaseActivation { get; set; }

	[Export]
	public float ActivationTime { get; set; }

	[Export]
	public Light3D[] LightSources { get; set; }

	public override void _Process(double delta)
	{
		base._Process(delta);

		//GD.Print(World.DayTime());

		//if (LightSources.Length > 0) {
		//	GD.Print(LightSources[0]);
		//	LightSources[0].GetParent<Node3D>().RotationDegrees = new Vector3(
		//		0,
		//		-Sun.RotationDegrees.X,
		//		0
		//	);
		//}
	}
}
