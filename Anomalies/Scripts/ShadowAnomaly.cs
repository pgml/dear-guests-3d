using Godot;
using System.Collections.Generic;

public partial class ShadowAnomaly : Anomaly
{
	[Export]
	public uint DefaultVisualLayer { get; set; } = 1;

	[Export]
	public uint AnomalyVisualLayer { get; set; } = new();

	[Export]
	public Area3D AnomalyArea { get; set; }

	[Export]
	public MeshInstance3D AnomalySphere { get; set; }

	private ShaderMaterial _shaderMaterial;

	public async override void _Ready()
	{
		base._Ready();

		AnomalyArea.BodyEntered += _onBodyEntered;
		AnomalyArea.BodyExited += _onBodyExited;

		_shaderMaterial = GD.Load<ShaderMaterial>(Resources.AnomalyShadowMaterial);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		Scene scene = GetTree().CurrentScene as Scene;
		scene.SceneLoaded += _onSceneLoaded;

		//foreach (var node in _findVisualInstances(this)) {
		//	if (node is VisualInstance3D child) {
		//		GD.PrintS(child.Name);
		//		//child.Layers = AnomalyVisualLayer;
		//	}
		//}
	}

	public override void _Process(double delta)
	{
		var shaderMaterial = AnomalySphere.MaterialOverride as ShaderMaterial;
		shaderMaterial.SetShaderParameter("hit_position", ActorData.Parent.Position);
		shaderMaterial.SetShaderParameter("effect_enabled", true);

		GD.PrintS(
			shaderMaterial.GetShaderParameter("hit_position"),
			shaderMaterial.GetShaderParameter("effect_enabled")
		);
	}

	private void _onSceneLoaded()
	{
		CallDeferred("_prepareShadows");
	}

	private async void _prepareShadows()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		foreach (var body in AnomalyArea.GetOverlappingBodies()) {
			if (body.GetParent() is MeshInstance3D mesh) {
				var bodyRoot = body.GetParent().GetParent<Node3D>();

				mesh.Layers = DefaultVisualLayer;
				mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
				var baseMaterial = mesh.GetActiveMaterial(0) as BaseMaterial3D;
				var albedo = baseMaterial.AlbedoTexture;
				//var shaderMaterial = GD.Load<ShaderMaterial>(Resources.AnomalyShadowMaterial);
				var shaderMaterial = ResourceLoader.Load<ShaderMaterial>(Resources.AnomalyShadowMaterial);
				var mat = shaderMaterial.Duplicate() as ShaderMaterial;
				mat.SetShaderParameter("albedo_texture", albedo);

				var meshClone = mesh.Duplicate() as MeshInstance3D;

				meshClone.Layers = AnomalyVisualLayer;
				meshClone.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
				meshClone.MaterialOverride = mat;

				bodyRoot.AddChild(meshClone);
			}
		}
	}

	private void _anomalyEntered(Node body)
	{
		foreach (var node in _findVisualInstances(body)) {
			if (node is VisualInstance3D child) {
				child.Layers = AnomalyVisualLayer;
			}
		}
	}

	private void _anomalyExited(Node body)
	{
		foreach (var node in _findVisualInstances(body)) {
			if (node is VisualInstance3D child) {
				child.Layers = DefaultVisualLayer;
			}
		}
	}

	private List<VisualInstance3D> _findVisualInstances(Node node)
	{
		var visualInstances = new List<VisualInstance3D>();
		foreach (var child in node.GetChildren()) {
			if (child is VisualInstance3D visualInstance) {
				visualInstances.Add(visualInstance);
			}
			visualInstances.AddRange(_findVisualInstances(child));
		}
		return visualInstances;
	}

	private void _onBodyEntered(Node body)
	{
		_anomalyEntered(body);
	}

	private void _onBodyExited(Node body)
	{
		_anomalyExited(body);
	}
}
