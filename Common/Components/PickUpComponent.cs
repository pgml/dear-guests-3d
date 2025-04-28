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

	public override void _Ready()
	{
		base._Ready();

		PickUpArea.BodyEntered += _onBodyEntered;
		PickUpArea.BodyExited += _onBodyExited;
	}

	public override void _Process(double delta)
	{
		_hoveredObj = _hoveredObject();
		if (_hoveredObj.Node is RigidBody3D node && _hoveredObj.InVicinity) {
			ActorData.CanPickUp = true;
			var hoverMaterial = GD.Load<ShaderMaterial>(Resources.ItemPickupHover);
			(node.FindChild("Sprite3D") as Sprite3D).MaterialOverride = hoverMaterial;
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
		// increase hover detection slightly so that it's a little bit
		// easier to grab smaller objects
		Vector2[] offsets = new Vector2[]
		{
			Vector2.Zero,
			new Vector2(2, 0),
			new Vector2(-2, 0),
			new Vector2(0, 2),
			new Vector2(0, -2),
		};

		var spaceState = GetWorld3D().DirectSpaceState;
		var camera = World.Viewport.GetCamera3D();

		foreach (var offset in offsets)
		{
			Vector2 screenPos = World.Viewport.GetMousePosition() + offset;
			Vector3 rayOrigin = camera.ProjectRayOrigin(screenPos);
			Vector3 rayDir = camera.ProjectRayNormal(screenPos);
			Vector3 rayEnd = rayOrigin + rayDir * 1000f;

			var query = new PhysicsRayQueryParameters3D
			{
				From = rayOrigin,
				To = rayEnd,
				CollisionMask = 512,
			};

			var result = spaceState.IntersectRay(query);

			if (result.Count > 0) {
				var hitNode = (Node3D)result["collider"];

				if (hitNode is RigidBody3D) {
					var obj = hitNode as RigidBody3D;
					bool inVicinity = _bodiesInVicinity.Contains(obj);

					return new PickUpObject {
					 Node = obj,
					 InVicinity = inVicinity
					};
				}
			}
		}

		return new PickUpObject {
			Node = null,
			InVicinity = false
		};
	}

	private void _resetHoveredObjects()
	{
		foreach (var body in _bodiesInVicinity) {
			if (body is null) {
				continue;
			}
			(body.FindChild("Sprite3D") as Sprite3D).MaterialOverride = null;
		}

		ActorData.CanPickUp = false;
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
