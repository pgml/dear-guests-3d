using Godot;

//[Tool]
public partial class Scene : Node3D
{
	[Signal]
	public delegate void SceneLoadedEventHandler();

	protected CreatureData ActorData;

	private UiLoading _uiLoading;
	private Godot.Collections.Array<Node> _instancePlaceholders;

	private PackedScene _sceneTransition;

	public async override void _Ready()
	{
		_instancePlaceholders = GetTree().GetNodesInGroup("InstancePlaceholder");
		_sceneTransition = GD.Load<PackedScene>(Resources.SceneTransition);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		ActorData = GD.Load<CreatureData>(Resources.ActorData);

		if (_instancePlaceholders.Count > 0) {
			_uiLoading = GD.Load<PackedScene>(Resources.UiLoading)
				.Instantiate<UiLoading>();

			CallDeferred("add_child", _uiLoading);
			_uiLoading.Show();
			_loadPlaceholders();
			SceneLoaded += _onSceneLoaded;
		}
	}

	public async void Change(Node3D from, string to)
	{
		var actor = GetTree().GetNodesInGroup("Actor")[0];
		var transition = _sceneTransition.Instantiate<SceneTransition>();
		AddChild(transition);

		ActorData.CanMoveAndSlide = false;
		ActorData.Direction = Vector3.Zero;
		transition.FadeIn();
		await transition.AnimationFinished();

		if (IsInstanceValid(actor.GetParent())) {
			actor.GetParent().RemoveChild(actor);
		}

		from.GetTree().CallDeferred("change_scene_to_file", to);
		await ToSignal(GetTree().CreateTimer(.4), "timeout");

		transition.FadeOut();
		await ToSignal(GetTree().CreateTimer(.5), "timeout");

		ActorData.CanMoveAndSlide = true;
		transition.QueueFree();

		GD.PrintS($"---: {GetTree().CurrentScene}");
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
		GD.Print("asdasd");
		_uiLoading.FadeOut();
		_uiLoading.QueueFree();
	}
}
