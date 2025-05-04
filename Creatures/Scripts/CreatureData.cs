using Godot;
using static IController;
using System.Collections.Generic;

public partial class CreatureData : Resource
{
	public Node3D Parent { get; set; }
	public dynamic Node = null;
	public Controller Controller = null;
	//public RectangleShape2D CollisionShape = null;

	// movement
	/// <summary>
	/// Indicates whether a character can move and slide.<br />
	/// If set to false the character can still move on the y-axis<br />
	/// Turning will still be possible
	/// </summary>
	public bool CanMoveAndSlide = true;

	/// <summary>
	/// Indicates whether a character can move in any direction.<br />
	/// If set to false the character can not move at all
	/// </summary>
	public bool CanMove = true;
	public bool CanJump = false;
	public bool CanClimb = false;
	public bool CanPickUp = false;

	public bool StartClimb = false;
	public bool StartJump = false;

	public bool ShouldJump = false;
	public bool ShouldJumpForward = true;
	public bool ShouldClimb = false;

	public float WalkSpeed = 0;
	public float RunSpeed = 0;
	public float DefaultWalkSpeed = 0;
	public float DefaultRunSpeed = 0;
	public float VelocityMultiplier = 0;
	public float JumpImpulse = 0;

	public Vector3 Position = Vector3.Zero;
	public Vector3 Direction = Vector3.Zero;
	public Vector3 ForwardDirection = Vector3.Zero;
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
	public bool IsPickingUpItem = false;

	// ui states
	public bool IsAnyPanelOpen = false;
	public bool IsConsoleOpen = false;
	public bool IsInventoryOpen = false;
	public bool IsReplicatorOpen = false;
	public bool IsQuickInventoryOpen = false;
	public bool IsBuildMoveActive = false;

	// Component Helper
	public AnimationComponent AnimationComponent = new();
	public EdgeCheckComponent EdgeCheck = new();
	public JumpComponent JumpComponent = new();
	public ClimbComponent ClimbComponent = new();
	public ObjectDetectionComponent ObjectDetectionComponent = new();

	// Mimic stuff
	public bool IsMimic = false;
	public PhysicsObject MimicObject = null;
	public bool CanMimic = false;

	// misc helpers
	public List<Equipment> EquipmentInVicinity = new();
	public Equipment FocusedEquipment = null;
	public float CameraOffset = 0;

	public T Character<T>() where T : class
	{
		return Node is T value
			? value
			: default(T);
	}

	public Sprite3D CharacterSprite()
	{
		if (Node is null) {
			return null;
		}

		Sprite3D sprite = (Node as Actor).CharacterSprite;
		if (Node is AI) {
			sprite = (Node as AI).CharacterSprite;
		}

		return sprite;
	}

	public float CharacterHeight()
	{
		Sprite3D sprite = CharacterSprite();
		float height = 0;

		if (sprite.Vframes > 0) {
			height = sprite.Texture.GetHeight() / sprite.Vframes;
		}
		else if (sprite.RegionEnabled) {
			height = sprite.RegionRect.Size.Y;
		}
		else {
			height = sprite.Texture.GetHeight();
		}

		return height;
	}

	public bool IsAnyUiPanelOpen()
	{
		if (IsInventoryOpen ||
			IsConsoleOpen ||
			IsQuickInventoryOpen ||
			IsReplicatorOpen ||
			IsBuildMoveActive)
		{
			return true;
		}

		return false;
	}
}
