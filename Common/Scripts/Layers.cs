using System;

/// <summary>
/// Collision layers
/// </summary>
[Flags]
public enum Layer
{
	World = 1 << 0,
	Player = 1 << 1,
	AI = 1 << 2,
	Interactions = 1 << 3,
	Anomalies = 1 << 4,
	Props = 1 << 8,
	Items = 1 << 9
}
