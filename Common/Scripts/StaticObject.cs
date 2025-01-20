using Godot;

[Tool]
[GlobalClass]
public partial class StaticObject : Node3D
{
	[ExportToolButton(text: "Prepare For Topdown")]
	public Callable SetPrepareForTopDown => Callable.From(PrepareForTopdown);

	[Export]
	public MeshInstance3D SunShadowMesh { get; set; }

	private bool _fake3DShadow = false;
	[Export]
	public bool Fake3DShadow {
		get => _fake3DShadow;
		set {
			_fake3DShadow = value;
			PrepareForTopdown();
		}
	}

	public MeshInstance3D Mesh { get; set; }
	public Vector3 TopDownScale = new Vector3(1.12f, 1.584f, 1.6f);

	protected World World;
	protected DirectionalLight3D Sun;

	public override void _Ready()
	{
		if (!Engine.IsEditorHint()) {
			World = GetTree().Root.GetNode<World>("Scene/World");

			if (IsInstanceValid(World)) {
				Sun = World.Sun;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) {
			return;
		}

		if (IsInstanceValid(Sun) && IsInstanceValid(SunShadowMesh) && Fake3DShadow) {
			SunShadowMesh.RotationDegrees = new Vector3(0, Sun.RotationDegrees.X, 0);
		}
	}

	public void PrepareForTopdown()
	{
		if (!_renameMesh() || FindChild("Mesh") is null) {
			return;
		}

		Mesh = GetNode<MeshInstance3D>("Mesh");

		if (!IsInstanceValid(Mesh)) {
			return;
		}

		if (SunShadowMesh is not null) {
			SunShadowMesh.Scale = TopDownScale;
			SunShadowMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
			SunShadowMesh.Visible = _fake3DShadow;
		}

		if (_fake3DShadow) {
			Mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		}
		else {
			Mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided;
		}

		if (Mesh.Scale != TopDownScale) {
			Mesh.Scale = TopDownScale;
		}
	}

	private bool _renameMesh()
	{
		var children = GetChildren();

		if (children[0] is not null) {
			children[0].Name = "Mesh";
			return true;
		}

		return false;
	}
}
