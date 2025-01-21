using Godot;
//using static Godot.GD;

public partial class AudioComponent : Component
{
	[Export]
	public double FootStepLoopInterval = 0.0;

	private AudioInstance _footstepNode;
	private double _footstepTimer = 0.0;

	public override void _Ready()
	{
		base._Ready();

		_footstepNode = AudioLibrary.CreateAudioInstance("Footsteps", this);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(CreatureData)) {
			return;
		}

		if (CreatureData.CanMove) {
			_footstepTimer += delta;
			if (_footstepTimer >= FootStepLoopInterval) {
				PlayFootStepSound();
				_footstepTimer = 0.0;
			}
		}
	}

	public void PlayFootStepSound()
	{
		if (CreatureData.Velocity == Vector3.Zero) {
			return;
		}

		// play only generic for now
		// @todo: make surface dependend
		_footstepNode.Play(AudioLibrary.FootStepGeneric);
	}
}
