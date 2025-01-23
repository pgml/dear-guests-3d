using Godot;

//[Tool]
public partial class Scene : Node3D
{
	[Signal]
	public delegate void SceneLoadedEventHandler();

	private UiLoading _uiLoading;
	private Godot.Collections.Array<Node> _instancePlaceholders;

	public override void _Ready()
	{
		_instancePlaceholders = GetTree().GetNodesInGroup("InstancePlaceholder");

		_uiLoading = GD.Load<PackedScene>(Resources.UiLoading)
			.Instantiate<UiLoading>();

		CallDeferred("add_child", _uiLoading);

		_uiLoading.Show();

		_loadPlaceholders();

		SceneLoaded += _onSceneLoaded;
	}

	private async void _loadPlaceholders()
	{
		GD.Print("LOADING PLACEHOLDERS");

		var groups = Time.GetTicksMsec();

		int i = 1;
		foreach (var child in _instancePlaceholders) {
			if (child is not InstancePlaceholder) {
				continue;
			}

			_uiLoading.UpdateLabel($"LOADING PROPS...({i}/{_instancePlaceholders.Count})");

			var placeholder = child as InstancePlaceholder;
			string path = placeholder.GetInstancePath();

			await AsyncLoader.LoadResource<Resource>(path, "", true);

			placeholder.CreateInstance();

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			i++;
		}

		GD.Print("Time: ", Time.GetTicksMsec() - groups, " ms");

		EmitSignal(SignalName.SceneLoaded);
	}

	private void _onSceneLoaded()
	{
		_uiLoading.FadeOut();
		_uiLoading.QueueFree();
	}
}
