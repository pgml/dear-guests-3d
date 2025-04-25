using Godot;
using System.Collections.Generic;

public partial class PickUpComponent : Component
{
	[Export]
	public Area3D PickUpArea { get; set; }

	public bool IsHoveringObject { get; set; } = false;

	private List<RigidBody3D> _bodiesInVicinity = new();

	public override void _Ready()
	{
		base._Ready();

		PickUpArea.BodyEntered += _onBodyEntered;
		PickUpArea.BodyExited += _onBodyExited;
	}

	public override void _Process(double delta)
	{
		if (_hoveredObject() is RigidBody3D obj && IsHoveringObject) {
			if (_bodiesInVicinity.Contains(obj)) {
				ActorData.CanPickUp = true;
				var hoverMaterial = GD.Load<ShaderMaterial>(Resources.ItemPickupHover);
				(obj.FindChild("Sprite3D") as Sprite3D).MaterialOverride = hoverMaterial;
			}
		}
		else {
			_resetHoveredObjects();
		}
	}

	private RigidBody3D _hoveredObject()
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
		var camera = GetViewport().GetCamera3D();

		foreach (var offset in offsets)
		{
			Vector2 screenPos = GetViewport().GetMousePosition() + offset;
			Vector3 rayOrigin = camera.ProjectRayOrigin(screenPos);
			Vector3 rayDir = camera.ProjectRayNormal(screenPos);
			Vector3 rayEnd = rayOrigin + rayDir * 1000f;

			var query = new PhysicsRayQueryParameters3D
			{
				From = rayOrigin,
				To = rayEnd,
				CollisionMask = 2,
			};

			var result = spaceState.IntersectRay(query);

			if (result.Count > 0)
			{
				var hitNode = (Node3D)result["collider"];

				if (hitNode is RigidBody3D) {
					IsHoveringObject = true;
					return (RigidBody3D)hitNode;
				}
			}
		}

		IsHoveringObject = false;
		return null;
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
		//GD.PrintS(body);
	}

	private void _onBodyExited(Node3D body)
	{
		_bodiesInVicinity.Remove(body as RigidBody3D);
		//GD.PrintS(body);
	}
}
