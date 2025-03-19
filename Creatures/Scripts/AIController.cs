using Godot;

public partial class AIController : Controller
{
	[Export]
	public Agent NavigationAgent { get; set; }

	public Vector3 MovementTarget {
		get { return NavigationAgent.TargetPosition; }
		set { NavigationAgent.TargetPosition = value; }
	}

	private Vector3 _movementTargetPosition = Vector3.Zero;

	public override void _Ready()
	{
		base._Ready();
	}
}
