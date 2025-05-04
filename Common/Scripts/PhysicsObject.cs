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

	private Vector3 _initialRotation;

	public override void _Ready()
	{
		if (LockRotationAxisXY) {
			_initialRotation = Rotation;
			SetAxisLock(PhysicsServer3D.BodyAxis.AngularX, true);
			SetAxisLock(PhysicsServer3D.BodyAxis.AngularY, true);
			SetAxisLock(PhysicsServer3D.BodyAxis.AngularZ, false);
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		if (LockRotationAxisXY) {
			state = PhysicsObject.LockRotationAxis(state, _initialRotation);
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
