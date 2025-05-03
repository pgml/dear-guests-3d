using Godot;

/// <summary>
/// EquipmentType represents level of the equipment.<br />
/// Each level has its own efficiency and perks depending on what
/// kind of equipment we're dealing with<br />
/// The higher the type, the better the equipmemt
/// </summary>
public enum EquipmentType
{
	Type1,
	Type2,
	Type3,
	Type4
}

public partial class Equipment : Node3D
{
	[Export]
	public string EquipmentName { get; set; }

	[Export]
	public Area3D TriggerArea { get; set; }

	[Export]
	public ItemType AllowedInputType { get; set; }

	[Export(PropertyHint.File, "*.tres")]
	public string ResourcePath { get; set; }

	[Export]
	public Color MeshBackLightColor { get; set; }

	public Node Mesh {
		get {
			if (FindChild(_meshNodeName) is null) {
				return null;
			}
			return GetNode(_meshNodeName);
		}
	}

	public PackedScene UseIndicator { get {
		return GD.Load<PackedScene>(Resources.UseIndicator);
	}}

	public PackedScene PowerIndicator { get {
		return GD.Load<PackedScene>(Resources.PowerIndicator);
	}}

	public PackedScene QuickInventory { get {
		return GD.Load<PackedScene>(Resources.UiQuickInventory);
	}}

	public bool CanUse { get; set; } = false;

	public DateTime DateTime { get; private set; }
	public UiQuickInventory QuickInventoryInst = null;
	//public Vector3 TopDownScale = new Vector3(1.12f, 1.584f, 1.6f);
	public Vector3 TopDownScale = new Vector3(0.69f, 0.99f, 1f);

	protected CreatureData ActorData;

	private Node3D _indicatorInst;
	private Node3D _powerIndicatorInst;
	private string _meshNodeName = "Mesh";

	private EquipmentResource _itemResource = null;

	public async override void _Ready()
	{
		if (!Engine.IsEditorHint()) {
			DateTime = GD.Load<DateTime>(Resources.DateTime);
			//QuickInventory = mainUI.FindChild("QuickInventory") as UiQuickInventory;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			ActorData = GD.Load<CreatureData>(Resources.ActorData);

			_powerIndicatorInst = PowerIndicator.Instantiate<Node3D>();
			_powerIndicatorInst.Position = new Vector3(0, ColliderHeight(), 0);
			_powerIndicatorInst.Visible = false;
			AddChild(_powerIndicatorInst);

			_itemResource = ResourceLoader.Load<EquipmentResource>(ResourcePath);
		}

		TriggerArea.BodyEntered += _onTriggerAreaBodyEntered;
		TriggerArea.BodyExited += _onTriggerAreaBodyExited;
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) {
			return;
		}

		//if (_quickInventory.IsOpen) {
		//	_quickInventory.Position = QuickInventoryPosition();
		//}

		if (IsInstanceValid(_powerIndicatorInst)) {
			_powerIndicatorInst.Visible = !HasPower() && _itemResource.NeedsPower;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (Engine.IsEditorHint() || (ActorData is not null && ActorData.IsConsoleOpen)) {
			return;
		}

		if (!IsInstanceValid(QuickInventoryInst) &&
			@event.IsActionReleased("action_use") &&
			CanUse)
		{
			QuickInventoryInst = QuickInventory.Instantiate<UiQuickInventory>();
			GetNode("/root/MainUI").AddChild(QuickInventoryInst);
			Vector2 position = InventoryPosition(QuickInventoryInst);
			QuickInventoryInst.Description = $"Use {EquipmentName}";
			QuickInventoryInst.RestrictTypeTo = AllowedInputType;
			QuickInventoryInst.Open(position);
		}

		if (IsInstanceValid(QuickInventoryInst)) {
			if (@event.IsActionPressed(DGInputMap.ActionExit)) {
				//QuickInventoryInstance.Close();
				QuickInventoryInst.QueueFree();
			}
		}
	}

	protected float ColliderHeight()
	{
		var collisionShape = TriggerArea
			.FindChild("CollisionShape3D") as CollisionShape3D;

		return collisionShape.Shape switch {
			BoxShape3D => (collisionShape.Shape as BoxShape3D).Size.Y,
			CapsuleShape3D => (collisionShape.Shape as CapsuleShape3D).Height,
			CylinderShape3D => (collisionShape.Shape as CylinderShape3D).Height,
			_ => 0
		};
	}

	protected Vector2 InventoryPosition(Control uiInstance)
	{
		if (uiInstance is null) {
			return Vector2.Zero;
		}

		var world = GetTree().CurrentScene.FindChild("World") as World;
		var subViewportContainer = world.Viewport.GetParent<SubViewportContainer>();
		Vector2 scale = subViewportContainer.Scale;
		Vector2 position = world.Viewport.GetCamera3D().UnprojectPosition(Position);

		position.X += uiInstance.Size.X * scale.X;
		position.Y -= uiInstance.Size.Y * scale.Y;

		return position;
	}

	protected virtual bool HasPower()
	{
		foreach (var area in TriggerArea.GetOverlappingAreas()) {
			if (area.GetParent().IsInGroup("PowerSource")) {
				return true;
			}
		}
		return false;
	}

	private void _onTriggerAreaBodyEntered(Node3D body)
	{
		if (body is not Controller) {
			return;
		}

		var controller = body as Controller;
		if (controller.CreatureData.Node is not Actor) {
			return;
		}

		ActorData.EquipmentInVicinity.Add(this);
		ActorData.FocusedEquipment = this;

		_indicatorInst = UseIndicator.Instantiate<Node3D>();
		_indicatorInst.Position = new Vector3(0, ColliderHeight(), 0);
		AddChild(_indicatorInst);
		CanUse = true;
	}

	private void _onTriggerAreaBodyExited(Node3D body)
	{
		ActorData.EquipmentInVicinity.Remove(this);
		CanUse = false;
		ActorData.FocusedEquipment = null;

		if (IsInstanceValid(_indicatorInst)) {
			_indicatorInst.QueueFree();
		}

		//if (QuickInventoryInstance.IsOpen) {
		//	QuickInventoryInstance.Close();
		//	QuickInventoryInstance.QueueFree();
		//}
	}

	// ----- Tools

	public void SetupMesh()
	{
		_renameMesh();

		if (IsInstanceValid(Mesh)) {
			var meshInst = (MeshInstance3D)Mesh;
			GD.PrintS(meshInst.Scale, TopDownScale);
			if (meshInst.Scale != TopDownScale) {
				meshInst.Scale = TopDownScale;
			}
			var mat = meshInst.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;
			mat.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
			mat.BlendMode = BaseMaterial3D.BlendModeEnum.PremultAlpha;
			mat.BacklightEnabled = true;
			mat.Backlight = MeshBackLightColor;
		}
	}

	private bool _renameMesh()
	{
		foreach (var child in GetChildren()) {
			if (child is MeshInstance3D) {
				child.Name = _meshNodeName;
				return true;
			}
		}
		return false;
	}
}
