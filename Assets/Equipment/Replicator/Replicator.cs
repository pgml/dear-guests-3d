using Godot;
using System;
using System.Collections.Generic;

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

	public bool IsReplicating = false;
	public UiReplicator UiReplicatorInstance = null;
	public Vector3 TopDownScale = new Vector3(1.12f, 1.584f, 1.6f);

	public Dictionary<
		ArtifactGrowCondition,
		SliderProperties
	> CurrentSettings { get; set; } = new();

	private string _meshNodeName = "Mesh";
	private ReplicatorStorage _replicatorStorage = new();
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
		if (LightsParent is not null) {
			SetLights();
		}

		if (IsInstanceValid(UiReplicatorInstance) && UiReplicatorInstance.IsOpen) {
			UiReplicator instance = UiReplicatorInstance;
			bool isInsertPressedConnected = instance.InsertButton.IsConnected(
				"pressed",
				Callable.From(InsertArtifact)
			);

			bool isReplicatePressedConnected = instance.ReplicateButton.IsConnected(
				"pressed",
				Callable.From(Replicate)
			);

			bool isClosedConnected = instance.CloseButton.IsConnected(
				"pressed",
				Callable.From(CloseUi)
			);

			if (!isInsertPressedConnected) {
				instance.InsertButton.Pressed += InsertArtifact;
			}

			if (!isReplicatePressedConnected) {
				instance.ReplicateButton.Pressed += Replicate;
			}

			if (!isClosedConnected) {
				instance.CloseButton.Pressed += CloseUi;
			}

			if (_currentArtifact is not null) {
				_updateReplicatorInstanceUi();
			}
			else {
				_currentArtifact = Content().Artifact;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (Engine.IsEditorHint()) {
			return;
		}

		if (!IsInstanceValid(UiReplicatorInstance)
			&& (@event.IsActionReleased("action_use"))
			&& CanUse
		) {
			// temporarily add replicator ui to mainUI...
			OpenUi();
		}
		else if (IsInstanceValid(UiReplicatorInstance)) {
			if (@event.IsActionPressed("action_cancel")) {
				CloseUi();
			}
		}
	}

	public void InsertArtifact()
	{
		TreeItem selectedItem = UiReplicatorInstance.ItemList.GetSelected();
		if (selectedItem is not null && AllowedInputType == ItemType.Artifact) {
			_currentArtifact = (ArtifactResource)selectedItem.GetMetadata(0);

			if (IsInstanceValid(_currentArtifact)) {
				UiReplicatorInstance.ArtifactName.Text = _currentArtifact.Name;
				_updateReplicatorInstanceUi();
			}
		}
	}

	public void Replicate()
	{
		if (_currentArtifact is null) {
			return;
		}
		_replicatorStorage.Update(this, new ReplicatorContent(
			_currentArtifact,
			DateTime.TimeStamp(),
			0,
			CurrentSettings
		));
		IsReplicating = true;
		_updateReplicatorInstanceUi();
	}

	private void _updateReplicatorInstanceUi()
	{
		if (_currentArtifact is not null) {
			UiReplicator instance = UiReplicatorInstance;
			instance.ReplicatorStatus.Visible = true;
			instance.ArtifactName.Text = _currentArtifact.Name;

			instance.InsertButton.Visible = false;
			instance.ReplicateButton.Visible = true;

			if (IsReplicating) {
				instance.StartTime.Text = StartTimeString();
				instance.Progress.Text = $"{Progress()}%";

				instance.ReplicateButton.Visible = false;
				instance.CancelButton.Visible = true;
			}
			instance.EndTime.Text = $"~ {RemainingTime()}h";

			instance.SettingsParent.Visible = true;
			instance.SettingsHeadline.Visible = true;

			instance.ItemList.Visible = false;
			instance.ItemListHeadline.Visible = false;

		}
	}

	public double Progress()
	{
		if (!Activated) {
			return 0;
		}

		double startTime = StartTime();
		double endTime = EndTime();
		double currentTime = DateTime.TimeStamp();
		double progress = (currentTime - startTime) / (endTime - startTime) * 100;

		return Math.Round(progress, 2);
	}

	public double StartTime()
	{
		return Content().ReplicationStart;
	}

	public string StartTimeString()
	{
		return DateTime.TimeStampToDateTimeString(StartTime());
	}

	public double EndTime()
	{
		return DateTime.TimeStamp(ApproxEndDateTime());
	}

	public string EndTimeString()
	{
		return EndTime().ToString();
	}

	public System.DateTime ApproxEndDateTime()
	{
		double optimalReplicationTime = _currentArtifact.FastestReplicationTime;
		int increaseStep = 50;
		double penalty = 0;

		foreach (var (condition, value) in _currentArtifact.OptionalGrowConditions) {
			if (_replicatorStorage.Has(this)) {
				var properties = Content().Settings[condition];
				var optimalCondition = _currentArtifact.OptionalGrowConditions[condition];
				penalty = optimalReplicationTime + Mathf.FloorToInt(
					Mathf.Abs(properties.Value - optimalCondition) / increaseStep
				);
			}
		}

		double startTime = Content().ReplicationStart	== 0
			? DateTime.TimeStamp()
			: StartTime();

		return DateTime
			.TimeStampToDateTime(startTime)
			.AddHours(penalty);
	}

	public int RemainingTime()
	{
		System.TimeSpan remainingTime = ApproxEndDateTime().Subtract(DateTime.Now());
		return (int)Math.Round(remainingTime.TotalHours);
	}

	public void SetLights()
	{
		if (_currentArtifact is null) {
			return;
		}

		bool hasContent = false;
		if (_replicatorStorage.Has(this)) {
			hasContent = true;
		}

		LightsParent.Visible = Activated;
		if (hasContent && IsReplicating) {
			Activated = true;
			LightColor = Content().Artifact.ReplicatorGlowColour;
		}
		else {
			Activated = false;
		}

		foreach (OmniLight3D light in LightsParent.GetChildren()) {
			light.LightColor = LightColor;
		}

		OmniLight3D tubeLight = LightsParent.GetChild<OmniLight3D>(0);
		var brightnessEnum = ArtifactGrowCondition.Brightness;

		if (hasContent && IsReplicating) {
			var value = Content().Settings[brightnessEnum].Value;
			float lightEnergy = Mathf.Sqrt((float)value / 100);
			tubeLight.LightEnergy = lightEnergy;
			tubeLight.OmniRange = 5 + ((float)value / 500) * 3;

			if (lightEnergy == 0) {
				Activated = false;
			}
		}
	}

	public void UpdateSettings(
		ArtifactGrowCondition condition,
		SliderProperties sliderProperties
	)
	{
		if (CurrentSettings.ContainsKey(condition)) {
			CurrentSettings[condition] = sliderProperties;
		}
		else {
			CurrentSettings.Add(condition, sliderProperties);
		}

		if (_replicatorStorage.Has(this)) {
			var content = Content();
			content.Settings = CurrentSettings;
			_replicatorStorage.Update(this, content);
		}
		else {
			_replicatorStorage.Add(this, new ReplicatorContent(
				_currentArtifact,
				DateTime.TimeStamp(),
				0,
				CurrentSettings
			));
		}
	}

	public void OpenUi()
	{
		if (!ActorData.IsAnyUiPanelOpen()) {
			UiReplicatorInstance = UiReplicator.Instantiate<UiReplicator>();
			UiReplicatorInstance.Replicator = this;
			GetNode("/root/MainUI").AddChild(UiReplicatorInstance);
			Vector2 position = InventoryPosition(UiReplicatorInstance);
			UiReplicatorInstance.Open(position);
		}
	}

	public void CloseUi()
	{
		UiReplicatorInstance.Close();
	}

	public ReplicatorContent Content()
	{
		return _replicatorStorage.Replicators[this];
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
