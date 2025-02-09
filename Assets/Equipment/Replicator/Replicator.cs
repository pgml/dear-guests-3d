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
	public delegate void ReplicationCompleteEventHandler();

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
	public bool IsReplicationComplete = false;
	public UiReplicator UiReplicatorInstance = null;
	public Vector3 TopDownScale = new Vector3(1.12f, 1.584f, 1.6f);

	public Dictionary<
		ArtifactGrowCondition,
		SliderProperties
	> CurrentSettings { get; set; } = new();

	public static AudioLibrary AudioLibrary { get {
		return GD.Load<AudioLibrary>(Resources.AudioLibrary);
	}}

	//public AudioInstance AudioInstance { get; private set; }
	public AudioInstance AudioInstance { get {
		return AudioLibrary.CreateAudioInstance("Replicator", this, 8);
	}}

	public AudioInstance ContinuousAudioInstance { get {
		return AudioLibrary.CreateAudioInstance("ReplicatorLoop", this, 32);
	}}

	private string _meshNodeName = "Mesh";
	private ReplicatorStorage _replicatorStorage = new();
	private ArtifactResource _currentArtifact;
	private Inventory _actorInventory;

	public override void _Ready()
	{
		base._Ready();

		if (!Engine.IsEditorHint()) {
			_replicatorStorage = GD.Load<ReplicatorStorage>(Resources.ReplicatorStorage);
			_actorInventory = GD.Load<Inventory>(Resources.ActorInventory);
		}

		ReplicationComplete += _onReplicationComplete;
		ContinuousAudioInstance.Audio.Finished += _onContinuousAudioFinished;
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
			var openingSound = Type switch {
				ReplicatorType.Type1 => AudioLibrary.ReplicatorType1Open,
				_ => null
			};

			if (openingSound is not null) {
				AudioInstance.PlayUiSound(openingSound);
			}

			// temporarily add replicator ui to mainUI...
			OpenUi();
		}
		else if (IsInstanceValid(UiReplicatorInstance)) {
			if (@event.IsActionReleased("action_use")) {
				if (Artifact() is null) {
					InsertArtifact();
				}
				else if (Artifact() is not null && !IsReplicating) {
					Replicate();
				}
			}

			if (@event.IsActionPressed("action_cancel")) {
				var closingSound = Type switch {
					ReplicatorType.Type1 => AudioLibrary.ReplicatorType1Close,
					_ => null
				};

				if (closingSound is not null) {
					AudioInstance.PlayUiSound(closingSound);
				}
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

				// create copy of content and update settings
				var content = Content();
				content.Artifact = artifact;
				_replicatorStorage.Update(this, content);
				_updateReplicatorUi();

				int itemResourceIndex = _actorInventory.GetItemResourceIndex(artifact);
				_actorInventory.RemoveOneItem(itemResourceIndex);
				AudioInstance.PlayUiSound(AudioLibrary.ReplicatorInsertArtifact);
			}
		}
	}

	public async void Replicate()
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

		_playSound(AudioLibrary.ReplicatorStart1);
		await ToSignal(AudioInstance.Audio, "finished");
		_playSound(AudioLibrary.ReplicatorHum1, true);

		IsReplicating = true;
		IsReplicationComplete = !IsReplicating;
		_updateReplicatorUi();
	}

	public void Collect()
	{
		if (!IsReplicationComplete) {
			return;
		}

		_moveReplicaToInventory();
		_moveArtifactToInventory();
		CancelReplication();
		AudioInstance.PlayUiSound(AudioLibrary.ReplicatorRetrieveArtifact);
		ContinuousAudioInstance.Stop();
		_playSound(AudioLibrary.ReplicatorStop1);

		IsReplicationComplete = false;
	}

	public void CancelReplication()
	{
		_moveArtifactToInventory();
		_replicatorStorage.Clear(this);
		//_currentArtifact = null;
		IsReplicating = false;
		Activated = false;
		_updateReplicatorUi();
		ContinuousAudioInstance.Stop();
		_playSound(AudioLibrary.ReplicatorStop1);
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

		return Math.Round(progress >= 100 ? 100 : progress, 2);
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

		System.TimeSpan remainingTimeSpan = ApproxEndDateTime().Subtract(DateTime.Now());
		int remainingTime = (int)Math.Round(remainingTimeSpan.TotalHours);

		if (!IsReplicationComplete && remainingTime == 0) {
			remainingTime = 1;
		}
		else if (IsReplicationComplete && remainingTime <= 0) {
			remainingTime = 0;
		}

		return remainingTime;
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

	/// <summary>
	/// The artifact that is currently stored in the replicator
	/// </summary>
	public ArtifactResource Artifact()
	{
		if (Content().Artifact is null || Content().Artifact.Name is null) {
			return null;
		}

		return Content().Artifact;
	}

	private void _playSound(AudioClip audioClip, bool continuous = false)
	{
		if (continuous) {
			ContinuousAudioInstance.Play(audioClip, AudioBus.Game);
		}
		else {
			AudioInstance.Play(audioClip, AudioBus.Game);
		}
	}

	private bool _moveArtifactToInventory()
	{
		return _actorInventory.AddItem(Artifact(), 1);
	}

	private bool _moveReplicaToInventory()
	{
		string replicaPath = Artifact().ReplicatesInto;
		if (ResourceLoader.Exists(replicaPath)) {
			var replica = GD.Load<ArtifactResource>(replicaPath);
			return _actorInventory.AddItem(replica, 1);
		}
		return false;
	}

	private void _updateReplicatorUi()
	{
		if (!IsInstanceValid(UiReplicatorInstance)) {
			return;
		}

		if (Progress() >= 100 && Activated && IsReplicating) {
			EmitSignal(SignalName.ReplicationComplete);
		}

		UiReplicatorInstance.UpdateUi(
			Artifact(),
			StartTimeString(),
			Progress(),
			RemainingTime(),
			IsReplicating,
			IsReplicationComplete
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

		bool isCollectPressedConnected = instance.CollectButton.IsConnected(
			"pressed",
			Callable.From(Collect)
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

		if (!isReplicatePressedConnected) {
			instance.CollectButton.Pressed += Collect;
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

	private void _onReplicationComplete()
	{
		IsReplicationComplete = true;
	}

	private void _onContinuousAudioFinished()
	{
		// replay audio but don't fix position
		ContinuousAudioInstance.Play(
			AudioLibrary.ReplicatorHum1,
			AudioBus.Game,
			false
		);
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
