using Godot;
//using static Godot.GD;

public partial class AnimationComponent : Component
{
	[Export]
	public AnimationTree AnimationTree;

	private AnimationNodeStateMachinePlayback _stateMachine;
	private Vector3 _moveDirection;
	private Vector2 _startingDirection = Vector2.Down;

	public async override void _Ready()
	{
		base._Ready();

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_stateMachine = AnimationTree.Get("parameters/playback").Obj as AnimationNodeStateMachinePlayback;
		_startingDirection = CreatureData.Controller.StartingDirection;
		SetInitialFacingDirection();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(CreatureData)) {
			return;
		}

		_moveDirection = CreatureData.Direction;

		_updateAnimationState();
		_updateAnimationParameters(_moveDirection);
	}

	public void SetInitialFacingDirection()
	{
		AnimationTree.Set("parameters/Idle/blend_position", _startingDirection);
		AnimationTree.Set("parameters/BoxedIdle/blend_position", _startingDirection);
	}

	private void _updateAnimationParameters(Vector3 moveInput)
	{
		if (moveInput != Vector3.Zero) {
			Vector2 dir = new Vector2(moveInput.X, moveInput.Z);
			AnimationTree.Set("parameters/Idle/blend_position", dir);
			AnimationTree.Set("parameters/Walk/blend_position", dir);
			AnimationTree.Set("parameters/BoxedIdle/blend_position", dir);
			AnimationTree.Set("parameters/BoxedWalk/blend_position", dir);
		}
	}

	private void _updateAnimationState()
	{
		var shouldIdle = CreatureData.VelocityMultiplier == 0;

		if (!shouldIdle && _moveDirection != Vector3.Zero) {
			if (CreatureData.IsOnFloor) {
				//if (Controller.IsCharacterBoxed()) {
				//	_stateMachine.Travel("BoxedWalk");
				//}
				//else {
				_stateMachine.Travel("Walk");
				//}
			}
		}
		else {
			//if (Controller.IsCharacterBoxed()) {
			//	_stateMachine.Travel("BoxedIdle");
			//}
			//else {
				_stateMachine.Travel("Idle");
			//}
		}
	}
}
