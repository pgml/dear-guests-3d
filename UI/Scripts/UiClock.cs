using Godot;
using System;

public partial class UiClock : Control
{
	[Export]
	public World World { get; set; }

	[Export]
	public Label Date { get; set; }

	[Export]
	public Label Time { get; set; }

	public override void _Ready()
	{
		Tools.CheckAssigned(World, "World node is not assigned", GetType().Name);
		Tools.CheckAssigned(Date, "Date label is not assigned", GetType().Name);
		Tools.CheckAssigned(Time, "Time label is not assigned", GetType().Name);
	}

	public override void _Process(double delta)
	{
		_setDate();
		_setTime();
	}

	private void _setDate()
	{
		DateTime startOfYear = new(World.Year, 1, 1);
		DateTime date = startOfYear.AddDays(World.DayOfYear - 1);
		Date.Text = date.ToString(World.DateFormat);
	}

	private void _setTime()
	{
		if (World.DayTime.Minute % 10 == 0) {
			Time.Text = World.DayTime.Formatted;
		}
	}
}
