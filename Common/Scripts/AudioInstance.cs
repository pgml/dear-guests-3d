using Godot;
using static Godot.GD;

public enum AudioBus
{
	Master,
	Game,
	Fx,
	Music
}

public partial class AudioInstance : Node3D
{
	public AudioStreamPlayer3D Audio;
	public AudioStreamPlayer AudioUi;
	public bool QueueFreeOnFinish = false;

	public override void _Ready()
	{
		Audio = GetNode<AudioStreamPlayer3D>("Audio");
		AudioUi = GetNode<AudioStreamPlayer>("UiAudio");
		Audio.Finished += _onAudioFinished;
		AudioUi.Finished += _onAudioFinished;
	}

	public void Play(AudioClip audioClip, AudioBus bus = AudioBus.Master)
	{
		int randomIndex = RandRange(0, audioClip.AudioFiles.Count - 1);

		Audio.Stream = audioClip.AudioFiles[randomIndex];
		Audio.Bus = bus.ToString();

		Audio.VolumeLinear = (float)RandRange(
			audioClip.MinVolume,
			audioClip.MaxVolume
		);
		Audio.Autoplay = audioClip.AutoPlay;
		Audio.Play();
	}

	public void Stop()
	{
		Audio.Stop();
	}

	public void PlayUiSound(AudioClip audioClip, AudioBus bus = AudioBus.Fx)
	{
		int randomIndex = RandRange(0, audioClip.AudioFiles.Count - 1);

		AudioUi.Stream = audioClip.AudioFiles[randomIndex];
		AudioUi.Bus = bus.ToString();

		AudioUi.VolumeLinear = (float)RandRange(
			audioClip.MinVolume,
			audioClip.MaxVolume
		);
		AudioUi.Autoplay = audioClip.AutoPlay;
		AudioUi.Play();
	}

	public void UiStop()
	{
		AudioUi.Stop();
	}

	private void _onAudioFinished()
	{
		if (!QueueFreeOnFinish) {
			return;
		}

		QueueFree();
	}
}
