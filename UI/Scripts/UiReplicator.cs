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

	public override void _Ready()
	{
		base._Ready();

		if (!IsInstanceValid(TreeRoot)) {
			TreeRoot = ItemList.CreateItem();
		}

		PopulateList();
		ActorInventory.InventoryUpdated += PopulateList;
	}

	public override void _ExitTree()
	{
		ActorInventory.InventoryUpdated -= PopulateList;
	}

	public void Open(Vector2 position)
	{
		PopulateList();
		TreeItem firstItem = TreeRoot.GetChildren()[0];
		ItemList.GrabFocus();
		ItemList.SetSelected(firstItem, 0);
		firstItem.Select(0);
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
		bool isReplicationFinished
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
		ReplicatorStatus.Visible = isReplicating || isReplicationFinished;
		ReplicatorStatus.Text = isReplicationFinished
			? "Replication complete!".ToUpper()
			: _defaultReplicatorStatus;
		CollectButton.Visible = isReplicationFinished;
		CancelButton.Visible = hasArtifact && isReplicating && !isReplicationFinished;

		ItemList.Visible = !hasArtifact;
		ItemListHeadline.Visible = !hasArtifact;
		ItemList.Visible = !hasArtifact;
		ItemListHeadline.Visible = !hasArtifact;

		SettingsParent.Visible = hasArtifact;
		SettingsHeadline.Visible = hasArtifact;
		SettingsParent.Visible = hasArtifact;
		SettingsHeadline.Visible = hasArtifact;
	}

	public void PopulateList()
	{
		var items = ActorInventory.GetItemsOfType(ItemType.Artifact);

		if (items is null) {
			return;
		}

		// @todo try to avoid clearing it beforehand
		// instead try to update or append
		ClearList();

		foreach (var inventoryItem in items) {
			ItemResource item = inventoryItem.ItemResource;
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
}
