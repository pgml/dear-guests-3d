using Godot;

[Tool]
public partial class InstancePlaceholderGroup : Node3D
{
	[ExportToolButton(text: "Set Children as Placeholders")]
	public Callable SetInstancesAsPlaceholder => Callable.From(_setInstancesAsPlaceholer);

	[ExportToolButton(text: "Unset Children as Placeholders")]
	public Callable SetUnsetInstancesAsPlaceholder => Callable.From(_unsetInstancesAsPlaceholer);

	public override void _EnterTree()
	{
		foreach (var child in GetChildren()) {
			if (child is not InstancePlaceholder) {
				continue;
			}
			child.AddToGroup("InstancePlaceholder");
		}
	}

	private void _setInstancesAsPlaceholer()
	{
		foreach (var child in GetChildren()) {
			if (child is InstancePlaceholder) {
				continue;
			}
			child.SetSceneInstanceLoadPlaceholder(true);
			child.AddToGroup("InstancePlaceholder");
		}
	}

	private void _unsetInstancesAsPlaceholer()
	{
		foreach (var child in GetChildren()) {
			if (child is InstancePlaceholder) {
				continue;
			}
			child.SetSceneInstanceLoadPlaceholder(false);
			child.RemoveFromGroup("InstancePlaceholder");
		}
	}
}
