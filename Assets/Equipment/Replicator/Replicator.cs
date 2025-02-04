using Godot;
using System;

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

	public PackedScene UiReplicator { get {
		return GD.Load<PackedScene>(Resources.UiReplicator);
	}}
	public UiReplicator UiReplicatorInstance = null;

	public Vector3 TopDownScale = new Vector3(1.12f, 1.584f, 1.6f);

	private string _meshNodeName = "Mesh";
	private ReplicatorStorage _replicatorStorage = new();
	private ReplicatorContent _replicatorContent;
	private ArtifactResource _currentArtifact;

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
			SetLights();
		}

		if (IsInstanceValid(UiReplicatorInstance) && UiReplicatorInstance.IsOpen) {
			bool isPressedConnected = UiReplicatorInstance.ReplicateButton.IsConnected(
				"pressed",
				Callable.From(InsertArtifact)
			);

			if (!isPressedConnected) {
				UiReplicatorInstance.ReplicateButton.Pressed += InsertArtifact;
			}

			bool isClosedConnected = UiReplicatorInstance.CloseButton.IsConnected(
				"pressed",
				Callable.From(CloseUi)
			);

			if (!isClosedConnected) {
				UiReplicatorInstance.CloseButton.Pressed += CloseUi;
			}

			_currentArtifact = _replicatorContent.Artifact;
			if (_currentArtifact is not null) {
				_updateReplicatorInstanceUi();
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsInstanceValid(UiReplicatorInstance)
			&& (@event.IsActionReleased("action_use"))
			&& CanUse
		) {
			// temporarily add replicator ui to mainUI...
			UiReplicatorInstance = UiReplicator.Instantiate<UiReplicator>();
			GetNode("/root/MainUI").AddChild(UiReplicatorInstance);
			Vector2 position = InventoryPosition(UiReplicatorInstance);
			UiReplicatorInstance.Open(position);
		}

		if (IsInstanceValid(UiReplicatorInstance)) {
			if (@event.IsActionPressed("action_cancel")) {
				CloseUi();
			}
		}
	}

	public void InsertArtifact()
	{
		if (_replicatorStorage.Replicators.ContainsKey(this)) {
			return;
		}

		TreeItem selectedItem = UiReplicatorInstance.ItemList.GetSelected();
		if (selectedItem is not null && AllowedInputType == ItemType.Artifact) {
			var artifact = (ArtifactResource)selectedItem.GetMetadata(0);

			if (IsInstanceValid(artifact)) {
				if (_currentArtifact is not null) {
					return;
				}

				var replicators = _replicatorStorage.Replicators;
				replicators.Add(this, new ReplicatorContent(
					artifact,
					DateTime.TimeStamp(),
					0
				));

				_updateReplicatorInstanceUi();
			}
		}
	}

	private void _updateReplicatorInstanceUi()
	{
		if (_currentArtifact is not null) {
			UiReplicator instance = UiReplicatorInstance;
			double replicationStart = _replicatorContent.ReplicationStart;

			instance.ReplicatorStatus.Visible = true;
			instance.ReplicatorStatus.Text = "Replicating artifact...";
			instance.ArtifactName.Text = _currentArtifact.Name;
			instance.StartTime.Text = StartTimeString();
			instance.Progress.Text = $"{Progress()}%";
			instance.EndTime.Text = $"~ {RemainingTime()}h";

			instance.SettingsParent.Visible = true;
			instance.SettingsHeadline.Visible = true;

			instance.ItemList.Visible = false;
			instance.ItemListHeadline.Visible = false;
		}
	}

	public double Progress()
	{
		double startTime = StartTime();
		double endTime = EndTime();
		double currentTime = DateTime.TimeStamp();
		double progress = (currentTime - startTime) / (endTime - startTime) * 100;

		return Math.Round(progress, 2);
	}

	public double StartTime()
	{
		return _replicatorContent.ReplicationStart;
	}

	public string StartTimeString()
	{
		return DateTime.TimeStampToDateTimeString(
			_replicatorContent.ReplicationStart
		);
	}

	public double EndTime()
	{
		return DateTime.TimeStamp(EndDateTime());
	}

	public string EndTimeString()
	{
		return EndTime().ToString();
	}

	public System.DateTime EndDateTime()
	{
		return DateTime
			.TimeStampToDateTime(_replicatorContent.ReplicationStart)
			.AddHours(_currentArtifact.ReplicationTime);
	}

	public int RemainingTime()
	{
		System.TimeSpan remainingTime = EndDateTime().Subtract(DateTime.Now());
		return (int)Math.Round(remainingTime.TotalHours);
	}

	public void SetLights()
	{
		bool hasContent = false;
		if (_replicatorStorage.Replicators.ContainsKey(this)) {
			_replicatorContent = _replicatorStorage.Replicators[this];
			hasContent = true;
		}

		LightsParent.Visible = Activated;
		if (hasContent) {
			Activated = true;
			LightColor = _replicatorContent.Artifact.ReplicatorGlowColour;
		}
		else {
			Activated = false;
		}

		foreach (OmniLight3D light in LightsParent.GetChildren()) {
			light.LightColor = LightColor;
		}
	}

	public void CloseUi()
	{
		UiReplicatorInstance.QueueFree();
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
