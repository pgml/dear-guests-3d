using Godot;

public partial class Actor : Creature
{
	[Export]
	public new CreatureData CreatureData { get; private set; }

	[Export]
	public Inventory Inventory { get; private set; }

	[Export]
	public Belt Belt { get; private set; }

	private Console _console { get {
		return GD.Load<Console>(Resources.Console);
	}}

	private WorldData _worldData;
	private Camera _camera;
	private float _cameraOffset = 0;
	private float _cameraSmoothingDelta = 0;
	private Scene _scene;

	public override void _Ready()
	{
		//await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Tools.CheckAssigned(CreatureData, "ActorData is not assigned", GetType().Name);

		Inventory = GD.Load<Inventory>(Resources.ActorInventory);
		Belt = GD.Load<Belt>(Resources.ActorBelt);
		_worldData = GD.Load<WorldData>(Resources.WorldData);

		// store camera data
		_camera = _worldData.Camera as Camera;
		_cameraSmoothingDelta = _camera.SmoothingDelta;
		_cameraOffset = Position.Y;

		base._Ready();

		CreatureData.Node = this;
		CreatureData.CameraOffset = _cameraOffset;

		_scene = GetTree().CurrentScene as Scene;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed(DGInputMap.ActionCancel)) {
			if (CreatureData.IsMimic) {
				MorphBack();
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!CreatureData.CanMove || CreatureData.IsAnyUiPanelOpen()) {
			if (!CreatureData.IsBuildMoveActive) {
				CreatureData.Direction = Vector3.Zero;
				return;
			}
		}

		if (Belt.SelectedItemResource is ItemResource beltItem) {
			if (beltItem is MimicResource) {
				var objDetection = CreatureData.ObjectDetectionComponent;
				var hoveredObject = objDetection.HoveredObject();

				if (hoveredObject is PhysicsObject obj && obj.CanBeMimicked) {
					CreatureData.CanMimic = true;
					objDetection.HighlightHovered = true;

					if (Input.IsMouseButtonPressed(MouseButton.Left)) {
						CloneMorph(obj, delta);
					}
				}
				else {
					CreatureData.CanMimic = false;
				}
			}
		}

		//SunShadowSprite.RotationDegrees = new Vector3(
		//	0,
		//	_worldData.World.Sun.RotationDegrees.X,
		//	0
		//);

		TopShadow.Frame = CharacterSprite.Frame;
		TopShadow.FrameCoords = CharacterSprite.FrameCoords;

		SunShadow.Frame = CharacterSprite.Frame;
		SunShadow.FrameCoords = CharacterSprite.FrameCoords;

		if (Input.IsPhysicalKeyPressed(Key.Shift)) {
			CreatureData.IsRunning = !CreatureData.IsRunning;
		}

		Vector3 direction = GetInputDirection();

		if (CreatureData is CreatureData cd && !_console.IsOpen) {
			cd.Direction = direction;
			cd.VelocityMultiplier = cd.WalkSpeed;

			if (cd.IsRunning) {
				cd.VelocityMultiplier = cd.RunSpeed;
			}

			cd.ForwardDirection = cd.Direction;
		}
	}

	public Vector3 GetInputDirection()
	{
		Vector2 input = Input.GetVector(
			"action_walk_left",
			"action_walk_right",
			"action_walk_up",
			"action_walk_down"
		);

		return new() {
			X = input.X,
			Y = 0,
			Z = input.Y * Mathf.Sqrt(1.58f)
		};
	}

	public void SpawnAtPosition(Vector3 position, Scene scene)
	{
		var actor = ResourceLoader.Load<PackedScene>(Resources.Actor).Instantiate<Node3D>();
		GD.PrintS(actor, scene);
		scene.AddChild(actor);
		actor.Position = position;
	}

