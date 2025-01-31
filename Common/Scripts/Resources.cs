using Godot;

public partial class Resources : Node3D
{
	// Godot resources
	public static readonly string ActorData = "res://Creatures/actor_data.tres";
	public static readonly string AiData = "res://Creatures/ai_data.tres";
	public static readonly string AudioLibrary = "res://Common/Tools/audio_library.tres";
	public static readonly string DateTime = "res://Common/date_time.tres";

	// Scenes
	public static readonly string AudioInstance = "res://Common/Tools/audio_instance.tscn";
	public static readonly string SceneTransition = "res://Common/Tools/scene_transition.tscn";

	// UI
	public static readonly string UiLoading = "res://UI/ui_loading.tscn";
}
