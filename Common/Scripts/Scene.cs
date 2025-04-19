using Godot;

//[Tool]
public partial class Scene : Node3D
{
	[Signal]
	public delegate void SceneLoadedEventHandler();

	[Export]
	public string SceneName { get; set; }

	protected CreatureData ActorData;

	private UiLoading _uiLoading;
	private Godot.Collections.Array<Node> _instancePlaceholders;
	private PackedScene _sceneTransition;
	private SceneTree _tree;

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

		if (!IsInstanceValid(_tree)) {
			_tree = GetTree();
		}

		if (!HasActor(_tree)) {
			SpawnActorAt(new Vector3(0, 3.6f, 0), _tree.CurrentScene as Scene);
	 	}
	}

	public async void Change(Node3D from, string to)
	{
		var actor = ActorData.Character<Actor>();
		var transition = _sceneTransition.Instantiate<SceneTransition>();
		AddChild(transition);

		ActorData.CanMoveAndSlide = false;
		ActorData.Direction = Vector3.Zero;
		transition.FadeIn();
		await transition.AnimationFinished();

		//if (IsInstanceValid(actor.GetParent())) {
		//	actor.GetParent().RemoveChild(actor);
		//}

		_tree = GetTree();
		Node currentScene = _tree.CurrentScene;
		var toScene = ResourceLoader.Load<PackedScene>(to).Instantiate<Scene>();
		_tree.Root.AddChild(toScene);
		(currentScene as Node3D).Visible = false;

		await ToSignal(GetTree().CreateTimer(.4), "timeout");
		transition.FadeOut();
		await ToSignal(GetTree().CreateTimer(.5), "timeout");

		ActorData.CanMoveAndSlide = true;

		transition.QueueFree();
		_tree.Root.RemoveChild(currentScene);

		_tree.CurrentScene = toScene;

		_setActorPosition(currentScene.SceneFilePath);

		GD.PrintS("changed scene to:", toScene.SceneName);
	}

	public bool HasActor(SceneTree tree)
	{
		return tree.GetNodesInGroup("Actor").Count > 0;
	}

	public Node3D SpawnActorAt(Vector3 position, Scene scene)
	{
		var actor = ResourceLoader.Load<PackedScene>(Resources.Actor).Instantiate<Node3D>();
		scene.AddChild(actor);
		actor.Position = position;
		return actor;
	}

	private void _setActorPosition(string currentSceneRes)
	{
		foreach (EntranceMarker marker in _tree.GetNodesInGroup("EntranceMarker")) {
			var fromScenePath = ResourceUid.GetIdPath(ResourceUid.TextToId(marker.FromScene));
			if (fromScenePath == currentSceneRes) {
				Vector3 startPos = marker.GlobalPosition;
				// @todo determine character height dynamically
				if (!HasActor(_tree)) {
					var actor = SpawnActorAt(startPos, _tree.CurrentScene as Scene);
					//(actor.GetParent() as Controller).SetFacingDirection(marker.GetFacingDirection());
					GD.PrintS(ActorData.Controller, marker.GetFacingDirection());
					ActorData.Controller.SetFacingDirection(marker.GetFacingDirection());
		 		}
				else {
					startPos.Y += 3.55f;
					ActorData.Parent.GlobalPosition = startPos;
					ActorData.Controller.SetFacingDirection(marker.GetFacingDirection());
				}
			}
		}
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
