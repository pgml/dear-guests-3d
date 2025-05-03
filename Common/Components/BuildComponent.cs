using Godot;
using System.Collections.Generic;

public enum BuildMode {
	Place,
	Move,
	PickUp
}

public partial class BuildComponent : Component
{
	public PackedScene UiBuildMode { get {
		return GD.Load<PackedScene>(Resources.UiBuildMode);
	}}

	public UiBuildMode UiBuildModeInstance { get; private set; }

	/// <summary>
	/// The currently selected item from the build menu item list
	/// </summary>
	public TreeItem SelectedItem { get; private set; }

	public Area3D ProximityCheck { get {
		return FindChild("ProximityCheck") as Area3D;
	}}

	public BuildMode CurrentMode { get; private set; } = BuildMode.Place;
	public bool IsBuildModeActive { get; private set; } = false;
	public bool IsMovingItem { get; private set; } = false;
	public bool IsSnappingEnabled { get; private set; } = false;

	/// <summary>
	/// The equipment that is currently focused in either
	/// move or remove mode
	/// </summary>
	private Equipment _equipmentInFocus = null;

	/// <summary>
	/// The item scene instance of the moved or currently in placement
	/// equipment
	/// </summary>
	private Equipment _itemInstance = null;
	private EquipmentResource _itemResource = null;

	/// <summary>
	/// All objects that are somehow currently focused
	/// </summary>
	private List<Equipment> _allFocusedEquipment = new();

	/// <summary>
	/// The collider that is currently colliding with a
	/// neighbouring object in snap mode
	/// </summary>
	private PhysicsBody3D _activeCollider = null;
	private Node3D _itemCollidingWith = null;

	// Since the current build object has 4 shape cast surrounding at each
	// side we store and disable the one that is on the characters side
	private ShapeCast3D _disabledColliderShapeCasts = null;
	private bool _isObjectSnapped = false;
	private bool _isItemPlaceable = true;

	/// <summary>
	/// indicates whether the item that is being moved came from the
	/// inventory or was already placed on the map
	/// </summary>
	private bool _isFromInventory = false;

	public override void  _Ready()
	{
		base._Ready();
	}

	public override void _Process(double delta)
	{
		if (!IsInstanceValid(UiBuildModeInstance)) {
			return;
		}

		if (IsBuildModeActive && IsMovingItem) {
			MoveItem();
		}

		_unfocusAll();

		Equipment focusedEquipment = null;
		if (ActorData.FocusedEquipment is Equipment) {
			focusedEquipment = ActorData.FocusedEquipment;
			_itemInstance = focusedEquipment;
			_itemResource = _equipmentResource();
		}

		if ((CurrentMode == BuildMode.Move ||
			CurrentMode == BuildMode.PickUp) &&
			focusedEquipment is not null)
		{
			focusedEquipment.CanUse = false;
			_makeGhost(focusedEquipment);
			_equipmentInFocus = focusedEquipment;

			if (!IsInstanceValid(_itemInstance)) {
				_itemInstance = _equipmentInFocus;
			}

			// in some occasions several objects get focused which we need to
			// store in order to be able to remove the focus of all objects
			if (!_allFocusedEquipment.Contains(_equipmentInFocus)) {
				_allFocusedEquipment.Add(_equipmentInFocus);
			}
		}

		_connectPlaceDetectionSignals();
	}

