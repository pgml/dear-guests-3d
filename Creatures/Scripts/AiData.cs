using Godot;
using System.Collections.Generic;

public partial class AiData : Resource
{
	public Dictionary<string, CreatureData> CreatureData { get; set; } = new();
}
