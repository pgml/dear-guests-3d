using Godot;

/// <summary>
/// small helper to avoid permanently rewriting the same shit
/// </summary>
public partial class TestMotion
{
	public PhysicsTestMotionResult3D Result { get; set; } = new();
	public bool IsColliding { get; set; } = false;

	public TestMotion(
		Rid rid,
		Transform3D from,
		Vector3 motion,
		float margin = 0.001f
	)
	{
		PhysicsTestMotionParameters3D testParams = new() {
			From = from,
			Motion = motion,
			Margin = margin
		};

		IsColliding = PhysicsServer3D.BodyTestMotion(
			rid,
			testParams,
			Result
		);
	}

	/// <summary>
	/// Same as PhysicsTestMotionResult3D.GetCollider()
	/// but conveniently return correct type
	/// </summary>
	public T Collider<T>() where T : class
	{
		return Result.GetCollider() is T value
			? value
			: default(T);
	}
}
