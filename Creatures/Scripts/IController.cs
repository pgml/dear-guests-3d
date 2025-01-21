using Godot;

public interface IController
{
	public enum MoveState {
		IDLE,
		WALK,
		BOXED_IDLE,
		BOXED_WALK,
	}

	public Node3D CharacterNode { get; set; }
	public bool CanMove => true;
	public MoveState CurrentState { get; }
	public CreatureData CreatureData { get; }

	public void Movement(double delta);

	//public void SlopeMovement();

	//public void ToggleUseProgressBar(
	//	double delta,
	//	double maxValue,
	//	bool reverse = false,
	//	bool followMouse = false
	//);

	//public bool IsCharacterBoxed();
}
