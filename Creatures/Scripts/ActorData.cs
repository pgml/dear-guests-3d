using Godot;
//using System.Collections.Generic;
using static IController;

public partial class ActorData : Resource
{
	public Node3D Parent = null;
	public Actor Node = null;
	public Controller Controller = null;
	//public RectangleShape2D CollisionShape = null;

	// movement
	public bool CanMove = true;
	public bool CanMoveAndTurn = true;
	public float WalkSpeed = 0.0f;
	public float RunSpeed = 0.0f;
	public float DefaultWalkSpeed = 0.0f;
	public float DefaultRunSpeed = 0.0f;
	public float VelocityMultiplier = 0.0f;
	public Vector3 Position = Vector3.Zero;
	public Vector3 Direction = Vector3.Zero;
	public Vector3 Velocity = Vector3.Zero;
	public Vector3 FacingDirection = Vector3.Zero;

	// states
	public MoveState CurrentState = MoveState.IDLE;
	public bool IsOnFloor = true;
	public bool IsGrounded = true;
	public bool WasGrounded = true;
	public bool IsRunning = false;
	public bool IsIdle = false;
	public bool IsOnStairs = false;
	public bool IsOnSlope = false;

	// Component Helper
	public EdgeCheckComponent edgeCheck = new();
}