	public override void _Input(InputEvent @event)
	{
		var uiInstance = UiBuildModeInstance;

		if (@event.IsActionReleased("action_build") &&
			!ActorData.IsAnyUiPanelOpen() &&
			!IsBuildModeActive)
		{
			EnterBuildMode();
		}
		else if ((@event.IsActionReleased("action_build") ||
			@event.IsActionReleased(DGInputMap.ActionExit)) &&
			IsBuildModeActive)
		{
			uiInstance.ExitBuildModeButton.SetPressedNoSignal(false);
			ExitBuildMode();
		}

		if (!IsBuildModeActive || !IsInstanceValid(uiInstance)) {
			return;
		}

		// make ui buttons react to keypresses
		if (@event.IsActionPressed(DGInputMap.ActionExit)) {
			uiInstance.ExitBuildModeButton.SetPressedNoSignal(true);
		}

		if (@event.IsActionPressed("build_mode_switch_mode")) {
			uiInstance.SwitchModeButton.SetPressedNoSignal(true);
		}

		if (@event.IsActionReleased("build_mode_switch_mode")) {
			_cycleBuildModes();
			uiInstance.CurrentModeLabel.Text = CurrentMode.ToString();
			uiInstance.SwitchModeButton.SetPressedNoSignal(false);
			AudioInstance.PlayUiSound(AudioLibrary.InventoryBrowse);
		}

		if (@event.IsActionReleased("build_mode_enable_snapping")) {
			IsSnappingEnabled = !IsSnappingEnabled;
			uiInstance.EnableSnappingButton.SetPressedNoSignal(IsSnappingEnabled);
			AudioInstance.PlayUiSound(AudioLibrary.MiscClick);
		}

		if (@event.IsActionReleased("action_use")) {
			AudioInstance.PlayUiSound(AudioLibrary.MiscPlace);
			if (CurrentMode == BuildMode.Place) {
				if (!IsMovingItem) {
					IsMovingItem = true;
					SelectedItem = uiInstance.SelectedItem();

					if (SelectedItem is not null) {
						_itemResource = _equipmentResource();
						SpawnItem();
						_createSnapShapeCasts();
					}
				}
				else {
					PlaceItem();
				}
			}
			else if (CurrentMode == BuildMode.Move) {
				if (!IsMovingItem &&
					ActorData.FocusedEquipment is Equipment focusedEquipment &&
					IsInstanceValid(focusedEquipment))
				{
					_itemInstance = focusedEquipment;
					IsMovingItem = true;
					_createSnapShapeCasts();
				}
				else {
					PlaceItem();
				}
			}
			else if (CurrentMode == BuildMode.PickUp) {
				PickUpItem();
			}
		}
	}

	/// <summary>
	/// Instantiates an item from a resource path directly into move mode
	/// </summary>
	public void SpawnItem()
	{
		_itemInstance = _itemResource.ItemScene.Instantiate<Equipment>();
		_itemInstance.Name = $"{_itemResource.Name} {GlobalPosition.ToString()}";
		//(_itemInstance.FindChild("CollisionShape3D", true) as CollisionShape3D).Disabled = true;
		GetTree().CurrentScene.AddChild(_itemInstance);
		_itemInstance.CanUse = false;
		_isFromInventory = true;
	}

	public void MoveItem()
	{
		if (IsInstanceValid(_itemInstance)) {
			_makeGhost(_itemInstance);
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

			_isItemPlaceable = true;
			if (_itemCollidingWith is not null && !_isObjectSnapped) {
				_isItemPlaceable = false;
			}
			else {
				_isObjectSnapped = false;
			}

			// always put moving object in front of character
			Vector3 facingDirection = CreatureData.FacingDirection;
			Vector3 forward = GlobalTransform.Origin + facingDirection * 2.5f;
			forward.Z += minMeshLength / 2;
			Transform3D position = new Transform3D(GlobalTransform.Basis, forward);

			// adjust position snapping is enabled and object can be snapped
			if (IsSnappingEnabled) {
				bool canSnap = false;
				var testMotion = Controller.TestMotion(ActorData.FacingDirection * 4);
				if (testMotion.IsColliding) {
					var col = testMotion.Collider<PhysicsBody3D>();
					canSnap = col == mesh.GetChild(0);
				}

				if (canSnap && _activeCollider is not null) {
					position = DeterminSnapPosition(
						_getMesh(_itemInstance),
						_activeCollider.GetParent<MeshInstance3D>()
					);
					_isObjectSnapped = true;
				}
				else {
					_isObjectSnapped = false;
				}
			}

			position.Origin.Y = (float)Controller.DistanceToFloor();
			_itemInstance.Position = position.Origin;
		}
	}

