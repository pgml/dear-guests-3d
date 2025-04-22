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
	public bool IsActivated { get; set; } = false;

	[ExportCategory("Lights")]
	[Export]
	public Node3D LightsParent { get; set; }

	[Export]
	public Color LightColor { get; set; }


	public PackedScene UiReplicator { get {
		return GD.Load<PackedScene>(Resources.UiReplicator);
	}}

	public UiReplicator UiReplicatorInst = null;

	public Dictionary<
		ArtifactGrowCondition,
		SliderProperties
	> CurrentSettings { get; set; } = new();

	public AudioLibrary AudioLibrary { get; private set; }
	public AudioInstance AudioInstance { get; private set; }

	/// <summary>
	/// AudioInstance for audio that needs to run continuously
	/// </summary>
	public AudioInstance ContinuousAudioInstance { get; private set; }

	private ReplicatorStorage _replicatorStorage = new();
	private ReplicationProcess _process = new();
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
		else {
			if (LightsParent is not null) {
				SetLights();
			}
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
			// temporarily add replicator ui to mainUI...
			OpenUi();
		}
		else if (IsInstanceValid(UiReplicatorInst)) {
			if (@event.IsActionReleased("action_use")) {
				if (_process.Artifact is null) {
					InsertArtifact();
				}
				else if (_process.Artifact is not null && !_process.InProgress) {
					Replicate();
				}
			}

			if (@event.IsActionPressed("action_cancel")) {
				CloseUi();
			}
		}
	}

	/// <summary>
	/// Insert an artifact into a replicator
	/// </summary>
	public void InsertArtifact()
	{
		TreeItem selectedItem = UiReplicatorInst.ItemList.GetSelected();
		if (selectedItem is not null && AllowedInputType == ItemType.Artifact) {
			var artifact = (ArtifactResource)selectedItem.GetMetadata(0);

			if (IsInstanceValid(artifact)) {
				UiReplicatorInst.ArtifactName.Text = artifact.Name;

				// create copy of content and update settings
				_process.Artifact = artifact;
				_updateReplicatorUi();

				int itemResourceIndex = _actorInventory.GetItemResourceIndex(artifact);
				_actorInventory.RemoveOneItem(itemResourceIndex);
				AudioInstance.PlayUiSound(AudioLibrary.ReplicatorInsertArtifact);
			}
		}
	}

	/// <summary>
	/// Start the replication process
	/// </summary>
	public async void Replicate()
	{
		if (_process.Artifact is null) {
			return;
		}

		if (!HasPower()) {
			AudioInstance.PlayUiSound(AudioLibrary.MiscDenied);
			return;
		}

		//_process.Artifact = Artifact();
		if (!_process.IsPaused) {
			_process.TimeStart = DateTime.TimeStamp();
		}
		_process.Settings = CurrentSettings;
		_process.IsPaused = false;
		_process.InProgress = true;
		//_process.IsComplete = !_process.InProgress;

		_playSound(AudioLibrary.ReplicatorStart1);
		await ToSignal(AudioInstance.Audio, "finished");
		_playSound(AudioLibrary.ReplicatorHum1, true);

		IsActivated = true;
		_updateReplicatorUi();
	}

	/// <summary>
	/// Collect a finished replicated artifact
	/// </summary>
	public void Collect()
	{
		if (!_process.IsComplete) {
			return;
		}

		_moveReplicaToInventory();
		_moveArtifactToInventory();
		CancelReplication();
		AudioInstance.PlayUiSound(AudioLibrary.ReplicatorRetrieveArtifact);
		ContinuousAudioInstance.Stop();
		_playSound(AudioLibrary.ReplicatorStop1);

		_process.IsComplete = false;
	}

	public void CancelReplication()
	{
		_moveArtifactToInventory();
		_replicatorStorage.Clear(this);
		_process.IsComplete = false;
		//_currentArtifact = null;
		TurnOff();
	}

	/// <summary>
	/// Turn off the replicator
	/// </summary>
	public void TurnOff()
	{
		if (_process.InProgress) {
			_process.TimePaused = DateTime.TimeStamp();
			_process.ProgressPaused = _process.Progress;
			_process.IsPaused = true;
		}

		if (!IsActivated) {
			return;
		}
		IsActivated = false;

		ContinuousAudioInstance.Stop();

		UpdateProgress();
		_playSound(AudioLibrary.ReplicatorStop1);
	}

	/// <summary>
	/// Update the current replication progress
	/// </summary>
	public void UpdateProgress()
	{
		if (!_process.InProgress || _process.IsPaused || !HasPower()) {
			return;
		}

		double startTime = _process.StartTime();

		//if (_process.ProgressPaused > 0) {
		//	startTime -= _process.ProgressPaused;
		//}

		double endTime = _process.EndTime();
		double currentTime = DateTime.TimeStamp();
		double progress = (currentTime - startTime) / (endTime - startTime) * 100;

		if (_process.ProgressPaused > 0) {
			progress -= _process.ProgressPaused;
		}

		 //GD.PrintS(startTime, endTime, currentTime, _process.PauseProgress);
		_process.Progress = Math.Round(progress >= 100 ? 100 : progress, 2);
	}

	/// <summary>
	/// Activates the replicator lights and sets brightness and colour
	/// according to the replicator settings and artifact type
	/// </summary>
	public void SetLights()
	{
		if (_process.Artifact is null) {
			LightsParent.Visible = false;
			return;
		}

		var brightness = Brightness();

		if (!HasPower()) {
			brightness = 0;
		}

		bool hasContent = false;
		if (_replicatorStorage.Has(this)) {
			hasContent = true;
		}

		LightsParent.Visible = brightness > 0 && _process.InProgress;
		if (hasContent && _process.InProgress) {
			LightColor = _process.Artifact.ReplicatorGlowColour;
		}

		foreach (OmniLight3D light in LightsParent.GetChildren()) {
			light.LightColor = LightColor;
		}

		var tubeLight = LightsParent.GetChild<OmniLight3D>(0);

		if (hasContent && _process.InProgress) {
			float lightEnergy = Mathf.Sqrt((float)brightness / 100);
			tubeLight.LightEnergy = lightEnergy;
			tubeLight.OmniRange = 5 + ((float)brightness / 500) * 3;
		}
	}

	/// <summary>
	/// Save current settings from replicator ui to storage
	/// </summary>
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
			_process.Settings = CurrentSettings;
		}
		else {
			ReplicationProcess process = new() {
				TimeStart = DateTime.TimeStamp(),
				Settings = CurrentSettings
			};

			_process = process;
			_replicatorStorage.Add(this, _process);
		}
	}

	public void OpenUi()
	{
		if (!ActorData.IsAnyUiPanelOpen()) {
			_playOpeningSound();

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

	public double Brightness()
	{
		var brightnessEnum = ArtifactGrowCondition.Brightness;

		if (_process.Settings is null || !_process.Settings.ContainsKey(brightnessEnum)) {
			return 0;
		}

		return _process.Settings[brightnessEnum].Value;
	}

	/// <summary>
	/// The artifact that is currently stored in the replicator
	/// </summary>
	//public ArtifactResource Artifact()
	//{
	//	if (_process.Artifact is null || _process.Artifact.Name is null) {
	//		return null;
	//	}

	//	return _process.Artifact;
	//}

	private void _playSound(AudioClip audioClip, bool continuous = false)
	{
		if (continuous) {
			ContinuousAudioInstance.Play(audioClip, AudioBus.Game);
		}
		else {
			AudioInstance.Play(audioClip, AudioBus.Game);
		}
	}

	private void _playOpeningSound()
	{
		var openingSound = Type switch {
			EquipmentType.Type1 => AudioLibrary.ReplicatorType1Open,
			_ => null
		};

		if (openingSound is not null) {
			AudioInstance.PlayUiSound(openingSound);
		}
	}


	private void _playClosingSound()
	{
		var closingSound = Type switch {
			EquipmentType.Type1 => AudioLibrary.ReplicatorType1Close,
			_ => null
		};

		if (closingSound is not null) {
			AudioInstance.PlayUiSound(closingSound);
		}
	}

	private bool _moveArtifactToInventory()
	{
		return _actorInventory.AddItem(_process.Artifact, 1);
	}

	private bool _moveReplicaToInventory()
	{
		string replicaPath = _process.Artifact.ReplicatesInto;
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

		if (_process.Progress >= 100 && IsActivated && _process.InProgress) {
			EmitSignal(SignalName.ReplicationComplete);
		}

		UiReplicatorInst.UpdateUi(_process);
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
		_process.IsComplete = true;
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
