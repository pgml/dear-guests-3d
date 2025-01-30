using Godot;

[Tool]
public partial class ScenePostImport : EditorScenePostImport
{
	public override GodotObject _PostImport(Node scene)
	{
		Iterate(scene);
		GD.Print("asdasd");
		return scene;
	}

	public void Iterate(Node node)
	{
		GD.PrintRich(node.Name);
		if (node is not null) {
			//AnimatableBody3D animatableBody = new();
			//node.ReplaceBy(animatableBody, true);

			node.Name = "modified" + node.Name;
			foreach (var child in node.GetChildren()) {
				Iterate(child);
			}
		}
	}
}