	public async void CloneMorph(PhysicsObject obj, double delta)
	{
		var cdata = CreatureData;
		if (cdata.IsMimic) {
			return;
		}

		cdata.CanMove = false;
		cdata.IsMimic = true;

		// freeze camera for a short amount of time to make
		// transition a little bit smoother
		_camera.Freeze = true;
		// increase camera smoothing to let movement appear a bit
		// heavier since we are an object now
		_camera.SmoothingDelta = 8;

		var tween = CreateTween();
		CharacterSprite.Transparency = 0;
		tween.TweenProperty(CharacterSprite, "transparency", 0.85, 0.3);

		cdata.AudioComponent.PlayMorphSoundMwhoop();
		await ToSignal(tween, Tween.SignalName.Finished);
		tween = CreateTween();
		tween.TweenProperty(CharacterSprite, "transparency", 0, 0.3);

		await ToSignal(tween, Tween.SignalName.Finished);
		tween = CreateTween();
		tween.TweenProperty(CharacterSprite, "transparency", 0.85, 0.3);

		cdata.AudioComponent.PlayMorphSoundMwhoop();
		await ToSignal(tween, Tween.SignalName.Finished);
		tween = CreateTween();
		tween.TweenProperty(CharacterSprite, "transparency", 0, 0.3);

		await ToSignal(tween, Tween.SignalName.Finished);
		await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);
		tween.IsQueuedForDeletion();

		cdata.AudioComponent.PlayMorphSoundBlob();
		// Hide original actor form
		cdata.Controller.Visible = false;

		var itemInstance = obj.Duplicate() as PhysicsObject;
		//var mesh = itemInstance.FindChild("Mesh") as MeshInstance3D;
		float meshHeight = 0;
		foreach (var child in itemInstance.GetChildren()) {
			if (child is MeshInstance3D mesh) {
				//var mesh = itemInstance.GetChild<MeshInstance3D>(0);
				//float meshHeight = mesh.GetAabb().Size.Y * 32;
				meshHeight = mesh.GetAabb().Size.Y * 32;
			}
			else if (child is Sprite3D sprite) {
				if (sprite.RegionEnabled) {
					meshHeight = sprite.RegionRect.Size.Y;
				}
				else {
					meshHeight = sprite.Texture.GetHeight();
				}
			}
		}

		if (cdata.CharacterHeight() > meshHeight) {
			itemInstance.Position = new Vector3(0, _cameraOffset, 0);
			cdata.CameraOffset = itemInstance.Position.Y - 2.0f;
		}

		itemInstance.Freeze = true;
		var collisionMask = Layer.World | Layer.Props;
		itemInstance.CollisionMask = (uint)collisionMask;

		cdata.MimicObject = itemInstance;
		cdata.Parent.AddChild(itemInstance);
		cdata.Controller.SetMorphCollisionMask();

		await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);
		cdata.CanMove = true;
		_camera.Freeze = false;
		itemInstance.Freeze = false;
	}

	/// <summary>
	/// Morphs the actor into a `PhysicsObject`
	/// `itemPath` can be a `uid://`, `res://`
	/// </summary>
	public async void MorphInto(string itemPath)
	{
		// freeze camera for a short amount of time to make
		// transition a little bit smoother
		_camera.Freeze = true;
		// increase camera smoothing to let movement appear a bit
		// heavier since we are an object now
		_camera.SmoothingDelta = 8;
		// Hide original actor form
		CreatureData.Controller.Visible = false;

		var itemInstance = GD.Load<PackedScene>(itemPath).Instantiate<PhysicsObject>();
		itemInstance.Position = new Vector3(0, _cameraOffset, 0);

		CreatureData.IsMimic = true;
		CreatureData.MimicObject = itemInstance;
		CreatureData.Parent.AddChild(itemInstance);
		CreatureData.CameraOffset = itemInstance.Position.Y - 1.5f;

		await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);
		_camera.Freeze = false;
	}

	/// <summary>
	/// Morphs actor back into its original form
	/// </summary>
	public void MorphBack()
	{
		if (CreatureData.IsInShadowAnomaly) {
			return;
		}

		if (CreatureData is CreatureData cdata) {
			cdata.Parent.RemoveChild(CreatureData.MimicObject);
			cdata.IsMimic = false;
			cdata.MimicObject = null;
			cdata.Controller.Visible = true;
			cdata.CameraOffset = _cameraOffset;
			cdata.AudioComponent.PlayMorphSoundBlob();
			cdata.Controller.SetDefaultCollisionMask();
		}

		_camera.SmoothingDelta = _cameraSmoothingDelta;

		var tween = CreateTween();
		CharacterSprite.Transparency = 0;
		tween.TweenProperty(CharacterSprite, "transparency", 0, 0.3);
	}
}