	public bool PlaceItem()
	{
		if (!IsInstanceValid(_itemInstance)) {
			GD.PushWarning($"[BUILDMODE] Cannot place item: _itemInstance is not set.");
			return false;
		}

		if (!_isItemPlaceable) {
			GD.PushWarning($"[BUILDMODE] {_itemInstance.Name} cannot be placed here.");
			AudioInstance.PlayUiSound(AudioLibrary.MiscDenied);
			return false;
		}

		var material = _itemMaterial();
		material.AlbedoColor = new Color(1, 1, 1);
		_itemMeshInstance().SetSurfaceOverrideMaterial(0, material);

		// remove snap detection shapecasts
		foreach (var child in _itemInstance.GetChildren()) {
			if (child is ShapeCast3D) {
				child.QueueFree();
			}
		}

		// remove from inventory when in place mode
		if (CurrentMode == BuildMode.Place) {
			int itemResourceIndex = ActorInventory.GetItemResourceIndex(_itemResource);
			ActorInventory.RemoveOneItem(itemResourceIndex);
		}

		IsMovingItem = false;
		_itemInstance = null;
		_itemResource = null;
		_activeCollider = null;
		_disabledColliderShapeCasts = null;
		_itemCollidingWith = null;
		_isFromInventory = false;

		UiBuildModeInstance.SelectFirstRow();
		return true;
	}

	public bool PickUpItem()
	{
		if (!IsInstanceValid(ActorData.FocusedEquipment)) {
			return false;
		}

		// @todo: implement removal confirmation
		_itemInstance = ActorData.FocusedEquipment;
		_itemInstance.QueueFree();
		ActorInventory.AddItem(_itemResource, 1);
		return true;
	}

	public void EnterBuildMode()
	{
		CurrentMode = BuildMode.Place;

		UiBuildModeInstance = UiBuildMode.Instantiate<UiBuildMode>();
		GetNode("/root/MainUI").AddChild(UiBuildModeInstance);
		UiBuildModeInstance.Open();
		IsBuildModeActive = true;
		ActorData.IsBuildMoveActive = true;
		AudioInstance.PlayUiSound(AudioLibrary.MiscBleep);
	}

	public void ExitBuildMode()
	{
		UiBuildModeInstance.QueueFree();
		IsBuildModeActive = false;
		ActorData.IsBuildMoveActive = false;
		if (IsMovingItem) {
			_removeItemInstance();
			IsMovingItem = false;
		}
		_unfocusAll();
		AudioInstance.PlayUiSound(AudioLibrary.MiscBleep);
	}

	public Transform3D DeterminSnapPosition(
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

		snapPosition.Y = (float)Controller.DistanceToFloor();

		return new Transform3D(
			_itemInstance.GlobalTransform.Basis,
			bodyPos + snapPosition
		);
	}

	/// <summary>
	/// Makes the currently focused object translucent when in move or remove mode<br />
	/// In move mode the translucency has a red tint
	/// </summary>
	private void _makeGhost(Equipment equipment)
	{
		var mesh = _getMesh(equipment);
		if (!IsInstanceValid(mesh)) {
			mesh = _itemMeshInstance();
		}

		var material = _itemMaterial(mesh);
		string ghostColour = _isItemPlaceable ? "ffffff82" : "ffb49782";

		//if (CurrentMode == BuildMode.Place || CurrentMode == BuildMode.Move) {
		//	ghostColour = _itemCanBePlaced ? "7df00077": "ffb49782";
		//}

		material.AlbedoColor = Color.FromString(ghostColour, default);
		mesh.SetSurfaceOverrideMaterial(0, material);
	}

	private void _cycleBuildModes()
	{
		CurrentMode = CurrentMode switch {
			BuildMode.Place => BuildMode.Move,
			BuildMode.Move => BuildMode.PickUp,
			BuildMode.PickUp => BuildMode.Place,
			_ => BuildMode.Place,
		};

		if (IsInstanceValid(_itemInstance) && IsMovingItem) {
			if (CurrentMode != BuildMode.Place) {
				IsMovingItem = false;
				_isObjectSnapped = false;
				_isItemPlaceable = true;
				_isFromInventory = false;
				_itemCollidingWith = null;
				_equipmentInFocus = null;
			}

			if (CurrentMode != BuildMode.PickUp) {
				_removeItemInstance();
			}

			_unfocus(_itemInstance);
		}

		if (CurrentMode == BuildMode.Place) {
			UiBuildModeInstance.SelectFirstRow();
		}
	}

