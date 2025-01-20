using Godot;
using static Godot.GD;

public partial class AudioLibrary : Resource
{
	[ExportGroup("FootSteps")]
	[Export]
	public AudioClip FootStepGeneric;

	[ExportGroup("QuickBar")]
	[Export]
	public AudioClip ChangeQuickbarSlot;

	[ExportGroup("Actions")]
	[Export]
	public AudioClip PickupItem;

	[ExportGroup("Containers")]
	[Export]
	public AudioClip OpenChest;

	public AudioInstance CreateAudioInstance(string instanceName, Node scene)
	{
		PackedScene audioInstance = Load<PackedScene>(Resources.AudioInstance);
		AudioInstance instance = audioInstance.Instantiate<AudioInstance>();
		instance.Name = instanceName;
		scene.AddChild(instance);
		return instance;
	}
}

