using Godot;
using static IController;

public partial class Controller : CreatureController, IController
{
	private CreatureData _actorData;
	//private AIData _aiData;
	private AnimationNodeStateMachinePlayback _stateMachine;

	public async override void _Ready()
	{
		_setActorData();
		base._Ready();

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CreatureData is not null) {
			CreatureData.WasGrounded = CreatureData.IsGrounded;
			CreatureData.IsGrounded = IsOnFloor();

			base._PhysicsProcess(delta);
			Movement(delta);
		}
	}

	public void Movement(double delta)
	{
		CreatureData.CurrentState = CurrentState;
		CreatureData.Position = Position;

		// Set velocity to zero to make animations stop when
		// movement is forced
		if (CreatureData.VelocityMultiplier == 0.0f) {
			Velocity = Vector3.Zero;
		}
		else {
			CurrentState = _stateIdle();
		}

		if (Velocity != Vector3.Zero)	{
			CurrentState = _stateWalk();
		}

		CreatureData.CurrentState = CurrentState;
		CreatureData.IsIdle = CurrentState == _stateIdle();

		if (CreatureData.CanMove) {
			CreatureData.VelocityMultiplier = CreatureData.WalkSpeed;

			if (CreatureData.IsRunning) {
				CreatureData.VelocityMultiplier = CreatureData.RunSpeed;
			}

			SlopeMovement();

			Velocity = CreatureData.Direction * CreatureData.VelocityMultiplier;
			CreatureData.Velocity = Velocity;

			MoveAndSlide();

		}

		if (CreatureData.Direction != Vector3.Zero) {
			CreatureData.FacingDirection = CreatureData.Direction;
		}

		//SetCollider(CharacterData.Direction);
	}

	public void SlopeMovement()
	{
		if (_isOnSlope()) {
			// Counter orthogonal slope movement seem faster on vertical (in top down view) slopes
			if (CreatureData.Direction.Z != 0) {
				CreatureData.VelocityMultiplier /= Mathf.Sqrt2;
			}

			FloorConstantSpeed = true;
		}
		else {
			FloorConstantSpeed = false;
		}
	}

	private bool _isOnSlope()
	{
		var isOnSlope = false;

		if (Mathf.IsZeroApprox(GetFloorAngle())) {
			return isOnSlope;
		}

		isOnSlope = true;

		if (GetSlideCollisionCount() > 0) {
			var collider = GetLastSlideCollision().GetCollider() as Node3D;

			if (collider.IsInGroup("Stairs")) {
				CreatureData.IsOnStairs = CreatureData.Node.IsOnStairs;
			}
		}

		CreatureData.IsOnSlope = isOnSlope;

		return isOnSlope;
	}

	private void _setActorData()
	{
		var charNode = CharacterNode as Actor;
		_actorData = charNode.CharacterData;
		//_actorData.IsBoxed = IsBoxed;

		CreatureData = _actorData;
		CreatureData.Controller = this;
		CreatureData.Parent = GetParent() as Node3D;
		CreatureData.IsRunning = ToggleRun;
		CreatureData.DefaultWalkSpeed = DefaultWalkSpeed;
		CreatureData.DefaultRunSpeed = DefaultRunSpeed;
		CreatureData.WalkSpeed = DefaultWalkSpeed;
		CreatureData.RunSpeed = DefaultRunSpeed;
		CreatureData.VelocityMultiplier = 0.0f;
	}

	private MoveState _stateIdle()
	{
		var currentState = MoveState.IDLE;

		//if (IsCharacterBoxed()) {
		//	currentState = MoveState.BOXED_IDLE;
		//}

		return currentState;
	}

	private MoveState _stateWalk()
	{
		var currentState = MoveState.WALK;

		//if (IsCharacterBoxed()) {
		//	currentState = MoveState.BOXED_WALK;
		//}

		return currentState;
	}
}
