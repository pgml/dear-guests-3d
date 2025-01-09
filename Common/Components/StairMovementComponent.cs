/*
 * Credits:
 * Special thanks to Majikayo Games for original solution to stair_step_down!
 * (https://youtu.be/-WjM1uksPIk)
 *
 * Special thanks to Myria666 for their paper on Quake movement mechanics (used for stair_step_up)!
 * (https://github.com/myria666/qMovementDoc)
 *
 * Special thanks to Andicraft for their help with implementation stair_step_up!
 * (https://github.com/Andicraft)
 *
 * Source:
 * https://godotengine.org/asset-library/asset/2481
 */

using Godot;
using static Godot.GD;

public partial class StairMovementComponent : Node
{
	[Export]
	public CreatureController Controller { get; set; }

	[Export]
	public float MovementPenalty = 0.8f;

	[Export]
	public float MaxStepUp = 0.5f;

	[Export]
	public float MaxStepDown = 0.5f;

	[ExportGroup("Debug")]

	[Export]
	public bool DebugStepDown = false;

	[Export]
	public bool DebugStepUp = false;

	public dynamic CharacterData { get; set; }

	private Vector3 _vertical = new Vector3(0, 1, 0);
	private Vector3 _horizontal = new Vector3(1, 0, 1);
	private Rid _rid;

