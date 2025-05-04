using Godot;

public partial class Mimic : Node3D
{
	protected CreatureData ActorData;

	private ObjectDetectionComponent _objDetection;

	public async override void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		ActorData = GD.Load<CreatureData>(Resources.ActorData);
	}

	public override void _Process(double delta)
	{
		GD.PrintS(ActorData);
		if (ActorData is CreatureData ad) {
			if (_objDetection is null) {
				_objDetection = ad.ObjectDetectionComponent;
			}

			_objDetection.HighlightHovered = true;
			var hoveredObject = _objDetection.HoveredObject();
			GD.PrintS(hoveredObject);
		}
	}
}
