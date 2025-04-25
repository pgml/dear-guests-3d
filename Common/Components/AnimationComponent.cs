using Godot;

public partial class AnimationComponent : Component
{
	[Export]
	public AnimationTree AnimationTree;

	private AnimationNodeStateMachinePlayback _stateMachine;
	private Vector3 _moveDirection;
	private Vector3 _forwardDirection;
	private Vector2 _startingDirection = Vector2.Down;

	public async override void _Ready()
	{
		base._Ready();

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_stateMachine = AnimationTree.Get("parameters/playback").Obj as AnimationNodeStateMachinePlayback;
		_startingDirection = CreatureData.Controller.StartingDirection;
		SetInitialFacingDirection(_startingDirection);
		CreatureData.AnimationComponent = this;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(CreatureData)) {
			return;
		}

		_moveDirection = CreatureData.ForwardDirection;
		_forwardDirection = CreatureData.ForwardDirection;

		_updateAnimationState();
		_updateAnimationParameters(_forwardDirection);
	}

	public void SetInitialFacingDirection(Vector2 direction)
	{
		AnimationTree.Set("parameters/Idle/blend_position", direction);
		AnimationTree.Set("parameters/BoxedIdle/blend_position", direction);
	}

	private void _updateAnimationParameters(Vector3 moveInput)
	{
		if (moveInput != Vector3.Zero) {
			Vector2 dir = new Vector2(moveInput.X, moveInput.Z);
			AnimationTree.Set("parameters/Idle/blend_position", dir);
			AnimationTree.Set("parameters/Walk/blend_position", dir);
			AnimationTree.Set("parameters/JumpBegin/blend_position", dir);
			AnimationTree.Set("parameters/Jump/blend_position", dir);
			AnimationTree.Set("parameters/Fall/blend_position", dir);
			AnimationTree.Set("parameters/BoxedIdle/blend_position", dir);
			AnimationTree.Set("parameters/BoxedWalk/blend_position", dir);
		}
	}

	private void _updateAnimationState()
	{
		var shouldIdle = CreatureData.VelocityMultiplier == 0;

		if (CreatureData.StartJump && CreatureData.CanJump) {
			// @todo - check why this doesn't work
			_stateMachine.Travel("JumpBegin");
		}
		if (CreatureData.ShouldJump && CreatureData.CanJump) {
			_stateMachine.Travel("Jump");
		}
		else if (CreatureData.IsJumping && !CreatureData.IsOnFloor && CreatureData.Velocity.Y < 0) {
			_stateMachine.Travel("Fall");
		}
		// @todo replace with climbing animation
		else if (CreatureData.ShouldClimb && CreatureData.CanClimb) {
			_stateMachine.Travel("Jump");
		}
		// @todo replace with climbing animation
		else if (CreatureData.IsClimbing && !CreatureData.IsOnFloor) {
			_stateMachine.Travel("Fall");
		}
		else if (CreatureData.IsOnFloor) {
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
}
