using Godot;
using System.Collections.Generic;

public enum HighlightMode {
	Outline,
	Translucent,
	Coloured,
	ColouredTranslucent
}

public partial class ObjectDetectionComponent : Component
{
	[Export(PropertyHint.Flags)]
	public Layer HoverCollisionMask { get; set; }

	public bool HighlightHovered { get; set; } = false;
	public HighlightMode HighlightMode { get; set; } = HighlightMode.Outline;
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

			var query = new PhysicsRayQueryParameters3D {
				From = rayOrigin,
				To = rayEnd,
				CollisionMask = (uint)HoverCollisionMask
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
		if (node.FindChild("Sprite3D") is Sprite3D sprite) {
			var mat = GD.Load<ShaderMaterial>(Resources.ItemCanvasOutline);
			mat.SetShaderParameter("sprite_texture", sprite.Texture);
			sprite.MaterialOverride = mat;
			if (HighlightMode == HighlightMode.Translucent) {
				sprite.Transparency = 0.5f;
			}
		}

		if (node.FindChild("Mesh") is MeshInstance3D mesh) {
			var surfaceMat = mesh.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;

			if (HighlightMode == HighlightMode.Outline) {
				var mat = GD.Load<ShaderMaterial>(Resources.ItemCanvasOutline);
				mat.SetShaderParameter("sprite_texture", surfaceMat.AlbedoTexture);
				mesh.MaterialOverlay = mat;
			}

			surfaceMat.AlbedoColor = HighlightMode switch {
				HighlightMode.Translucent => Color.FromString("ffffff99", default),
				HighlightMode.ColouredTranslucent => Color.FromString("ddc40099", default),
				_ => Color.FromString("ffffff", default)
			};
		}
	}

	private void _resetHoveredNodes()
	{
		foreach (var node in _hoveredNodes) {
			if (node is null || !IsInstanceValid(node)) {
				continue;
			}

			if (node.FindChild("Sprite3D") is Sprite3D sprite) {
				sprite.MaterialOverride = null;
				if (HighlightMode == HighlightMode.Translucent) {
					sprite.Transparency = 0;
				}
			}

			if (node.FindChild("Mesh") is MeshInstance3D mesh) {
				var surfaceMat = mesh.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;
				surfaceMat.AlbedoColor = Color.FromString("ffffff", default);
				mesh.MaterialOverlay = null;
			}
		}

		HighlightHovered = false;
		_hoveredNodes = new();
	}
}
