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
	[Signal]
	public delegate void ReplicationFinishedEventHandler();

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
		if (Engine.IsEditorHint()) {
			return;
		}

		if (LightsParent is not null) {
			SetLights();
		}

		if (IsInstanceValid(UiReplicatorInstance)) {
			if (UiReplicatorInstance.IsOpen) {
				_connectButtonSignals();
				_updateReplicatorUi();
			}
			else {
				_disconnectButtonSignals();
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
			var artifact = (ArtifactResource)selectedItem.GetMetadata(0);

			if (IsInstanceValid(artifact)) {
				UiReplicatorInstance.ArtifactName.Text = artifact.Name;
				var content = Content();
				content.Artifact = artifact;
				_replicatorStorage.Update(this, content);
				_updateReplicatorUi();
			}
		}
	}

	public void Replicate()
	{
		if (Artifact() is null) {
			return;
		}

		_replicatorStorage.Update(this, new ReplicatorContent(
			Artifact(),
			DateTime.TimeStamp(),
			0,
			CurrentSettings
		));

		IsReplicating = true;
		_updateReplicatorUi();
	}

	public void CancelReplication()
	{
		_replicatorStorage.Clear(this);
		//_currentArtifact = null;
		IsReplicating = false;
		Activated = false;
		_updateReplicatorUi();
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

	/// <summary>
	/// Calculate the approximate time till the replication will be finished
	/// based on the artifact grow conditions<br />
	/// Penalties will be given dependent on the deviation penalties
	/// set in the artifact
	/// </summary>
	public System.DateTime ApproxEndDateTime()
	{
		double deviationPenalty = 0;

		foreach (var (condition, value) in Artifact().OptimalGrowConditions) {
			if (!Content().Settings.ContainsKey(condition)
				&& _replicatorStorage.Has(this)
			) {
				continue;
			}

			DeviationPenalty artifactDeviationPenalty = Artifact().DeviationPenalty(condition);
			double individualPenalty = artifactDeviationPenalty.Penalty;
			double deviationTolerance = artifactDeviationPenalty.Tolerance;
			float optimalCondition = Artifact().OptimalGrowConditions[condition];
			SliderProperties properties = Content().Settings[condition];

			double diffFromOptimal = Mathf.Abs(properties.Value - optimalCondition);
			individualPenalty *= diffFromOptimal / deviationTolerance;
			deviationPenalty += Mathf.FloorToInt(individualPenalty);
		}

		return DateTime
			.TimeStampToDateTime(StartTime())
			.AddHours(deviationPenalty + Artifact().FastestReplicationTime);
	}

	public int RemainingTime()
	{
		if (Artifact() is null) {
			return 0;
		}

		System.TimeSpan remainingTime = ApproxEndDateTime().Subtract(DateTime.Now());
		return (int)Math.Round(remainingTime.TotalHours);
	}

	public void SetLights()
	{
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

		if (hasContent && IsReplicating) {
			var value = Brightness();
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
				Artifact(),
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

	// ----- some helpers

	public ReplicatorContent Content()
	{
		if (!_replicatorStorage.Replicators.ContainsKey(this)) {
			return new();
		}
		return _replicatorStorage.Replicators[this];
	}

	public double StartTime()
	{
		return Content().ReplicationStart == 0
			? DateTime.TimeStamp()
			: Content().ReplicationStart;
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

	public double Brightness()
	{
		var brightnessEnum = ArtifactGrowCondition.Brightness;

		if (Content().Settings is null
			|| !Content().Settings.ContainsKey(brightnessEnum)
		) {
			return 0;
		}

		return Content().Settings[brightnessEnum].Value;
	}

	public ArtifactResource Artifact()
	{
		if (Content().Artifact is null || Content().Artifact.Name is null) {
			return null;
		}

		return Content().Artifact;
	}

	private void _updateReplicatorUi()
	{
		if (Artifact() is null) {
			return;
		}

		UiReplicatorInstance.UpdateUi(
			Artifact(),
			StartTimeString(),
			Progress(),
			RemainingTime(),
			IsReplicating
		);
	}

	private void _connectButtonSignals()
	{
		UiReplicator instance = UiReplicatorInstance;
		bool isInsertPressedConnected = instance.InsertButton.IsConnected(
			"pressed",
			Callable.From(InsertArtifact)
		);

		bool isReplicatePressedConnected = instance.ReplicateButton.IsConnected(
			"pressed",
			Callable.From(Replicate)
		);

		bool isCancelPressedConnected = instance.CancelButton.IsConnected(
			"pressed",
			Callable.From(CancelReplication)
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

		if (!isCancelPressedConnected) {
			instance.CancelButton.Pressed += CancelReplication;
		}

		if (!isClosedConnected) {
			instance.CloseButton.Pressed += CloseUi;
		}
	}

	private void _disconnectButtonSignals()
	{
		UiReplicator instance = UiReplicatorInstance;
		instance.InsertButton.Pressed -= InsertArtifact;
		instance.ReplicateButton.Pressed -= Replicate;
		instance.CancelButton.Pressed -= CancelReplication;
		instance.CloseButton.Pressed -= CloseUi;
	}

	// ----- Tools

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
