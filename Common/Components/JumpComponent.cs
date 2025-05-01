using Godot;

public partial class JumpComponent : Component
{
	[Export]
	public float JumpImpulse { get; set; } = 20f;

	[Export]
	public float JumpTestForwardDistance { get; set; } = 3f;

	[Export]
	public bool JumpLimitation { get; set; } = true;

	public Vector3 JumpOrigin { get; set; }
	public Vector3 JumpTo { get; set; }

	private World _world;
	private Camera _camera;
	private float _cameraOffset = 0;
	private float _cameraSmoothingDelta = 0;

	public async override void _Ready()
	{
		base._Ready();

		_world = GetTree().CurrentScene.FindChild("World") as World;
		_camera = _world.Viewport.GetCamera3D() as Camera;
		_cameraSmoothingDelta = _camera.SmoothingDelta;

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		if (CreatureData is null) {
			return;
		}

		CreatureData.JumpComponent = this;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(CreatureData)) {
			return;
		}

		CreatureData.JumpImpulse = JumpImpulse;

		if (Input.IsActionPressed("action_jump") && CreatureData.IsOnFloor) {
			_camera.SmoothingDelta = 8f;
			CreatureData.StartJump = true;
		}
		else if (Input.IsActionJustReleased("action_jump") && CreatureData.IsOnFloor) {
			CreatureData.StartJump = true;
			Vector3 globalPosition = CreatureData.Controller.GlobalPosition;
			Vector3 facingDirection = CreatureData.FacingDirection;

			JumpOrigin = globalPosition;
			JumpTo = globalPosition + facingDirection * JumpTestForwardDistance;

			CreatureData.ShouldJump = true;
		}
		else if (CreatureData.IsOnFloor) {
			_camera.SmoothingDelta = _cameraSmoothingDelta;
			CreatureData.StartJump = false;
			CreatureData.ShouldJump = false;
		}
	}
}
