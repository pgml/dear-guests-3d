using Godot;

public partial class WorldData : Resource
{
	public World World { get; set; } = null;
	public SubViewport Viewport { get; set; } = null;
	public Camera3D Camera { get; set; } = null;
}
