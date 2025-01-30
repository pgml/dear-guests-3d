using Godot;

public partial class UiClock : Control
{
	[Export]
	public World World { get; set; }

	[Export]
	public Label Date { get; set; }

	[Export]
	public Label Time { get; set; }

	public override void _Process(double delta)
	{
		Time.Text = World.DayTime.Formatted;
	}
}
