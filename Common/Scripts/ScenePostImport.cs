using Godot;

[Tool]
public partial class ScenePostImport : EditorScenePostImport
{
	public override GodotObject _PostImport(Node scene)
	{
		Iterate(scene);
		return scene;
	}

	public void Iterate(Node node)
	{
		if (node is not null) {
			//AnimatableBody3D animatableBody = new();
			//node.ReplaceBy(animatableBody, true);

			//node.Name = "modified" + node.Name;
			if (node is MeshInstance3D mesh) {
				if (mesh.Name.ToString().Contains("-hidden")) {
					mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
					//var mat = meshInst.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;
					//mat.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
					//mat.BlendMode = BaseMaterial3D.BlendModeEnum.PremultAlpha;
				}

				var collisionShape = (CollisionShape3D)mesh.FindChild("CollisionShape3D");
				var shape = (collisionShape.Shape as ConcavePolygonShape3D);
				shape.BackfaceCollision = true;
				collisionShape.Shape = shape;
				node = mesh;
			}

			foreach (var child in node.GetChildren()) {
				Iterate(child);
			}
		}
	}
}
