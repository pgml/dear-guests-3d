using Godot;
using System.Collections.Generic;

public partial class UiReplicator : UiControl
{
	[ExportCategory("Info Window")]
	[Export]
	public Label InfoHeadline { get; set; }

	[Export]
	public TextureButton ArtifactIcon { get; set; }

	[Export]
	public Label ReplicatorStatus { get; set; }

	[Export]
	public Label ArtifactName { get; set; }

	[Export]
	public Label DescriptionConditions { get; set; }

	[Export]
	public Label RequiredConditions { get; set; }

	[Export]
	public Label DescriptionStartTime { get; set; }

	[Export]
	public Label StartTime { get; set; }

	[Export]
	public Label DescriptionProgress { get; set; }

	[Export]
	public Label Progress { get; set; }

	[Export]
	public Label DescriptionEndTime { get; set; }

	[Export]
	public Label EndTime { get; set; }


	[ExportCategory("Settings Window")]
	[Export]
	public VBoxContainer SettingsParent { get; set; }

	[Export]
	public Label SettingsHeadline { get; set; }

	[Export]
	public VBoxContainer HumidityParent { get; set; }

	[Export]
	public VBoxContainer PressureParent { get; set; }

	[Export]
	public VBoxContainer BrightnessParent { get; set; }

	[Export]
	public VBoxContainer WindVelocityParent { get; set; }

	[Export]
	public VBoxContainer TemperatureParent { get; set; }


	[ExportCategory("Item List")]
	[Export]
	public Label ItemListHeadline { get; set; }

	[Export]
	public Tree ItemList { get; set; }


	[ExportCategory("Buttons")]
	[Export]
	public Button ReplicateButton { get; set; }

	[Export]
	public Button InsertButton { get; set; }

	[Export]
	public Button CollectButton { get; set; }

	[Export]
	public Button CancelButton { get; set; }

	[Export]
	public Button CloseButton { get; set; }


	public static AudioLibrary AudioLibrary { get {
		return GD.Load<AudioLibrary>(Resources.AudioLibrary);
	}}

	//public AudioInstance AudioInstance { get; private set; }
	public AudioInstance AudioInstance { get {
		return AudioLibrary.CreateAudioInstance("Replicator", this);
	}}

	public PackedScene PackedUiReplicator { get {
		return GD.Load<PackedScene>(Resources.UiReplicator);
	}}

	public Dictionary<
		ArtifactGrowCondition,
		SliderProperties
	> SliderValues { get; set; } = new();

	public Replicator Replicator { get; set; }

	protected Inventory ActorInventory { get {
		return !Engine.IsEditorHint()
			? GD.Load<Inventory>(Resources.ActorInventory)
			: new();
	}}

	protected TreeItem TreeRoot;
	protected Dictionary<InventoryItemResource, TreeItem> ListItems = new();

	private string _defaultReplicatorStatus = "Replicating...";
	private string _defaultArtifactName = "[INSERT ARTIFACT]";
	private string _defaultRequiredConditions = "-";
	private string _defaultStartTime = "-";
	private string _defaultProgress = "-";
	private string _defaultEndTime = "-";
	private bool _hasAnySliderFocus = false;
	private bool _replicatorHasArtifact = false;
	private Slider _currentSlider = null;

	public override void _Ready()
	{
		base._Ready();

		if (!IsInstanceValid(TreeRoot)) {
			TreeRoot = ItemList.CreateItem();
		}

		//AudioInstance = AudioLibrary.CreateAudioInstance("browse", this);
		PopulateList();
		//ActorInventory.InventoryUpdated += PopulateList;
	}

