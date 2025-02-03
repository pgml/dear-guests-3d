using Godot;

public enum DuplicatorType
{
	Type1,
	Type2,
	Type3,
	Type4
}

[Tool]
[GlobalClass]
public partial class Duplicator : Equipment
{
	[ExportToolButton(text: "Set up duplicator mesh")]
	public Callable SetupDuplicatorMesh => Callable.From(SetupMesh);

	[Export]
	public DuplicatorType Type { get; set; }

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

	public override void _Process(double delta)
	{
		if (LightsParent is not null) {
			LightsParent.Visible = Activated;
			foreach (OmniLight3D light in LightsParent.GetChildren()) {
				light.LightColor = LightColor;
			}
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
