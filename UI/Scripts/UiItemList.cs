using Godot;
using System.Collections.Generic;

public struct TitleProperties
{
	public HorizontalAlignment Alignment;
	public int Width;

	public TitleProperties(HorizontalAlignment alignment, int width)
	{
		Alignment = alignment;
		Width = width;
	}
}

public partial class UiItemList : Tree
{
	protected Inventory ActorInventory { get {
		return !Engine.IsEditorHint()
			? GD.Load<Inventory>(Resources.ActorInventory)
			: new();
	}}

	public TreeItem TreeRoot;
	public Dictionary<InventoryItemResource, TreeItem> ListItems = new();

	public static AudioLibrary AudioLibrary { get {
		return GD.Load<AudioLibrary>(Resources.AudioLibrary);
	}}

	public AudioInstance AudioInstance { get; set; } = null;

	public override void _Ready()
	{
		TreeRoot = CreateItem();
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsInstanceValid(AudioInstance)) {
			return;
		}

		if (@event is InputEventKey e && e.Pressed) {
			if (GetSelected() is TreeItem selectedItem) {
				if (e.IsAction("ui_focus_next") && selectedItem.GetNext() is not null) {
					AudioInstance.PlayUiSound(AudioLibrary.InventoryBrowse);
				}

				if (e.IsAction("ui_focus_prev") && selectedItem.GetPrev() is not null) {
					AudioInstance.PlayUiSound(AudioLibrary.InventoryBrowse);
				}
			}
		}
	}

	protected void ClearList()
	{
		if (TreeRoot is null) {
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
		CallDeferred("grab_focus");
		CallDeferred("set_selected", firstItem, 0);
		firstItem.CallDeferred("select", 0);
	}
}
