using Godot;
using System.Collections.Generic;

//[Tool]
public partial class Scene : Node3D
{
	[Signal]
	public delegate void SceneLoadedEventHandler();

	[Export]
	public string SceneName { get; set; }

	public Dictionary<string, string> PlaceholderDict { get; set; } = new();

	protected CreatureData ActorData;

	private UiLoading _uiLoading;
	private SceneTree _tree;
	private Godot.Collections.Array<Node> _instancePlaceholders;
	private PackedScene _sceneTransition;
	private Dictionary<string, PackedScene> _sceneCache = new();

	private WorldData _worldData;

	public async override void _Ready()
	{
		_sceneTransition = GD.Load<PackedScene>(Resources.SceneTransition);
		_uiLoading = GD.Load<PackedScene>(Resources.UiLoading).Instantiate<UiLoading>();

		if (!IsInstanceValid(_tree)) {
			_tree = GetTree();
		}

		GD.PrintS("");
		GD.PrintRich($"[b]LOADING SCENE[/b]");

		var groups = Time.GetTicksMsec();
		_uiLoading.Show();
		CallDeferred("add_child", _uiLoading);

		_instancePlaceholders = _tree.GetNodesInGroup("InstancePlaceholder");

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		_worldData = GD.Load<WorldData>(Resources.WorldData);
		ActorData = GD.Load<CreatureData>(Resources.ActorData);

		if (!HasActor(_tree)) {
			SpawnActorAt(new Vector3(0, 3.6f, 0), _tree.CurrentScene as Scene);
	 	}

		await _preloadScenes();

		if (_instancePlaceholders.Count > 0 && _sceneCache.Count > 0) {
			SceneLoaded += _onSceneLoaded;
			await _loadPlaceholders();
		}

		GD.PrintRich($"[b]LOADED SCENE IN: ", Time.GetTicksMsec() - groups, "ms[/b]");

		EmitSignal(SignalName.SceneLoaded);
	}

	/// <summary>
	/// Changes a scene to a given scene `to`
	/// </summary>
	public async void Change(Node3D from, string to)
	{
		var transition = _sceneTransition.Instantiate<SceneTransition>();
		GetTree().Root.AddChild(transition);

		ActorData.CanMove = false;
		ActorData.Direction = Vector3.Zero;

		transition.FadeIn();
		await transition.AnimationFinished();

		_tree = GetTree();

		Node currentScene = _tree.CurrentScene;
		var toScene = ResourceLoader.Load<PackedScene>(to).Instantiate<Scene>();
		_tree.Root.AddChild(toScene);
		(currentScene as Node3D).Visible = false;

		_tree.Root.RemoveChild(currentScene);
		_tree.CurrentScene = toScene;
		_setActorPosition(currentScene.SceneFilePath);

		transition.FadeOut();
		await transition.AnimationFinished();
		currentScene.QueueFree();
		transition.QueueFree();

		ActorData.CanMove = true;

		GD.Print("");
		GD.PrintRich($"[b]Changed scene to: ", toScene.SceneName, "[/b]");
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

		GD.PrintRich($"[i] -- Spawning actor at: ", position, "[/i]");

		var actor = ResourceLoader.Load<PackedScene>(Resources.Actor);
		var instance = actor.Instantiate<Node3D>();
		instance.Position = position;
		_worldData.Viewport.AddChild(instance);
	}

	/// <summary>
	/// Sets the actor position, after a scene was changed, to the
	/// corresponding entrance marker
	/// </summary>
	private void _setActorPosition(string currentSceneRes)
	{
		foreach (EntranceMarker marker in _tree.GetNodesInGroup("EntranceMarker")) {
			string fromScenePath = ResourceUid.GetIdPath(
				ResourceUid.TextToId(marker.FromScene)
			);

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
		GD.PrintRich("[i] -- LOADING PLACEHOLDERS[/i]");

		ulong groups = Time.GetTicksMsec();
		int batchSize = 10;
		int i = 0;

		foreach (var child in _instancePlaceholders) {
			if (child is not InstancePlaceholder placeholder) {
				continue;
			}

			if (placeholder.IsInsideTree()) {
				var instance = placeholder.CreateInstance(true);
				PlaceholderDict.Add(instance.Name, placeholder.GetInstancePath());
			}

			if (++i % batchSize == 0) {
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			i++;
		}

		GD.PrintRich("[i] -- LOADED PLACEHOLDERS IN: ", Time.GetTicksMsec() - groups, "ms[/i]");
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

		GD.PrintS("[i] -- CACHED SCENES:", _sceneCache.Count, "[/i]");
	}

	private void _onSceneLoaded()
	{
		_uiLoading.FadeOut();
		_uiLoading.QueueFree();
	}
}
