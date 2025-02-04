using Godot;

public enum ReplicatorType
{
	Type1,
	Type2,
	Type3,
	Type4
}

[Tool]
[GlobalClass]
public partial class Replicator : Equipment
{
	[ExportToolButton(text: "Set up replicator mesh")]
	public Callable SetupDuplicatorMesh => Callable.From(SetupMesh);

	[Export]
	public ReplicatorType Type { get; set; }

	[Export]
	public bool Activated { get; set; } = false;

	[ExportCategory("Lights")]
	[Export]
	public Node3D LightsParent { get; set; }

	[Export]
	public Color LightColor { get; set; }

	[Export]
	public Color MeshBackLightColor { get; set; }

	public Node Mesh {
		get {
			if (FindChild(_meshNodeName) is null) {
				return null;
			}
			return GetNode(_meshNodeName);
		}
	}

	public Vector3 TopDownScale = new Vector3(1.12f, 1.584f, 1.6f);

	private string _meshNodeName = "Mesh";
	private ReplicatorStorage _replicatorStorage = new();

	public override void _Ready()
	{
		base._Ready();

		if (!Engine.IsEditorHint()) {
			_replicatorStorage = GD.Load<ReplicatorStorage>(Resources.ReplicatorStorage);
		}
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) {
			return;
		}

		if (LightsParent is not null) {
			LightsParent.Visible = Activated;

			if (_replicatorStorage.Replicators.ContainsKey(this)) {
				ReplicatorContent replicator = _replicatorStorage.Replicators[this];
				Activated = true;
				LightColor = replicator.Artifact.ReplicatorGlowColour;
			}
			else {
				Activated = false;
			}

			SetLights();
		}
	}

	public override void _Input(InputEvent @event)
	{
		var replicators = _replicatorStorage.Replicators;
		//bool canAdd = false;

		//if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed) {
		//	if (mouseButton.ButtonIndex == MouseButton.Left) {
		//		canAdd = true;
		//		GD.PrintS("mouse");
		//	}
		//}

		if (IsInstanceValid(QuickInventoryInstance)) {
			if (@event.IsActionReleased("action_use")
				&& QuickInventoryInstance.IsOpen
				&& !replicators.ContainsKey(this)
			) {
				TreeItem selectedItem = QuickInventoryInstance.QuickInventoryItemList.GetSelected();
				if (selectedItem is not null && AllowedInputType == ItemType.Artifact) {
					var artifact = (ArtifactResource)selectedItem.GetMetadata(0);

					replicators.Add(this, new ReplicatorContent(
						artifact,
						DateTime.TimeStamp(),
						0
					));

					//QuickInventoryInstance.Close();
					QuickInventoryInstance.QueueFree();
				}
			}
		}

		base._Input(@event);
	}

	public void SetLights()
	{
		foreach (OmniLight3D light in LightsParent.GetChildren()) {
			light.LightColor = LightColor;
		}
	}

	public void SetupMesh()
	{
		_renameMesh();

		if (IsInstanceValid(Mesh)) {
			var meshInstance = (MeshInstance3D)Mesh;
			if (meshInstance.Scale != TopDownScale) {
				meshInstance.Scale = TopDownScale;
			}
			var material = meshInstance.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;
			material.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
			material.BlendMode = BaseMaterial3D.BlendModeEnum.PremultAlpha;
			material.BacklightEnabled = true;
			material.Backlight = MeshBackLightColor;
		}
	}

	private bool _renameMesh()
	{
		foreach (var child in GetChildren()) {
			if (child is MeshInstance3D) {
				child.Name = _meshNodeName;
				return true;
			}
		}
		return false;
	}
}
