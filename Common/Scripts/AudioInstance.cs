using Godot;
using static Godot.GD;

public partial class AudioInstance : Node3D
{
	private AudioStreamPlayer3D _audio;

	public bool QueueFreeOnFinish = false;

	public override void _Ready()
	{
		_audio = GetNode<AudioStreamPlayer3D>("Audio");
	}

	public override void _Process(double delta)
	{
		if (QueueFreeOnFinish && !_audio.Playing) {
			QueueFree();
		}
	}

	public void Play(AudioClip audioClip)
	{
		int randomIndex = RandRange(0, audioClip.AudioFiles.Count - 1);

		_audio.Stream = audioClip.AudioFiles[randomIndex];
		_audio.VolumeDb = (float)RandRange(
			audioClip.MinVolume,
			audioClip.MaxVolume
		);
		_audio.Autoplay = audioClip.AutoPlay;
		_audio.Play();
	}
}

