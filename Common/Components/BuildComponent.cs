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
			PrePlace(_itemResource, delta);
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
					SpawnItem();
					IsPlacing = true;
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

		MeshInstance3D mesh = _getMesh(_itemInstance);
		GD.PrintS(mesh.GetAabb());
		ShapeCast3D ray = new() {
			MaxResults = 8,
			Shape = new SphereShape3D() { Radius = 1 },
			Name = "DetectionRay",
			TargetPosition = Vector3.Zero,
		};
		_itemInstance.AddChild(ray);
	}

	private RayCast3D _createRayCast(Vector3 position)
	{
		MeshInstance3D mesh = _getMesh(_itemInstance);
		GD.PrintS(mesh.GetAabb());
		RayCast3D ray = new() {
			Name = "DetectionRay",
			TargetPosition = _itemInstance.Position,
		};

		return ray;
	}

	public void PrePlace(EquipmentResource equipment, double delta)
	{
		if (IsInstanceValid(_itemInstance)) {
			MeshInstance3D mesh = _getMesh(_itemInstance);
			Vector3 meshSize = mesh.GetAabb().Size;
			float maxMeshLength = Mathf.Max(meshSize.X, meshSize.Z);
			float minMeshLength = Mathf.Min(meshSize.X, meshSize.Z);

			// _itemInstance.FindChild() doesn't work for whatever reason
			// so the stupid way it is
			ShapeCast3D ray = new();
			foreach (var child in _itemInstance.GetChildren()) {
				if (child is ShapeCast3D) {
					ray = child as ShapeCast3D;
					break;
				}
			}

			Vector3 rayPos = ActorData.FacingDirection * maxMeshLength;
			rayPos.Y = 1.1f;
			ray.Position = rayPos;
			Transform3D position = Transform3D.Identity;

			GD.PrintS(position.Origin);
			if (ray.IsColliding()) {
				StaticBody3D collider = new();
				for (var i = 0; i <= ray.GetCollisionCount(); i++) {
					if (ray.GetCollider(i) is not null) {
						collider = ray.GetCollider(i) as StaticBody3D;
						break;
					}
				}

				if (collider is not null && collider.GetParent() is MeshInstance3D) {
					position = GetSnapPosition(
						_getMesh(_itemInstance),
						collider.GetParent<MeshInstance3D>()
					);
				}
			}
			else {
				Vector3 facingDirection = CreatureData.FacingDirection;
				Vector3 forward = GlobalTransform.Origin + facingDirection * 2.5f;
				forward.Z += minMeshLength / 2;
				position = new Transform3D(GlobalTransform.Basis, forward);
			}
			position.Origin.Y = (float)Controller.DistanceToFloor();
			GD.PrintS(position.Origin);

			_itemInstance.Position = position.Origin;
			//GD.PrintS(snapPosition == Transform3D.Identity);
		}
	}

	public Transform3D GetSnapPosition(
		MeshInstance3D snapObject,
		MeshInstance3D snapToObject,
		float margin = 0.5f
	) {
		var itemAabb = snapObject.GetAabb();
		Vector3 itemSize = itemAabb.Size;
		Vector3 itemPos = snapObject.GlobalTransform.Origin;

		var bodyAabb = snapToObject.GetAabb();
		Vector3 bodySize = bodyAabb.Size;
		Vector3 bodyPos = snapToObject.GlobalTransform.Origin;

		Vector3 direction = (itemPos - bodyPos).Normalized();
		Vector3 snapPosition = Vector3.Zero;

		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Z)) {
			snapPosition.X = Mathf.Sign(direction.X) * (bodySize.X / 2 + itemSize.X / 2 + margin);
		}
		else {
			snapPosition.Z = Mathf.Sign(direction.Z) * (bodySize.Z / 2 + itemSize.Z / 2 + margin);
		}

		//snapPosition.Y = (bodySize.Y - itemSize.Y) / 2;
		snapPosition.Y = (float)Controller.DistanceToFloor();

		return new Transform3D(
			_itemInstance.GlobalTransform.Basis,
			bodyPos + snapPosition
		);
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
