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
			if (_currentSlider is not null) {
				NodePath nextFocusPath = e.Keycode switch {
					Key.S => _currentSlider.FocusNext,
					Key.W => _currentSlider.FocusPrevious,
					_ => null
				};

				if (nextFocusPath is not null) {
					_focusNextSlider(nextFocusPath);
				}
			}
		}
	}

	public void Open(Vector2 position)
	{
		PopulateList();
		SelectFirstRow(TreeRoot);

		Position = position;
		IsOpen = true;
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

	public void UpdateUi(
		ArtifactResource artifact,
		string startTimeString,
		double progress,
		int remainingTime,
		bool isReplicating,
		bool isReplicationComplete
	)
	{
		bool hasArtifact = artifact is not null;

		ArtifactName.Text = hasArtifact
			? artifact.Name
			: _defaultArtifactName;

		RequiredConditions.Text = hasArtifact
			? artifact.RequiredConditions().ToArray().Join(",")
			: _defaultRequiredConditions;

		StartTime.Text = isReplicating && hasArtifact
			? startTimeString
			: _defaultStartTime;

		Progress.Text = isReplicating && hasArtifact
			? $"{progress}%"
			: _defaultProgress;

		EndTime.Text = hasArtifact
			? $"~ {remainingTime}h"
			: _defaultEndTime;

		InsertButton.Visible = !hasArtifact;
		ReplicateButton.Visible = hasArtifact && !isReplicating;
		ReplicatorStatus.Visible = isReplicating || isReplicationComplete;
		ReplicatorStatus.Text = isReplicationComplete
			? "Replication complete!".ToUpper()
			: _defaultReplicatorStatus;
		CollectButton.Visible = isReplicationComplete;
		CancelButton.Visible = hasArtifact && isReplicating && !isReplicationComplete;

		ItemList.Visible = !hasArtifact;
		ItemListHeadline.Visible = !hasArtifact;
		ItemList.Visible = !hasArtifact;
		ItemListHeadline.Visible = !hasArtifact;

		SettingsParent.Visible = hasArtifact;
		SettingsHeadline.Visible = hasArtifact;
		SettingsParent.Visible = hasArtifact;
		SettingsHeadline.Visible = hasArtifact;

		_replicatorHasArtifact = hasArtifact;
		_focusFirstSlider();
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
