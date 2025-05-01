using Godot;
using System.Collections.Generic;

public partial class ObjectDetectionComponent : Component
{
	public bool HighlightHovered = false;
	public Node3D HoveredNode { get; set; }

	private List<Node3D> _hoveredNodes = new();

	public async override void _Ready()
	{
		base._Ready();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		if (CreatureData is CreatureData cd) {
			cd.ObjectDetectionComponent = this;
		}
	}

	public override void _Process(double delta)
	{
		if (CreatureData is not null) {
			HoveredNode = HoveredObject();

			if (HoveredNode is Node3D node && HighlightHovered) {
				var sprite = node.FindChild("Sprite3D") as Sprite3D;
				var hoverMaterial = GD.Load<ShaderMaterial>(Resources.ItemPickupHover);
				hoverMaterial.SetShaderParameter("sprite_texture", sprite.Texture);
				sprite.MaterialOverride = hoverMaterial;
			}
			else {
				_resetHoveredNodes();
			}
		}
	}

	/// <summary>
	/// Returns the object the mouse is currently hovering over.
	/// </summary>
	public Node3D HoveredObject()
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

				if (!_hoveredNodes.Contains(hitNode)) {
					_hoveredNodes.Add(hitNode);
				}

				return hitNode;
			}
		}

		return null;
	}

	private void _resetHoveredNodes()
	{
		HighlightHovered = false;

		foreach (var node in _hoveredNodes) {
			if (node is null) {
				continue;
			}
			(node.FindChild("Sprite3D") as Sprite3D).MaterialOverride = null;
		}
	}
}
