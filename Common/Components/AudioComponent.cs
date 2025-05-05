using Godot;
//using static Godot.GD;

public partial class AudioComponent : Component
{
	[Export]
	public double FootStepLoopInterval = 0.0;

	private AudioInstance _footstepNode;
	private AudioInstance _audioInstance = new();
	private double _footstepTimer = 0.0;

	public async override void _Ready()
	{
		base._Ready();

		_footstepNode = AudioLibrary.CreateAudioInstance("Footsteps", this, 32);
		_audioInstance = AudioLibrary.CreateAudioInstance("MiscSound", this, 8);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		if (CreatureData is null) {
			return;
		}

		CreatureData.AudioComponent = this;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(CreatureData)) {
			return;
		}

		if (CreatureData.CanMoveAndSlide) {
			_footstepTimer += delta;
			if (_footstepTimer >= FootStepLoopInterval) {
				PlayFootStepSound();
				_footstepTimer = 0.0;
			}
		}
	}

	public void PlayFootStepSound()
	{
		if (!CreatureData.CanMove || CreatureData.IsIdle || !CreatureData.IsOnFloor) {
			return;
		}

		// play only generic for now
		// @todo: make surface dependend
		_footstepNode.Play(AudioLibrary.FootStepGeneric, AudioBus.Game);
	}

	public void PlayMorphSoundMwhoop() => _audioInstance.Play(
		AudioLibrary.MiscMorphMwhoop,
		AudioBus.Game
	);

	public void PlayMorphSoundBlob() => _audioInstance.Play(
		AudioLibrary.MiscMorphBlob,
		AudioBus.Game
	);
}
