using Godot;

[Tool]
[GlobalClass]
public partial class Generator : Equipment
{
	[ExportToolButton(text: "Set up mesh")]
	public Callable SetupEquipmentMesh => Callable.From(SetupMesh);

	[Export]
	public EquipmentType Type { get; set; }

	public AudioLibrary AudioLibrary { get; private set; }
	public AudioInstance AudioInstance { get; private set; }
	public AudioInstance ContinuousAudioInstance { get; private set; }

	public bool IsRunning { get; set; } = false;

	public override void _Ready()
	{
		base._Ready();

		if (!Engine.IsEditorHint()) {
			AudioLibrary = GD.Load<AudioLibrary>(Resources.AudioLibrary);
			AudioInstance = AudioLibrary.CreateAudioInstance("Replicator", this, 8);
			ContinuousAudioInstance = AudioLibrary.CreateAudioInstance("ReplicatorLoop", this, 32);

			ContinuousAudioInstance.Audio.Finished += _onContinuousAudioFinished;
		}
	}

	private void _onContinuousAudioFinished()
	{
		// replay audio but don't fix position
		ContinuousAudioInstance.Play(
			AudioLibrary.ReplicatorHum1,
			AudioBus.Game,
			false
		);
	}
}