	/// <summary>
	/// Creates four sphere casts on each side of the object which are used
	/// to detect if theres something it can snap to
	/// </summary>
	private void _createSnapShapeCasts()
	{
		if (!IsInstanceValid(_itemInstance)) {
			return;
		}

		_itemInstance.AddChild(_createShapeCast(Vector3.Forward, "forward"));
		_itemInstance.AddChild(_createShapeCast(Vector3.Right, "right"));
		_itemInstance.AddChild(_createShapeCast(Vector3.Back, "back"));
		_itemInstance.AddChild(_createShapeCast(Vector3.Left, "left"));
	}

	private ShapeCast3D _createShapeCast(Vector3 position, string name = "")
	{
		if (!IsInstanceValid(_itemInstance)) {
			return null;
		}

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

	private bool _removeItemInstance()
	{
		if (!IsInstanceValid(_itemInstance)) {
			return false;
		}

		_itemInstance.QueueFree();
		_itemResource = null;
		return true;
	}

	private MeshInstance3D _itemMeshInstance()
	{
		if (_itemInstance is null) {
			return null;
		}
		return _getMesh(_itemInstance);
	}

	private BaseMaterial3D _itemMaterial(MeshInstance3D mesh = null)
	{
		if (!IsInstanceValid(mesh)) {
			mesh = _itemMeshInstance();
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

	/// <summary>
	/// Unfocues currently focused equipment in either move or remove mode
	/// </summary>
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

	/// <summary>
	/// Unfocues all equipment focused in either move or remove mode
	/// </summary>
	private void _unfocusAll()
	{
		foreach (var equipment in _allFocusedEquipment.ToArray()) {
			_unfocus(equipment);
			_allFocusedEquipment.Remove(equipment);
		}
	}

	private EquipmentResource _equipmentResource()
	{
		EquipmentResource itemResource = null;

		if (IsInstanceValid(_itemInstance)) {
			itemResource = ItemResource.Get<EquipmentResource>(
				_itemInstance.ResourcePath,
				true
			);
		}
		else if (IsInstanceValid(SelectedItem)) {
			itemResource = (EquipmentResource)SelectedItem.GetMetadata(0);
		}

		return itemResource;
	}

	private void _connectPlaceDetectionSignals()
	{
		if ((CurrentMode == BuildMode.Place ||
			CurrentMode == BuildMode.Move) &&
			IsInstanceValid(_itemInstance))
		{
			var triggerArea = _itemInstance.FindChild("TriggerArea") as Area3D;

			bool isBodyEnteredConnected = triggerArea.IsConnected(
				"body_entered",
				Callable.From<Node3D>(_onTriggerAreaBodyEntered)
			);

			if (!isBodyEnteredConnected) {
				triggerArea.Connect(
					"body_entered",
					Callable.From<Node3D>(_onTriggerAreaBodyEntered)
				);
				//triggerArea.BodyEntered += _onTriggerAreaBodyEntered;
			}

			bool isBodyExitedConnected = triggerArea.IsConnected(
				"body_exited",
				Callable.From<Node3D>(_onTriggerAreaBodyExited)
			);

			if (!isBodyExitedConnected) {
				triggerArea.Connect(
					"body_exited",
					Callable.From<Node3D>(_onTriggerAreaBodyExited)
				);
			}
		}
	}

	private void _onTriggerAreaBodyEntered(Node3D body)
	{
		// Exclude self from being detected
		if (IsInstanceValid(_itemInstance)) {
			if (body.GetParent() == _getMesh(_itemInstance)) {
				return;
			}
		}
		_itemCollidingWith = body;
	}

	private void _onTriggerAreaBodyExited(Node3D body)
	{
		_itemCollidingWith = null;
	}
}
