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

	protected TreeItem TreeRoot;
	protected Dictionary<InventoryItemResource, TreeItem> ListItems = new();

	public override void _Ready()
	{
		TreeRoot = CreateItem();
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
}
