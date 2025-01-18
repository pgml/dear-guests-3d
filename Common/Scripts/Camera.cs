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
	[Export]
	public CharacterBody3D Controller { get; set; }

	private Vector3 _currentCameraPosition = Vector3.Zero;
	private Vector3 _currentPlayerPosition = Vector3.Zero;
	private Vector3 _toPosition = Vector3.Zero;

	public override void _Ready() {}

	public override void _PhysicsProcess(double delta)
	{
		_followPlayer();
	}

	/**
	 * Implement follow logic instead of attaching camera to controller
	 * so that the camera can be stopped at map bounds
	 */
	private void _followPlayer()
	{
		_currentPlayerPosition = Controller.Position;
		var (playerX, playerY, playerZ) = _currentPlayerPosition;

		_toPosition = new Vector3(
			playerX,
			playerY + (Size * 2),
			playerZ + (Size * 2)
		);

		_limitCamera();

		Position = _toPosition;
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
			if ((playerX < camX && limitLeft)
				|| (playerX > camX && limitRight)
			) {
				_toPosition.X = camX;
			}

			if ((playerZ + (Size * 2) < camZ && limitTop)
				|| (playerZ + (Size * 2) > camZ && limitBottom)
			) {
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
			if (node is not VisibleOnScreenNotifier3D) {
				continue;
			}

			var bound = node as VisibleOnScreenNotifier3D;
			mapBounds.Add(bound);
		}

		return mapBounds;
	}
}
