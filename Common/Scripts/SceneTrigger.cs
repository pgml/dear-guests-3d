using Godot;

public partial class SceneTrigger : Area3D
{
	[Export(PropertyHint.File, "*.tscn")]
	public string ChangeSceneTo;
	//[Export]
	//public PackedScene ChangeSceneTo;

	[Export]
	public bool AutoTrigger = false;

	[Export]
	public AudioClip TriggerSound;

	// resources
	private CreatureData ActorData;

	private bool _inTriggerArea = false;

	private bool _isHoveringArea = false;

	public async override void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		ActorData = GD.Load<CreatureData>(Resources.ActorData);

		GD.PrintS("asda");
		//AreaEntered += _onAreaEntered;
		//AreaExited += _onAreaExited;
		//MouseEntered += _onMouseEntered;
		//MouseExited += _onMouseExited;
		BodyEntered += _onBodyEntered;
		BodyExited += _onBodyExited;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("action_use") && _inTriggerArea) {
			_enterArea();
		}
	}

	private void _enterArea()
	{
		var scene = GetTree().CurrentScene as Scene;

		scene.Change(GetOwner<Node3D>(), ChangeSceneTo);
		//_sceneManager.FromScene = GetTree().CurrentScene.SceneFilePath;
		//_sceneManager.ChangeScene(GetOwner<Node2D>(), ChangeSceneTo);
	}

	private void _onBodyEntered(Node3D body)
	{
		//if (body.IsInGroup("SceneTriggerArea")) {
		if (body is CharacterBody3D) {
			_inTriggerArea = true;

			if (AutoTrigger) {
				_enterArea();
			}
		}
	}

	private void _onBodyExited(Node3D body)
	{
		_inTriggerArea = false;
	}

	private void _onMouseEntered()
	{
		if (_inTriggerArea) {
			_isHoveringArea = true;
		}
	}

	private void _onMouseExited()
	{
		_isHoveringArea = false;
	}
}
