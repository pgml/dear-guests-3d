using Godot;
using System.Collections.Generic;

public partial class BuildComponent : Component
{

	public PackedScene UiBuildMode { get {
		return GD.Load<PackedScene>(Resources.UiBuildMode);
	}}

	public UiBuildMode UiBuildModeInstance { get; private set; }
	public TreeItem SelectedItem { get; private set; }
	public Area3D ProximityCheck { get {
		return FindChild("ProximityCheck") as Area3D;
	}}

	public bool RemoveMode { get; private set; } = false;
	public bool IsActive { get; private set; } = false;
	public bool IsPlacing { get; private set; } = false;

	private Equipment _equipmentInFocus = null;
	private Equipment _itemInstance = null;
	private EquipmentResource _itemResource = null;
	private List<Equipment> _allFocusedEquipment = new();

	public override void  _Ready()
	{
		base._Ready();
	}

	public override void _Process(double delta)
	{
		if (!IsInstanceValid(UiBuildModeInstance)) {
			return;
		}

		if (IsActive && IsPlacing) {
			PrePlace(_itemResource);
		}

		if (RemoveMode) {
			if (ActorData.FocusedEquipment is Equipment equipment) {
				equipment.CanUse = false;
				_makeGhost(equipment);
				_equipmentInFocus = equipment;
				if (!_allFocusedEquipment.Contains(_equipmentInFocus)) {
					_allFocusedEquipment.Add(_equipmentInFocus);
				}
			}
			else {
				_unfocusAll();
			}
		}
		else {
			_unfocusAll();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionReleased("action_build")
			&& !ActorData.IsAnyUiPanelOpen()
			&& !IsActive
		) {
			EnterBuildMode();
		}
		else if (@event.IsActionReleased("action_build")
			&& IsActive
		) {
			ExitBuildMode();
		}

		if (!RemoveMode) {
			if (@event.IsActionReleased("action_use")
				&& IsInstanceValid(UiBuildModeInstance)
			) {
				if (!IsPlacing)	{
					SelectedItem = UiBuildModeInstance.SelectedItem();
					_itemResource = (EquipmentResource)SelectedItem.GetMetadata(0);
					IsPlacing = true;
					SpawnItem();
				}
				else {
					Place();
				}
			}
		}

		if (@event.IsActionReleased("build_mode_remove")) {
			RemoveMode = !RemoveMode;
		}
	}

	public void SpawnItem()
	{
		_itemInstance = _itemResource.ItemScene.Instantiate<Equipment>();
		_itemInstance.Name = $"{_itemResource.Name} {GlobalPosition.ToString()}";
		//(_itemInstance.FindChild("CollisionShape3D", true) as CollisionShape3D).Disabled = true;
		GetTree().CurrentScene.AddChild(_itemInstance);
		_itemInstance.CanUse = false;
		_itemMesh().SetSurfaceOverrideMaterial(0, GetGhostMaterial(_itemMesh()));
	}

	public void PrePlace(EquipmentResource equipment)
	{
		Vector3 facingDirection = CreatureData.FacingDirection;
		Transform3D globalTransform = Controller.GlobalTransform;
		Vector3 forward = globalTransform.Origin + facingDirection * 2.5f;
		forward.Y = 0;
		if (IsInstanceValid(_itemInstance)) {
			_itemInstance.Position = forward;
		}
	}

	public void Place()
	{
		var material = _itemMaterial();
		material.AlbedoColor = new Color(1, 1, 1);
		_itemMesh().SetSurfaceOverrideMaterial(0, material);

		IsPlacing = false;
		_itemInstance = null;
		_itemResource = null;
	}

	public void EnterBuildMode()
	{
		UiBuildModeInstance = UiBuildMode.Instantiate<UiBuildMode>();
		GetNode("/root/MainUI").AddChild(UiBuildModeInstance);
		//Vector2 position = BuildMenuPosition();
		UiBuildModeInstance.Open();
		IsActive = true;
		ActorData.IsBuildMoveActive = true;
	}

	public void ExitBuildMode()
	{
		UiBuildModeInstance.QueueFree();
		IsActive = false;
		ActorData.IsBuildMoveActive = false;
		RemoveMode = false;
		IsPlacing = false;
		if (IsInstanceValid(_itemInstance)) {
			_itemInstance.QueueFree();
			_itemResource = null;
		}
	}

	public BaseMaterial3D GetGhostMaterial(MeshInstance3D mesh)
	{
		if (!IsInstanceValid(mesh)) {
			mesh = _itemMesh();
		}
		var material = _itemMaterial(mesh);
		material.AlbedoColor = Color.FromString("ffffff82", default);
		return material;
	}

	private MeshInstance3D _itemMesh()
	{
		return _getMesh(_itemInstance);
	}

	private BaseMaterial3D _itemMaterial(MeshInstance3D mesh = null)
	{
		if (!IsInstanceValid(mesh)) {
			mesh = _itemMesh();
		}
		return mesh.GetActiveMaterial(0).Duplicate() as BaseMaterial3D;
	}

	private MeshInstance3D _getMesh(Node item)
	{
		if (!IsInstanceValid(item)) {
			return null;
		}
		return item.FindChild("Mesh") as MeshInstance3D;
	}

	private void _makeGhost(Equipment equipment)
	{
		var mesh = _getMesh(equipment);
		mesh.SetSurfaceOverrideMaterial(0, GetGhostMaterial(mesh));
	}

	private void _unfocus(Equipment equipment)
	{
		if (equipment is null) {
			return;
		}

		var mesh = _getMesh(equipment);
		if (IsInstanceValid(mesh)) {
			var material = _itemMaterial(mesh);
			material.AlbedoColor = new Color(1, 1, 1);
			mesh.SetSurfaceOverrideMaterial(0, material);
		}
	}

	private void _unfocusAll()
	{
		foreach (var equipment in _allFocusedEquipment.ToArray()) {
			_unfocus(equipment);
			_allFocusedEquipment.Remove(equipment);
		}
	}
}
