using Godot;

public partial class PhysicsObject : RigidBody3D
{
	[Export(PropertyHint.File, "*.tres")]
	public string ItemResourcePath { get; set; }

	[Export]
	public bool CanBeMimicked { get; set; } = false;

	[Export]
	public bool Pickupable { get; set; } = false;

	[Export]
	public bool LockRotationAxisXY { get; set; } = false;

	[Export]
	public bool IsThrowable { get; set; } = false;

	[Export]
	public float MinThrowForce { get; set; } = 2;

	[Export]
	public float MaxThrowForce { get; set; } = 20;

	[Export]
	public float ThrowIncreaseStep { get; set; } = 0.05f;

	[Export]
	public float AdditionalImpulse { get; set; } = 1;

	[Export]
	public bool ContinuousImpulse { get; set; } = false;

	/// <summary>
	/// Changes the gravity scale while object is not on floor
	/// 0 means no override
	/// </summary>
	[Export]
	public float GravityScaleWhileAirborne { get; set; } = 5;

	private float _defaultGravityScale;
	private Vector3 _initialRotation;

	public override void _Ready()
	{
		if (LockRotationAxisXY) {
			_initialRotation = Rotation;
			SetAxisLock(PhysicsServer3D.BodyAxis.AngularX, true);
			SetAxisLock(PhysicsServer3D.BodyAxis.AngularY, true);
			SetAxisLock(PhysicsServer3D.BodyAxis.AngularZ, false);
		}

		_defaultGravityScale = GravityScale;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (LinearVelocity.Y <= -0.1 && GravityScaleWhileAirborne > 0) {
			GravityScale = GravityScaleWhileAirborne;
		}
		else {
			GravityScale = _defaultGravityScale;
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		if (LockRotationAxisXY) {
			state = PhysicsObject.LockRotationAxis(state, _initialRotation);
		}
	}

	public void Move(CreatureData cd)
	{
		Vector3 impulse = cd.Direction * Mass * AdditionalImpulse;
		if (ContinuousImpulse) {
			ApplyCentralImpulse(impulse);
		}
		else if (LinearVelocity.Length() <= 1) {
			if (Input.IsActionJustReleased(DGInputMap.ActionWalkLeft) ||
				Input.IsActionJustReleased(DGInputMap.ActionWalkRight) ||
				Input.IsActionJustReleased(DGInputMap.ActionWalkUp) ||
				Input.IsActionJustReleased(DGInputMap.ActionWalkDown))
			{
				ApplyCentralImpulse(impulse);
			}
		}
	}

	public static PhysicsDirectBodyState3D LockRotationAxis(
		PhysicsDirectBodyState3D state,
		Vector3 initialRotation
	) {
		// Preserve Z rotation, zero X and Y
		Basis currentBasis = state.Transform.Basis;
		Vector3 euler = currentBasis.GetEuler();
		euler.X = initialRotation.X;
		euler.Y = initialRotation.Y;
		state.Transform = new Transform3D(Basis.FromEuler(euler), state.Transform.Origin);

		// Clamp angular velocity too
		var angular = state.AngularVelocity;
		state.AngularVelocity = new Vector3(0, 0, angular.Z);

		return state;
	}
}
