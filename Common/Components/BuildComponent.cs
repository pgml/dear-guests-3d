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
	private PhysicsBody3D _activeCollider = null;
	// Since the current build object has 4 shape cast surrounding at each
	// side we store and disable the one that is on the characters side
	private ShapeCast3D _disabledColliderShapeCasts = null;
	private bool _isObjectSnapped = false;

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

		_itemInstance.AddChild(_createShapeCast(Vector3.Forward, "forward"));
		_itemInstance.AddChild(_createShapeCast(Vector3.Right, "right"));
		_itemInstance.AddChild(_createShapeCast(Vector3.Back, "back"));
		_itemInstance.AddChild(_createShapeCast(Vector3.Left, "left"));
	}

	public void PrePlace(EquipmentResource equipment, double delta)
	{
		if (IsInstanceValid(_itemInstance)) {
			MeshInstance3D mesh = _getMesh(_itemInstance);
			Vector3 meshSize = mesh.GetAabb().Size;
			float maxMeshLength = Mathf.Max(meshSize.X, meshSize.Z);
			float minMeshLength = Mathf.Min(meshSize.X, meshSize.Z);
			ShapeCast3D ray = new();

			foreach (var child in _itemInstance.GetChildren()) {
				if (child is not ShapeCast3D) {
					continue;
				}

				ray = child as ShapeCast3D;
				ray.Enabled = _disabledColliderShapeCasts != ray;

				if (ray.IsColliding()) {
					var col = ray.GetCollider(0);
					if (col is StaticBody3D || col is RigidBody3D) {
						_activeCollider = col as PhysicsBody3D;
						break;
					}
					else {
						_disabledColliderShapeCasts = ray;
						_activeCollider = null;
					}
				}
			}

			Vector3 facingDirection = CreatureData.FacingDirection;
			Vector3 forward = GlobalTransform.Origin + facingDirection * 2.5f;
			forward.Z += minMeshLength / 2;
			Transform3D position = new Transform3D(GlobalTransform.Basis, forward);

			bool canSnap = false;
			var testMotion = Controller.TestMotion(ActorData.FacingDirection * 4);
			if (testMotion.IsColliding) {
				var col = testMotion.Collider<PhysicsBody3D>();
				canSnap = col == mesh.GetChild(0);
			}

			if (canSnap && _activeCollider is not null) {
				position = GetSnapPosition(
					_getMesh(_itemInstance),
					_activeCollider.GetParent<MeshInstance3D>()
				);
				_isObjectSnapped = true;
			}
			else {
				_isObjectSnapped = false;
			}

			position.Origin.Y = (float)Controller.DistanceToFloor();
			_itemInstance.Position = position.Origin;
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

		// remove snap detection shapecasts
		foreach (var child in _itemInstance.GetChildren()) {
			if (child is ShapeCast3D) {
				child.QueueFree();
			}
		}
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

	private ShapeCast3D _createShapeCast(Vector3 position, string name = "")
	{
		MeshInstance3D mesh = _getMesh(_itemInstance);
		Vector3 meshSize = mesh.GetAabb().Size;
		float maxMeshLength = Mathf.Max(meshSize.X, meshSize.Z);

		Vector3 pos = position * maxMeshLength;
		pos.Y = 1.1f;

		ShapeCast3D ray = new() {
			Name = name,
			MaxResults = 1,
			Shape = new SphereShape3D() { Radius = 0.5f },
			TargetPosition = Vector3.Zero,
			Position = pos,
		};
		return ray;
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
