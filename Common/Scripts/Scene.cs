using Godot;
using System.Collections.Generic;

//[Tool]
public partial class Scene : Node3D
{
	[Signal]
	public delegate void SceneLoadedEventHandler();

	[Export]
	public string SceneName { get; set; }

	protected CreatureData ActorData;

	private UiLoading _uiLoading;
	private SceneTree _tree;
	private Godot.Collections.Array<Node> _instancePlaceholders;
	private PackedScene _sceneTransition;
	private Dictionary<string, PackedScene> _sceneCache = new();

	public async override void _Ready()
	{
		_sceneTransition = GD.Load<PackedScene>(Resources.SceneTransition);
		_uiLoading = GD.Load<PackedScene>(Resources.UiLoading).Instantiate<UiLoading>();

		if (!IsInstanceValid(_tree)) {
			_tree = GetTree();
		}

		GD.PrintS("");
		GD.PrintS(" -- LOADING SCENE");

		var groups = Time.GetTicksMsec();
		_uiLoading.Show();
		CallDeferred("add_child", _uiLoading);

		_instancePlaceholders = _tree.GetNodesInGroup("InstancePlaceholder");

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		ActorData = GD.Load<CreatureData>(Resources.ActorData);

		if (!HasActor(_tree)) {
			SpawnActorAt(new Vector3(0, 3.6f, 0), _tree.CurrentScene as Scene);
	 	}

		await _preloadScenes();

		if (_instancePlaceholders.Count > 0 && _sceneCache.Count > 0) {
			SceneLoaded += _onSceneLoaded;
			await _loadPlaceholders();
		}

		GD.PrintS(" -- LOADED SCENE IN:", Time.GetTicksMsec() - groups, "ms");
	}

	public async void Change(Node3D from, string to)
	{
		var transition = _sceneTransition.Instantiate<SceneTransition>();
		GetTree().Root.AddChild(transition);

		ActorData.CanMove = false;
		ActorData.Velocity = Vector3.Zero;

		transition.FadeIn();
		await transition.AnimationFinished();

		_tree = GetTree();
		_tree.CurrentScene.RemoveChild(ActorData.Parent);

		Node currentScene = _tree.CurrentScene;
		var toScene = ResourceLoader.Load<PackedScene>(to).Instantiate<Scene>();
		_tree.Root.AddChild(toScene);
		(currentScene as Node3D).Visible = false;

		//ActorData.CanMoveAndSlide = true;

		_tree.Root.RemoveChild(currentScene);
		_tree.CurrentScene = toScene;
		_setActorPosition(currentScene.SceneFilePath);

		transition.FadeOut();
		await transition.AnimationFinished();
		currentScene.QueueFree();
		transition.QueueFree();

		ActorData.CanMove = true;

		GD.PrintS("changed scene to:", toScene.SceneName);
	}

	public bool HasActor(SceneTree tree)
	{
		return tree.GetNodesInGroup("Actor").Count > 0;
	}

	/// <summary>
	/// Spawns an actor at the given positPosition
	/// </summary>
	public void SpawnActorAt(Vector3 position, Scene scene)
	{
		if (_tree.GetNodesInGroup("Actor").Count > 0) {
			return;
		}

		GD.PrintS(" -- Spawning actor at:", position);

		var actor = new Node3D();

		if (ActorData.Parent is null) {
			actor = ResourceLoader.Load<PackedScene>(Resources.Actor).Instantiate<Node3D>();
		}
		else {
			actor = ActorData.Parent;
		}

		actor.Position = position;
		scene.AddChild(actor);
	}

	/// <summary>
	/// Sets the actor position, after a scene was changed, to the
	/// corresponding entrance marker
	/// </summary>
	private void _setActorPosition(string currentSceneRes)
	{
		foreach (EntranceMarker marker in _tree.GetNodesInGroup("EntranceMarker")) {
			var fromScenePath = ResourceUid.GetIdPath(ResourceUid.TextToId(marker.FromScene));

			if (fromScenePath == currentSceneRes) {
				Vector3 startPos = marker.GlobalPosition;
				// @todo determine character height dynamically
				//startPos.Y += 1.9f;

				if (!HasActor(_tree)) {
					SpawnActorAt(startPos, _tree.CurrentScene as Scene);
		 		}
				else {
					ActorData.Parent.Position = startPos;
				}

				ActorData.Controller.StartingDirection = marker.GetFacingDirection();
			}
		}
	}

	/// <summary>
	/// Replaces all placeholder props with the actuall prop instances
	/// </summary>
	private async System.Threading.Tasks.Task _loadPlaceholders()
	{
		GD.PrintS(" -- LOADING PLACEHOLDERS");

		var groups = Time.GetTicksMsec();

		int batchSize = 10;
		int i = 0;

		foreach (var child in _instancePlaceholders) {
			if (child is not InstancePlaceholder placeholder) {
				continue;
			}

			if (placeholder.IsInsideTree()) {
				placeholder.CreateInstance();
			}

			if (++i % batchSize == 0) {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			i++;
		}

		GD.PrintS(" -- LOADED PLACEHOLDERS IN:", Time.GetTicksMsec() - groups, "ms");

		EmitSignal(SignalName.SceneLoaded);
	}

	/// <summary>
	/// Preloads props that are required in the current scene and
	/// stores them in a cache
	/// </summary>
	private async System.Threading.Tasks.Task _preloadScenes()
	{
		int batchSize = 5;
		int i = 1;

		foreach (var child in _instancePlaceholders)
		{
			if (child is not InstancePlaceholder ph) {
				continue;
			}

			var path = ph.GetInstancePath();

			if (_sceneCache.ContainsKey(path)) {
				continue;
			}

			_uiLoading.UpdateLabel("LOADING...");

			i++;
			if (i % batchSize == 0) {
				await ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
			}

			//var scene = ResourceLoader.LoadThreadedGet(path) as PackedScene;
			//var scene = await AsyncLoader.LoadResource<PackedScene>(path, "", true);
			var scene = GD.Load<PackedScene>(path);
			_sceneCache[path] = scene;
		}

		GD.PrintS(" -- CACHED SCENES:", _sceneCache.Count);
	}

	private void _onSceneLoaded()
	{
		_uiLoading.FadeOut();
		_uiLoading.QueueFree();
	}
}
