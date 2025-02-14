using Godot;
using System;

public partial class EquipmentResource : ItemResource
{
	[Export]
	public bool IsPowerSource { get; set; } = false;

	[Export]
	public bool NeedsPower { get; set; } = false;

	[Export]
	public float PowerConsumption { get; set; } = 0;
}
