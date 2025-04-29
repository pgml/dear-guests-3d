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

	public async override void _Ready()
	{
		base._Ready();

		AnomalyArea.BodyEntered += _onBodyEntered;
		AnomalyArea.BodyExited += _onBodyExited;

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		//foreach (var node in _findVisualInstances(this)) {
		//	if (node is VisualInstance3D child) {
		//		child.Layers = AnomalyVisualLayer;
		//	}
		//}
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
