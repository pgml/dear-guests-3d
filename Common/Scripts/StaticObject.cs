using Godot;

[Tool]
[GlobalClass]
public partial class StaticObject : Node3D
{
	[ExportToolButton(text: "Prepare For Topdown")]
	public Callable SetPrepareForTopDown => Callable.From(PrepareForTopdown);

	[Export]
	public bool IsSprite { get; set; }

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

	//public Vector3 TopDownScale = new Vector3(1.12f, 1.584f, 1.6f);
	public Vector3 TopDownScale = new Vector3(0.69f, 0.99f, 1.0f);
	public Node Mesh { get; private set; }

	public Sprite3D BillboardPlaceholer {
		get {
			if (FindChild(_billboardNodeName) is null) {
				return null;
			}
			var billboard = GetNode<Sprite3D>(_billboardNodeName);
			billboard.Visible = false;
			return billboard;
		}
	}

	protected World World;
	protected DirectionalLight3D Sun;

	private string _meshNodeName = "Mesh";
	private string _billboardNodeName = "Billboard";

	public override void _Ready()
	{
		if (!Engine.IsEditorHint()) {
			//World = GetTree().Root.GetNode<World>("Scene/World");
			World = GetTree().CurrentScene as World;

			if (IsInstanceValid(World)) {
				Sun = World.Sun;
			}
		}

		if (FindChild(_meshNodeName) is Node3D mesh) {
			Mesh = mesh;
		}

		ReplacePlaceholderWithMesh();
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) {
			return;
		}

		if (IsInstanceValid(Sun) &&
			IsInstanceValid(SunShadowMesh) &&
			Fake3DShadow)
		{
			SunShadowMesh.RotationDegrees = new Vector3(
				0,
				Sun.RotationDegrees.X,
				0
			);
		}
	}

	public async void ReplacePlaceholderWithMesh()
	{
		if (Engine.IsEditorHint() || Mesh is not InstancePlaceholder) {
			return;
		}

		if (IsInstanceValid(BillboardPlaceholer)) {
			BillboardPlaceholer.Visible = true;
		}

		if (GetNode(_meshNodeName) is InstancePlaceholder mesh) {
			//var meshNode = GetNode(_meshNodeName);
			var placeholder = (InstancePlaceholder)mesh;
			string path = placeholder.GetInstancePath();
			await AsyncLoader.LoadResource<Resource>(path, "", true);
			placeholder.CreateInstance();
			BillboardPlaceholer.QueueFree();
		}
	}

	public void PrepareForTopdown()
	{
		_renameMesh();

		Vector3 scale = TopDownScale;
		if (IsInstanceValid(Mesh)) {
			if (SunShadowMesh is not null) {
				SunShadowMesh.Scale = scale;
				SunShadowMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
				SunShadowMesh.Visible = _fake3DShadow;
			}

			var meshInstance = (MeshInstance3D)Mesh;
			if (_fake3DShadow) {
				meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			}
			else {
				meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided;
			}

			if (meshInstance.Scale != scale) {
				meshInstance.Scale = scale;
			}

			var collisionShape = (CollisionShape3D)Mesh.FindChild("CollisionShape3D");
			var shape = (collisionShape.Shape as ConcavePolygonShape3D);
			shape.BackfaceCollision = true;
			collisionShape.Shape = shape;

			//Mesh.SetSceneInstanceLoadPlaceholder(true);
			Mesh.SetSceneInstanceLoadPlaceholder(false);
		}

		if (!IsSprite) {
			_renameBillboard();
		}
		else {
			foreach (var child in GetChildren()) {
				if (child is Sprite3D) {
					Sprite3D sprite = child as Sprite3D;
					sprite.PixelSize = 0.07f;
					sprite.Shaded = true;
					sprite.AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass;
					sprite.TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmapsAnisotropic;
					sprite.CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided;
					sprite.Scale = scale;
				}
			}
		}

		if (IsInstanceValid(BillboardPlaceholer)) {
			//BillboardPlaceholer.Visible = false;
			BillboardPlaceholer.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			BillboardPlaceholer.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
			BillboardPlaceholer.PixelSize = 0.07f;
			BillboardPlaceholer.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
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

	private bool _renameBillboard()
	{
		foreach (var child in GetChildren()) {
			if (child is Sprite3D) {
				child.Name = _billboardNodeName;
				return true;
			}
		}
		return false;
	}
}
