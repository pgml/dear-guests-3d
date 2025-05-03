using Godot;
using System.Collections.Generic;

public partial class ObjectDetectionComponent : Component
{
	[Export]
	public uint HoverCollisionMask { get; set; }

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
		if (CreatureData is not null && IsInstanceValid(WorldData.World)) {
			HoveredNode = HoveredObject();

			if (HoveredNode is Node3D node && HighlightHovered) {
				HighlightNode(node);
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
		var camera = WorldData.Camera;

		foreach (var offset in offsets)
		{
			Vector2 screenPos = WorldData.World.Viewport.GetMousePosition() + offset;
			Vector3 rayOrigin = camera.ProjectRayOrigin(screenPos);
			Vector3 rayDir = camera.ProjectRayNormal(screenPos);
			Vector3 rayEnd = rayOrigin + rayDir * 1000f;

			var query = new PhysicsRayQueryParameters3D
			{
				From = rayOrigin,
				To = rayEnd,
				CollisionMask = HoverCollisionMask,
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

	public void HighlightNode(Node3D node)
	{
		var sprite = node.FindChild("Sprite3D") as Sprite3D;
		if (sprite is not null) {
			var mat = GD.Load<ShaderMaterial>(Resources.ItemCanvasOutline);
			mat.SetShaderParameter("sprite_texture", sprite.Texture);
			sprite.MaterialOverride = mat;
		}

		if (node is StaticBody3D) {
			var mesh = node.GetParent() as MeshInstance3D;
			var mat = GD.Load<ShaderMaterial>(Resources.ItemSpatialOutline);
			mesh.MaterialOverlay = mat;
		}
	}

	private void _resetHoveredNodes()
	{
		HighlightHovered = false;

		foreach (var node in _hoveredNodes) {
			if (node is null || !IsInstanceValid(node)) {
				continue;
			}

			var sprite = node.FindChild("Sprite3D") as Sprite3D;
			if (sprite is not null) {
				sprite.MaterialOverride = null;
			}

			if (node is StaticBody3D) {
				var mesh = node.GetParent() as MeshInstance3D;
				mesh.MaterialOverlay = null;
			}
		}

		_hoveredNodes = new();
	}
}
