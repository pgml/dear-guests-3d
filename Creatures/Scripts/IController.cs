public interface IController
{
	public enum MoveState {
		IDLE,
		WALK,
		JUMP,
		BOXED_IDLE,
		BOXED_WALK,
	}

	public bool CanMoveAndSlide => true;
	public MoveState CurrentState { get; }

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
