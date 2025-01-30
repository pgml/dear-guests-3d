using Godot;
using System.Collections.Generic;

public partial class Creature : Node3D
{
	[Export]
	public Node3D Parent { get; set; }

	[Export]
	public Node3D ComponentsParent { get; set; }

	[Export]
	public Sprite3D CharacterSprite { get; set; }

	[Export]
	public Sprite3D SunShadowSprite { get; set; }

	public Vector3 Direction { get; set; }
	public Dictionary<string, Component> Components { get; private set; } = new();
	public CreatureData CreatureData { get; set; }

	public override void _Ready()
	{
		Components = _components();

		if (ComponentsParent is null) {
			GD.PrintErr("DG: Parent not set");
		}

		if (ComponentsParent is null) {
			GD.PrintErr("DG: ComponentsParent not set");
		}

		if (CharacterSprite is null) {
			GD.PrintErr("DG: Character Sprite not set");
		}
	}

	private Dictionary<string, Component> _components()
	{
		Dictionary<string, Component> components = new();
		foreach (Component component in ComponentsParent.GetChildren()) {
			components.Add(component.Name, component);
		}
		return components;
	}
}
