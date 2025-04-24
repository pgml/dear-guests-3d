using Godot;
using System.Collections.Generic;

public partial class Belt : Resource
{
	[Signal]
	public delegate void BeltUpdatedEventHandler();

	[Export]
	public int MaxItems { get; set; } = 2;

	/// <summary>
	/// Dictionary of InventoryIndex and IsSelected
	/// </summary>
	public Dictionary<int, bool> Items { get; set; } = new();

	public int SelectedItemResourceIndex { get; set; }
	public ItemResource SelectedItemResource { get; set; }

	public int SelectedItemIndex()
	{
		foreach (var (index, isSelected) in Items) {
			if (isSelected) {
				return index;
			}
		}
		return -1;
	}
}
