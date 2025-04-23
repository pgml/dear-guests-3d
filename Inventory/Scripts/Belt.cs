using Godot;
using System.Collections.Generic;

public partial class Belt : Resource
{
	[Signal]
	public delegate void BeltUpdatedEventHandler();

	[Export]
	public int MaxItems { get; set; } = 2;

	public List<int> Items { get; set; } = new();
}
