using Godot;
using static Godot.GD;

public partial class AudioLibrary : Resource
{
	[ExportGroup("FootSteps")]
	[Export] public AudioClip FootStepGeneric { get; set; }

	[ExportGroup("Inventory")]
	[Export] public AudioClip InventoryBrowse { get; set; }
	[Export] public AudioClip InventoryOpen { get; set; }
	[Export] public AudioClip InventoryClose { get; set; }

	[ExportGroup("Actions")]
	[Export] public AudioClip PickupItem { get; set; }

	[ExportGroup("Replicator")]
	[Export] public AudioClip ReplicatorType1Open { get; set; }
	[Export] public AudioClip ReplicatorType1Close { get; set; }
	[Export] public AudioClip ReplicatorInsertArtifact { get; set; }
	[Export] public AudioClip ReplicatorRetrieveArtifact { get; set; }
	[Export] public AudioClip ReplicatorStart1 { get; set; }
	[Export] public AudioClip ReplicatorHum1 { get; set; }
	[Export] public AudioClip ReplicatorStop1 { get; set; }

	[ExportGroup("Misc")]
	[Export] public AudioClip MiscClick { get; set; }
	[Export] public AudioClip MiscBleep { get; set; }
	[Export] public AudioClip MiscPlace { get; set; }

	public AudioInstance CreateAudioInstance(
		string instanceName,
		Node scene,
		int maxPolyphony = 1
	)
	{
		foreach (var child in scene.GetChildren()) {
			if (child is AudioInstance && child.Name == instanceName) {
				return child as AudioInstance;
			}
		}

		PackedScene audioInstance = Load<PackedScene>(Resources.AudioInstance);
		AudioInstance instance = audioInstance.Instantiate<AudioInstance>();
		instance.Name = instanceName;
		scene.AddChild(instance);
		instance.Audio.MaxPolyphony = maxPolyphony;
		instance.AudioUi.MaxPolyphony = maxPolyphony;
		return instance;
	}

	public void RemoveAudioInstance(string instanceName, Node scene)
	{
		scene.FindChild(instanceName, true).QueueFree();
	}
}
