using Godot;
using static IController;

public partial class Controller : CreatureController, IController
{
	public StairMovementComponent StairMovement { get; private set; } = new();
	private ActorData _actorData;
	//private AIData _aiData;
	private AnimationNodeStateMachinePlayback _stateMachine;

	public async override void _Ready()
	{
		_setActorData();
		base._Ready();

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		StairMovement = CharacterData.StairMovementComponent;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CharacterData is not null) {
			CharacterData.WasGrounded = CharacterData.IsGrounded;
			CharacterData.IsGrounded = IsOnFloor();

			base._PhysicsProcess(delta);
			Movement(delta);
		}
	}

	public void Movement(double delta)
	{
		CharacterData.CurrentState = CurrentState;
		CharacterData.Position = Position;

		// Set velocity to zero to make animations stop when
		// movement is forced
		if (CharacterData.VelocityMultiplier == 0.0f) {
			Velocity = Vector3.Zero;
		}
		else {
			CurrentState = _stateIdle();
		}

		if (Velocity != Vector3.Zero)	{
			CurrentState = _stateWalk();
		}

		CharacterData.CurrentState = CurrentState;
		CharacterData.IsIdle = CurrentState == _stateIdle();

		if (CharacterData.CanMove) {
			CharacterData.VelocityMultiplier = CharacterData.WalkSpeed;

			if (CharacterData.IsRunning) {
				CharacterData.VelocityMultiplier = CharacterData.RunSpeed;
			}

			//CharacterData.IsOnStairs = CharacterData.Node.IsOnStairs;
			//if (CharacterData.IsOnSlope) {
			//	SlopeMovement();
			//}

			StairMovement.StepUp();

			Velocity = CharacterData.Direction * CharacterData.VelocityMultiplier;
			CharacterData.Velocity = Velocity;

			MoveAndSlide();
			StairMovement.StepDown();

			if (CharacterData.Direction != Vector3.Zero) {
				CharacterData.FacingDirection = CharacterData.Direction;
			}
		}

		//SetCollider(CharacterData.Direction);
	}


	private void _setActorData()
	{
		var charNode = CharacterNode as Actor;
		_actorData = charNode.CharacterData;
		//_actorData.IsBoxed = IsBoxed;

		CharacterData = _actorData;
		CharacterData.Controller = this;
		CharacterData.Parent = GetParent() as Node3D;
		CharacterData.IsRunning = ToggleRun;
		CharacterData.DefaultWalkSpeed = DefaultWalkSpeed;
		CharacterData.DefaultRunSpeed = DefaultRunSpeed;
		CharacterData.WalkSpeed = DefaultWalkSpeed;
		CharacterData.RunSpeed = DefaultRunSpeed;
		CharacterData.VelocityMultiplier = 0.0f;
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