	public override void _ExitTree()
	{
		//ActorInventory.InventoryUpdated -= PopulateList;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey e && e.Pressed) {
			bool playBrowseSound = false;

			if (_currentSlider is not null) {
				NodePath nextFocusPath = null;

				if (e.IsAction("ui_focus_next")) {
					playBrowseSound = true;
					nextFocusPath = _currentSlider.FocusNext;
				}

				if (e.IsAction("ui_focus_prev")) {
					playBrowseSound = true;
					nextFocusPath = _currentSlider.FocusPrevious;
				}

				if (e.IsAction("ui_slider_increase") ||
					e.IsAction("ui_slider_decrease"))
				{
					playBrowseSound = true;
				}

				if (nextFocusPath is not null) {
					_focusNextSlider(nextFocusPath);
				}
			}

			if ((e.IsAction("ui_focus_next")
					&& ItemList.GetSelected().GetNext() is not null)
				|| (e.IsAction("ui_focus_prev")
					&& ItemList.GetSelected().GetPrev() is not null)
			) {
				playBrowseSound = true;
			}

			if (playBrowseSound) {
				AudioInstance.PlayUiSound(AudioLibrary.InventoryBrowse);
			}
		}
	}

	public void Open(Vector2 position)
	{
		PopulateList();
		SelectFirstRow(TreeRoot);

		Position = position;
		IsOpen = true;
		RestrictPlayerMovement = true;
		ActorData().IsReplicatorOpen = true;
	}

	public void Close()
	{
		QueueFree();
		ActorData().IsReplicatorOpen = false;
	}

	public HSlider Slider(VBoxContainer parent)
	{
		return parent.FindChild("Slider") as HSlider;
	}

	public void UpdateUi(ReplicationProcess process)
	{
		_replicatorHasArtifact = process.Artifact is not null;

		ArtifactName.Text = _replicatorHasArtifact
			? process.Artifact.Name
			: _defaultArtifactName;

		RequiredConditions.Text =_replicatorHasArtifact
			? process.Artifact.RequiredConditions().ToArray().Join(",")
			: _defaultRequiredConditions;

		StartTime.Text = process.InProgress && _replicatorHasArtifact
			? process.StartTimeString()
			: _defaultStartTime;

		Progress.Text = process.InProgress && _replicatorHasArtifact
			? $"{process.Progress}%"
			: _defaultProgress;

		EndTime.Text = _replicatorHasArtifact
			? $"~ {process.RemainingTime()}h"
			: _defaultEndTime;

		InsertButton.Visible = !_replicatorHasArtifact;

		ReplicateButton.Visible = _replicatorHasArtifact
			&& (!process.InProgress || process.IsPaused);
		ReplicatorStatus.Visible = process.InProgress || process.IsComplete;
		ReplicatorStatus.Text = process.IsComplete
			? "Replication complete!".ToUpper()
			: _defaultReplicatorStatus;

		CollectButton.Visible = process.IsComplete;

		CancelButton.Visible = false;
		if (_replicatorHasArtifact && process.InProgress && !process.IsComplete) {
			CancelButton.Visible = true;
		}

		GD.PrintS(process.IsComplete, process.InProgress);

		_toggleItemList();
		_toggleSettings();
		_focusFirstSlider();
	}

	private void _toggleItemList()
	{
		ItemList.Visible = !_replicatorHasArtifact;
		ItemListHeadline.Visible = !_replicatorHasArtifact;
	}

	private void _toggleSettings()
	{
		SettingsParent.Visible = _replicatorHasArtifact;
		SettingsHeadline.Visible = _replicatorHasArtifact;
	}

	public void PopulateList()
	{
		var inventory = GD.Load<Inventory>(Resources.ActorInventory);
		var items = inventory.GetItemsOfType(ItemType.Artifact);

		if (items is null) {
			return;
		}

		// @todo try to avoid clearing it beforehand
		// instead try to update or append
		ClearList();

		foreach (var inventoryItem in items) {
			ArtifactResource item = inventoryItem.ItemResource as ArtifactResource;

			if (item.IsSynthetic) {
				continue;
			}

			TreeItem row = ItemList.CreateItem(TreeRoot);
			string amount = inventoryItem.Amount > 0
				? $" ({inventoryItem.Amount.ToString()})"
				: "";

			//row.SetText(0, $"{item.Name.ToUpper()}{amount}");
			row.SetText(0, $"{item.Name.ToUpper()}");
			row.SetMetadata(0, item);
			ListItems.Add(inventoryItem, row);
		}
	}

	protected void ClearList()
	{
		if (!IsInstanceValid(TreeRoot)) {
			return;
		}

		ListItems.Clear();
		foreach (var child in TreeRoot.GetChildren()) {
			TreeRoot.RemoveChild(child);
		}
	}

	public void SelectFirstRow(TreeItem root = null)
	{
		if (root is null) {
			root = TreeRoot;
		}

		TreeItem firstItem = root.GetChildren()[0];
		ItemList.CallDeferred("grab_focus");
		ItemList.CallDeferred("set_selected", firstItem, 0);
		firstItem.CallDeferred("select", 0);
	}

	private void _focusFirstSlider()
	{
		if (_replicatorHasArtifact && !_hasAnySliderFocus) {
			SettingsParent.CallDeferred("grab_focus");
			_currentSlider = SettingsParent.FindChild("Slider", true) as Slider;
			if (!_currentSlider.HasFocus()) {
				_currentSlider.CallDeferred("grab_focus");
				_hasAnySliderFocus = true;
			}
		}
	}

	private void _focusNextSlider(NodePath currentPath)
	{
		NodePath sliderParent = currentPath.ToString().Replace("../", "");
		var path = SettingsParent
			.FindChild(sliderParent.GetName(0), true)
			.FindChild("Slider") as HSlider;
		path.CallDeferred("grab_focus");
		_currentSlider = path;
	}
}
