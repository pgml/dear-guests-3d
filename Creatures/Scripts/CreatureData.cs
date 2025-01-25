using Godot;
//using System.Collections.Generic;
using static IController;

public partial class CreatureData : Resource
{
	public Node3D Parent = null;
	public Actor Node = null;
	public Controller Controller = null;
	//public RectangleShape2D CollisionShape = null;

	// movement
	public bool CanMoveAndSlide = true;
	public bool CanJump = false;
	public bool StartJump = false;
	public bool ShouldJump = false;
	public bool ShouldJumpForward = true;
	public bool CanClimb = false;
	public bool StartClimb = false;
	public bool ShouldClimb = false;
	public float WalkSpeed = 0;
	public float RunSpeed = 0;
	public float DefaultWalkSpeed = 0;
	public float DefaultRunSpeed = 0;
	public float VelocityMultiplier = 0;
	public float JumpImpulse = 0;
	public Vector3 Position = Vector3.Zero;
	public Vector3 Direction = Vector3.Zero;
	public Vector3 Velocity = Vector3.Zero;
	public Vector3 FacingDirection = Vector3.Zero;

	// states
	public MoveState CurrentState = MoveState.IDLE;
	public bool IsOnFloor = true;
	public bool IsJumping = false;
	public bool IsClimbing = false;
	public bool IsRunning = false;
	public bool IsIdle = false;
	public bool IsOnStairs = false;
	public bool IsOnSlope = false;
	public bool IsFacingEdge = false;

	// Component Helper
	public EdgeCheckComponent EdgeCheck = new();
	public JumpComponent JumpComponent = new();
	public ClimbComponent ClimbComponent = new();
}
