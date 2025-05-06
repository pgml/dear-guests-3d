using Godot;
using System.Collections.Generic;

public enum Bound
{
	Top,
	Right,
	Bottom,
	Left
}

public partial class Camera : Camera3D
{
	[ExportCategory("Camera Smoothing")]
	[Export]
	public bool EnableSmoothing = true;

	[Export]
	public float SmoothingDelta = 20f;

	public bool Freeze { get; set; } = false;

	private Vector3 _currentCameraPosition = Vector3.Zero;
	private Vector3 _currentPlayerPosition = Vector3.Zero;
	private Vector3 _toPosition = Vector3.Zero;
	private CreatureData _creatureData;

	public async override void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		_creatureData = GD.Load<CreatureData>(Resources.ActorData);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_creatureData is not null && !Freeze) {
			_followPlayer(delta);
		}
	}

	/**
	 * Implement follow logic instead of attaching camera to controller
	 * so that the camera can be stopped at map bounds
	 */
	private void _followPlayer(double delta)
	{
		float cameraOffset = _creatureData.CameraOffset;
		_currentPlayerPosition = _creatureData.Parent.Position;
		var (playerX, playerY, playerZ) = _currentPlayerPosition;

		_toPosition = _currentPlayerPosition;
		_toPosition.Y += Size * 2;
		_toPosition.Z += Size * 2 - cameraOffset;

		_limitCamera();

		if (EnableSmoothing && Position != _toPosition) {
			Position = Position.Lerp(_toPosition, (float)delta * SmoothingDelta);
		}
		else {
			Position = _toPosition;
		}
	}

	private void _limitCamera()
	{
		var (playerX, playerY, playerZ) = _currentPlayerPosition;

		var isNearBound = false;
		var limitTop = false;
		var limitBottom = false;
		var limitLeft = false;
		var limitRight = false;

		foreach (var mapBound in _mapBounds()) {
			if (mapBound.IsOnScreen()) {
				isNearBound = true;
				_currentCameraPosition = Position;

				if (mapBound.Name == Bound.Top.ToString()) {
					limitTop = true;
				}
				if (mapBound.Name == Bound.Right.ToString()) {
					limitRight = true;
				}
				if (mapBound.Name == Bound.Bottom.ToString()) {
					limitBottom = true;
				}
				if (mapBound.Name == Bound.Left.ToString()) {
					limitLeft = true;
				}
			}
		}

		var (camX, camY, camZ) = _currentCameraPosition;
		if (isNearBound) {
			if ((playerX < camX && limitLeft) ||
				(playerX > camX && limitRight))
			{
				_toPosition.X = camX;
			}

			float cameraOffset = _creatureData.CameraOffset;
			if ((playerZ + (Size * 2) < camZ + cameraOffset && limitTop) ||
				(playerZ + (Size * 2) > camZ + cameraOffset && limitBottom))
			{
				_toPosition.Y = camY;
				_toPosition.Z = camZ;
			}

			//if ((playerY + (Size * 2) < camY && limitTop)
			//	|| (playerY + (Size * 2) > camY && limitBottom)
			//) {
			//	_toPosition.Y = camY;
			//}
		}
		else {
			_currentCameraPosition = Position;
		}
	}

	private List<VisibleOnScreenNotifier3D> _mapBounds()
	{
		List<VisibleOnScreenNotifier3D> mapBounds = new();

		foreach (var node in GetTree().GetNodesInGroup("MapBounds")) {
			if (node is VisibleOnScreenNotifier3D bound) {
				mapBounds.Add(bound);
			}
		}

		return mapBounds;
	}
}
