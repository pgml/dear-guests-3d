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
			//_replicatorStorage.Replicator = this;
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

			bool isClosedConnected = UiReplicatorInstance.CloseButton.IsConnected(
				"pressed",
				Callable.From(CloseUi)
			);

			if (!isPressedConnected) {
				UiReplicatorInstance.ReplicateButton.Pressed += InsertArtifact;
			}

			if (!isClosedConnected) {
				UiReplicatorInstance.CloseButton.Pressed += CloseUi;
			}

			_currentArtifact = _replicatorStorage.Content(this).Artifact;
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
				var replicators = _replicatorStorage.Replicators;
				_replicatorStorage.Update(this, new ReplicatorContent(
					_currentArtifact,
					DateTime.TimeStamp(),
					0,
					CurrentSettings
				));

				_updateReplicatorInstanceUi();
			}
		}
	}

	private void _updateReplicatorInstanceUi()
	{
		if (_currentArtifact is not null) {
			UiReplicator instance = UiReplicatorInstance;
			double replicationStart = _replicatorStorage.Content(this).ReplicationStart;

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
		return _replicatorStorage.Content(this).ReplicationStart;
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
			//if (_replicatorStorage.Has(this)) {
			if (_replicatorStorage.Replicators.ContainsKey(this)) {
				var properties = _replicatorStorage.Content(this).Settings[condition];
				var optimalCondition = _currentArtifact.OptionalGrowConditions[condition];
				penalty = optimalReplicationTime + Mathf.FloorToInt(
					Mathf.Abs(properties.Value - optimalCondition) / increaseStep
				);
			}
		}

		return DateTime
			.TimeStampToDateTime(StartTime())
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
		//if (_replicatorStorage.Has(this)) {
		if (_replicatorStorage.Replicators.ContainsKey(this)) {
			hasContent = true;
		}

		LightsParent.Visible = Activated;
		if (hasContent) {
			Activated = true;
			LightColor = _replicatorStorage.Content(this).Artifact.ReplicatorGlowColour;
		}
		else {
			Activated = false;
		}

		foreach (OmniLight3D light in LightsParent.GetChildren()) {
			light.LightColor = LightColor;
		}

		OmniLight3D tubeLight = LightsParent.GetChild<OmniLight3D>(0);
		var brightnessEnum = ArtifactGrowCondition.Brightness;

		//if (_replicatorStorage.Has(this)) {
		if (_replicatorStorage.Replicators.ContainsKey(this)) {
			var value = _replicatorStorage.Content(this).Settings[brightnessEnum].Value;
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
			return;
		}
		CurrentSettings.Add(condition, sliderProperties);

		//if (_replicatorStorage.Has(this)) {
		if (_replicatorStorage.Replicators.ContainsKey(this)) {
			var content = _replicatorStorage.Replicators[this];
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
