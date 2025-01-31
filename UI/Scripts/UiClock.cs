using Godot;

public partial class UiClock : Control
{
	[Export]
	public Label Date { get; set; }

	[Export]
	public Label Time { get; set; }

	public DateTime DateTime { get; private set; }

	public override void _Ready()
	{
		Tools.CheckAssigned(Date, "Date label is not assigned", GetType().Name);
		Tools.CheckAssigned(Time, "Time label is not assigned", GetType().Name);

		DateTime = GD.Load<DateTime>(Resources.DateTime);
	}

	public override void _Process(double delta)
	{
		_setDate();
		_setTime();
	}

	private void _setDate()
	{
		Date.Text = DateTime.CurrentDate();
	}

	private void _setTime()
	{
		if (DateTime.Minutes() % 10 == 0) {
			Time.Text = DateTime.Formatted();
		}
	}
}