	public async override void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		CharacterData = Load<ActorData>(Resources.ActorData);
		CharacterData.StairMovementComponent = this;
		_rid = Controller.GetRid();
	}

	public override void _PhysicsProcess(double delta)
	{
	}

	public void StepDown()
	{
		if (CharacterData is null) {
			return;
		}

		if (CharacterData.IsGrounded) {
			return;
		}

		// If we're falling from a step
		if (Controller.Velocity.Y <= 0 && CharacterData.WasGrounded) {
			_debugStairStepDown("SSD_ENTER", null);

			if (CharacterData.IsOnStairs) {
				CharacterData.VelocityMultiplier *= MovementPenalty;
			}

			var bodyTestResult = new PhysicsTestMotionResult3D();
			var bodyTestParams = new PhysicsTestMotionParameters3D();

			bodyTestParams.From = Controller.GlobalTransform;
			bodyTestParams.Motion = new Vector3(0, MaxStepDown, 0);

			// Initialize body test variables
			if (PhysicsServer3D.BodyTestMotion(_rid, bodyTestParams, bodyTestResult)) {
				// Enters if a collision is detected by body_test_motion
				// Get distance to step and move player downward by that much
				CharacterData.Direction.Y += bodyTestResult.GetTravel().Y;
				Controller.ApplyFloorSnap();
				CharacterData.IsGrounded = true;
				CharacterData.VelocityMultiplier *= MovementPenalty;

				_debugStairStepDown("SSD_APPLIED", bodyTestResult.GetTravel().Y);
			}
		}
	}

	public void StepUp()
	{
		if (CharacterData is null) {
			return;
		}

		var direction = (Vector3)CharacterData.Direction;
		if (direction == Vector3.Zero) {
			return;
		}

		_debugStairStepUp("SSU_ENTER", null);

		// 0. Initialize testing variables
		var bodyTestResult = new PhysicsTestMotionResult3D();
		var bodyTestParams = new PhysicsTestMotionParameters3D();

		// Storing current GlobalTransform for Testing
		Transform3D testTransform = Controller.GlobalTransform;
		// Distance forward we want to check
		Vector3 distance = direction * 0.1f;
		// Controller as origin point
		bodyTestParams.From = Controller.GlobalTransform;
		// Go forward by current distance
		bodyTestParams.Motion = distance;

		_debugStairStepUp("SSU_TEST_POS", testTransform);

		// Pre-check: Are we colliding?
		if (!PhysicsServer3D.BodyTestMotion(_rid, bodyTestParams, bodyTestResult)) {
			_debugStairStepUp("SSU_EXIT_1", null);
			return;
		}

		// 1. Move test_transform to collision location
		// Get remainder from collision
		Vector3 remainder = bodyTestResult.GetRemainder();
		// Move test_transform by distance traveled before collision
		testTransform = testTransform.Translated(bodyTestResult.GetTravel());

		_debugStairStepUp("SSU_REMAINING", remainder);
		_debugStairStepUp("SSU_TEST_POS", testTransform);

		// 2. Move test_transform up to ceiling (if any)
		Vector3 stepUp = MaxStepUp * _vertical;
		bodyTestParams.From = testTransform;
		bodyTestParams.Motion = stepUp;
		PhysicsServer3D.BodyTestMotion(_rid, bodyTestParams, bodyTestResult);
		testTransform = testTransform.Translated(bodyTestResult.GetTravel());

		_debugStairStepUp("SSU_TEST_POS", testTransform);

		// 3. Move test_transform forward by remaining distance
		bodyTestParams.From = testTransform;
		bodyTestParams.Motion = remainder;
		PhysicsServer3D.BodyTestMotion(_rid, bodyTestParams, bodyTestResult);
		testTransform = testTransform.Translated(bodyTestResult.GetTravel());

		_debugStairStepUp("SSU_TEST_POS", testTransform);

		// 3.5 Project remaining along wall normal (if any)
		// So you can walk into wall and up a step
		if (bodyTestResult.GetCollisionCount() != 0) {
			remainder = bodyTestResult.GetRemainder();

			// Uh, there may be a better way to calculate this in Godot.
			Vector3 wallNormal = bodyTestResult.GetCollisionNormal();
			float dotDivMag = direction.Dot(wallNormal) / (wallNormal * wallNormal).Length();
			Vector3 projectedVector = (direction - dotDivMag * wallNormal).Normalized();

			bodyTestParams.From = testTransform;
			bodyTestParams.Motion = remainder * projectedVector;
			PhysicsServer3D.BodyTestMotion(_rid, bodyTestParams, bodyTestResult);
			testTransform = testTransform.Translated(bodyTestResult.GetTravel());

			_debugStairStepUp("SSU_TEST_POS", testTransform);
		}

		// 4. Move test_transform down onto step
		bodyTestParams.From = testTransform;
		bodyTestParams.Motion = MaxStepUp * -_vertical;

		// Return if no collision
		if (!PhysicsServer3D.BodyTestMotion(_rid, bodyTestParams, bodyTestResult)) {
			_debugStairStepUp("SSU_EXIT_2", null);
			return;
		}

		testTransform = testTransform.Translated(bodyTestResult.GetTravel());

		_debugStairStepUp("SSU_TEST_POS", testTransform);

			// 5. Check floor normal for un-walkable slope
		Vector3 surfaceNormal = bodyTestResult.GetCollisionNormal();
		float tempFloorMaxAngle = Controller.FloorMaxAngle + Mathf.DegToRad(20);

		if (Mathf.Snapped(surfaceNormal.AngleTo(_vertical), 0.001) > tempFloorMaxAngle) {
			_debugStairStepUp("SSU_EXIT_3", null);
			return;
		}

		_debugStairStepUp("SSU_TEST_POS", testTransform);

		// 6. Move player up
		Vector3 globalPos = Controller.GlobalPosition;
		float stepUpDist = testTransform.Origin.Y - globalPos.Y;

		_debugStairStepUp("SSU_APPLIED", stepUpDist);

		globalPos.Y = testTransform.Origin.Y;
		Controller.GlobalPosition = globalPos;
		CharacterData.VelocityMultiplier *= MovementPenalty;
	}

	private void _debugStairStepDown(string param, dynamic value)
	{
		if (!DebugStepDown) {
			return;
		}

		switch(param) {
			case "SSD_ENTER":
				Print();
				Print("Stair step down entered");
				break;
			case "SSD_APLLIED":
				Print();
				Print("Stair step down entered applied, travel = ", value);
				break;
		}
	}

	private void _debugStairStepUp(string param, dynamic value)
	{
		if (!DebugStepUp) {
			return;
		}

		switch(param) {
			case "SSD_ENTER":
				Print();
				Print("Stair step down entered");
				break;
			case "SSD_APLLIED":
				Print();
				Print("Stair step down entered applied, travel = ", value);
				break;
		}

		switch(param) {
			case "SSU_ENTER":
				Print();
				Print("SSU: Stair step up entered");
				break;
			case"SSU_EXIT_1":
				Print("SSU: Exited with no collisions");
				break;
			case"SSU_TEST_POS":
				Print("SSU: test_transform current position = ", value);
				break;
			case"SSU_REMAINING":
				Print("SSU: Remaining distance = ", value);
				break;
			case"SSU_EXIT_2":
				Print("SSU: Exited due to no step collision");
				break;
			case"SSU_EXIT_3":
				Print("SSU: Exited due to non-floor stepping");
				break;
			case"SSU_APPLIED":
				Print("SSU: Player moved up by ", value, " units");
				break;
		}
	}
}
