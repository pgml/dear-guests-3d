using Godot;

public partial class AudioClip : Resource
{
	[Export]
	public Godot.Collections.Array<AudioStream> AudioFiles { get; set; }

	[Export]
	public double MinVolume = 0.0;

	[Export]
	public double MaxVolume = 1.0;

	[Export]
	public bool AutoPlay = false;
}

