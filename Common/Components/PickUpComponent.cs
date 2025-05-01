using Godot;
using System.Collections.Generic;

struct PickUpObject
{
	public PhysicsBody3D Node { get; set; }
	public bool InVicinity { get; set; }
}

public partial class PickUpComponent : Component
{
	[Export]
	public Area3D PickUpArea { get; set; }

	private PickUpObject _hoveredObj = new();
	private List<RigidBody3D> _bodiesInVicinity = new();
	private ObjectDetectionComponent _objDetection;

	public override void _Ready()
	{
		base._Ready();

		PickUpArea.BodyEntered += _onBodyEntered;
		PickUpArea.BodyExited += _onBodyExited;
	}

	public override void _Process(double delta)
	{
		if (CreatureData is CreatureData cd && _objDetection is null) {
			_objDetection = cd.ObjectDetectionComponent;
		}

		_hoveredObj = _hoveredObject();
		if (_hoveredObj.Node is RigidBody3D node && _hoveredObj.InVicinity) {
			ActorData.CanPickUp = true;
			_objDetection.HighlightHovered = true;
		}
		else {
			_resetHoveredObjects();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.Pressed &&
			mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (_hoveredObj.Node is PhysicsObject obj && _hoveredObj.InVicinity) {
				ActorData.IsPickingUpItem = true;
				ItemResource itemResource = ItemResource.GetByUid(obj.ItemResourcePath);
				ActorData.Character<Actor>().Inventory.AddItem(itemResource, 1);
				obj.QueueFree();
			}
		}
		else {
			ActorData.IsPickingUpItem = false;
		}
	}

	/// <summary>
	/// Returns the object the mouse is currently hovering over.
	/// </summary>
	private PickUpObject _hoveredObject()
	{
		if (CreatureData is not null) {
			var hoveredObject = _objDetection.HoveredObject();

			if (hoveredObject is RigidBody3D obj) {
				bool inVicinity = _bodiesInVicinity.Contains(obj);

				return new PickUpObject {
					Node = obj,
					InVicinity = inVicinity
				};
			}
		}

		return new PickUpObject {
			Node = null,
			InVicinity = false
		};
	}

	private void _resetHoveredObjects()
	{
		ActorData.CanPickUp = false;
		ActorData.IsPickingUpItem = false;

		foreach (var body in _bodiesInVicinity) {
			if (body is null) {
				continue;
			}
			(body.FindChild("Sprite3D") as Sprite3D).MaterialOverride = null;
		}
	}

	private void _onBodyEntered(Node3D body)
	{
		_bodiesInVicinity.Add(body as RigidBody3D);
	}

	private void _onBodyExited(Node3D body)
	{
		_bodiesInVicinity.Remove(body as RigidBody3D);
	}
}
