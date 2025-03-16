using Godot;
using System;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class Replicator : Equipment
{
	[ExportToolButton(text: "Set up mesh")]
	public Callable SetupEquipmentMesh => Callable.From(SetupMesh);

	[Signal]
	public delegate void ReplicationCompleteEventHandler();

	[Export]
	public EquipmentType Type { get; set; }

	[Export]
	public bool Activated { get; set; } = false;

	[ExportCategory("Lights")]
	[Export]
	public Node3D LightsParent { get; set; }

	[Export]
	public Color LightColor { get; set; }


	public PackedScene UiReplicator { get {
		return GD.Load<PackedScene>(Resources.UiReplicator);
	}}

	public bool IsReplicating = false;
	public bool IsReplicationComplete = false;
	public UiReplicator UiReplicatorInst = null;

	public Dictionary<
		ArtifactGrowCondition,
		SliderProperties
	> CurrentSettings { get; set; } = new();

	public AudioLibrary AudioLibrary { get; private set; }
	public AudioInstance AudioInstance { get; private set; }
	public AudioInstance ContinuousAudioInstance { get; private set; }

	private ReplicatorStorage _replicatorStorage = new();
	private ReplicatorContent _content;
	private ArtifactResource _currentArtifact;
	private Inventory _actorInventory;

	public override void _Ready()
	{
		base._Ready();

		if (!Engine.IsEditorHint()) {
			_replicatorStorage = GD.Load<ReplicatorStorage>(Resources.ReplicatorStorage);
			_actorInventory = GD.Load<Inventory>(Resources.ActorInventory);

			AudioLibrary = GD.Load<AudioLibrary>(Resources.AudioLibrary);
			AudioInstance = AudioLibrary.CreateAudioInstance("Replicator", this, 8);
			ContinuousAudioInstance = AudioLibrary.CreateAudioInstance("ReplicatorLoop", this, 32);

			ReplicationComplete += _onReplicationComplete;
			ContinuousAudioInstance.Audio.Finished += _onContinuousAudioFinished;
		}
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) {
			return;
		}

		if (!HasPower()) {
			TurnOff();
		}

		if (LightsParent is not null) {
			SetLights();
		}

		if (IsInstanceValid(UiReplicatorInst)) {
			if (UiReplicatorInst.IsOpen) {
				UpdateProgress();
				_connectButtonSignals();
				_updateReplicatorUi();
			}
			else {
				_disconnectButtonSignals();
			}
		}

		base._Process(delta);
	}

	public override void _Input(InputEvent @event)
	{
		if (Engine.IsEditorHint()) {
			return;
		}

		if (ActorData is CreatureData data &&
			data.IsBuildMoveActive &&
			data.IsConsoleOpen)
		{
			return;
		}

		if (!IsInstanceValid(UiReplicatorInst) &&
			@event.IsActionReleased("action_use") &&
			CanUse)
		{
			var openingSound = Type switch {
				EquipmentType.Type1 => AudioLibrary.ReplicatorType1Open,
				_ => null
			};

			if (openingSound is not null) {
				AudioInstance.PlayUiSound(openingSound);
			}

			// temporarily add replicator ui to mainUI...
			OpenUi();
		}
		else if (IsInstanceValid(UiReplicatorInst)) {
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
					EquipmentType.Type1 => AudioLibrary.ReplicatorType1Close,
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
		TreeItem selectedItem = UiReplicatorInst.ItemList.GetSelected();
		if (selectedItem is not null && AllowedInputType == ItemType.Artifact) {
			var artifact = (ArtifactResource)selectedItem.GetMetadata(0);

			if (IsInstanceValid(artifact)) {
				UiReplicatorInst.ArtifactName.Text = artifact.Name;

				// create copy of content and update settings
				_content.Artifact = artifact;
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

		if (!HasPower()) {
			AudioInstance.PlayUiSound(AudioLibrary.MiscDenied);
			return;
		}

		_content.Artifact = Artifact();
		_content.ReplicationStart = DateTime.TimeStamp();
		_content.Settings = CurrentSettings;

		_playSound(AudioLibrary.ReplicatorStart1);
		await ToSignal(AudioInstance.Audio, "finished");
		_playSound(AudioLibrary.ReplicatorHum1, true);

		IsReplicating = true;
		IsReplicationComplete = !IsReplicating;
		Activated = true;
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
		IsReplicating = false;
		//_currentArtifact = null;
		TurnOff();
	}

	public void TurnOff()
	{
		if (!Activated) {
			return;
		}
		Activated = false;

		ContinuousAudioInstance.Stop();
		_content.ReplicationPause = DateTime.TimeStamp();
		_playSound(AudioLibrary.ReplicatorStop1);
	}

	public void UpdateProgress()
	{
		if (!IsReplicating) {
			return;
		}

		double startTime = StartTime();
		double endTime = EndTime();
		double currentTime = _content.ReplicationPause > 0
			? _content.ReplicationPause
			: DateTime.TimeStamp();
		double progress = (currentTime - startTime) / (endTime - startTime) * 100;

		_content.Progress = Math.Round(progress >= 100 ? 100 : progress, 2);
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
			if (!_content.Settings.ContainsKey(condition) &&
				_replicatorStorage.Has(this))
			{
				continue;
			}

			DeviationPenalty artifactDeviationPenalty = Artifact()
				.DeviationPenalty(condition);
			double individualPenalty = artifactDeviationPenalty.Penalty;
			double deviationTolerance = artifactDeviationPenalty.Tolerance;
			float optimalCondition = Artifact().OptimalGrowConditions[condition];
			SliderProperties properties = _content.Settings[condition];

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
		var brightness = Brightness();

		if (!HasPower()) {
			brightness = 0;
		}

		bool hasContent = false;
		if (_replicatorStorage.Has(this)) {
			hasContent = true;
		}

		LightsParent.Visible = brightness > 0 && IsReplicating;
		if (hasContent && IsReplicating) {
			LightColor = _content.Artifact.ReplicatorGlowColour;
		}

		foreach (OmniLight3D light in LightsParent.GetChildren()) {
			light.LightColor = LightColor;
		}

		OmniLight3D tubeLight = LightsParent.GetChild<OmniLight3D>(0);

		if (hasContent && IsReplicating) {
			float lightEnergy = Mathf.Sqrt((float)brightness / 100);
			tubeLight.LightEnergy = lightEnergy;
			tubeLight.OmniRange = 5 + ((float)brightness / 500) * 3;
		}
	}

	public void UpdateSettings(
		ArtifactGrowCondition condition,
		SliderProperties sliderProperties)
	{
		if (CurrentSettings.ContainsKey(condition)) {
			CurrentSettings[condition] = sliderProperties;
		}
		else {
			CurrentSettings.Add(condition, sliderProperties);
		}

		if (_replicatorStorage.Has(this)) {
			_content.Settings = CurrentSettings;
		}
		else {
			var content = new ReplicatorContent(
				Artifact(), DateTime.TimeStamp(), 0, 0, CurrentSettings
			);

			_content = content;
			_replicatorStorage.Add(this, _content);
		}
	}

	public void OpenUi()
	{
		if (!ActorData.IsAnyUiPanelOpen()) {
			UiReplicatorInst = UiReplicator.Instantiate<UiReplicator>();
			UiReplicatorInst.Replicator = this;
			GetNode("/root/MainUI").AddChild(UiReplicatorInst);
			Vector2 position = InventoryPosition(UiReplicatorInst);
			UiReplicatorInst.Open(position);
		}
	}

	public void CloseUi()
	{
		UiReplicatorInst.Close();
	}

	//protected override bool HasPower()
	//{
	//	if (!base.HasPower()) {
	//		Activated = false;
	//		//IsReplicating = false;
	//	}

	//	return base.HasPower();
	//}

	// ----- some helpers

	public double StartTime()
	{
		double startTime = DateTime.TimeStamp();

		if (_content.ReplicationStart > 0) {
			startTime = _content.ReplicationStart;
		}

		//if (_content.ReplicationPause > 0) {
		//	startTime = _content.ReplicationPause;
		//}

		return startTime;
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

		if (_content is null ||
			_content.Settings is null ||
			!_content.Settings.ContainsKey(brightnessEnum))
		{
			return 0;
		}

		return _content.Settings[brightnessEnum].Value;
	}

	/// <summary>
	/// The artifact that is currently stored in the replicator
	/// </summary>
	public ArtifactResource Artifact()
	{
		if (_content is null ||
			_content.Artifact is null ||
			_content.Artifact.Name is null)
		{
			return null;
		}

		return _content.Artifact;
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
		if (!IsInstanceValid(UiReplicatorInst)) {
			return;
		}

		if (_content.Progress >= 100 && Activated && IsReplicating) {
			EmitSignal(SignalName.ReplicationComplete);
		}

		UiReplicatorInst.UpdateUi(
			Artifact(),
			StartTimeString(),
			_content.Progress,
			RemainingTime(),
			IsReplicating,
			IsReplicationComplete
		);
	}

	private void _connectButtonSignals()
	{
		UiReplicator instance = UiReplicatorInst;
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
		UiReplicator instance = UiReplicatorInst;
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
}
