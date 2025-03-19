using Godot;

public class Task
{
	public DateTime DateTime { get; private set; }

	public string Action { get; set; }
	public double Priority { get; set; }
	public int Time { get; set; }
	public TaskLocation Location { get; set; }

	/// <summary>
	/// Checks if the task can be executed
	/// according to task date, time and/or frequency
	/// </summary>
	public bool CanExecute {
		get => _canExecute();
	}

	public Task()
	{
		DateTime = GD.Load<DateTime>(Resources.DateTime);
	}

	private bool _canExecute()
	{
		//int currentTime = dateTime.Time(false, true).ToInt();

		//if (!Recurring) {
		//	if (DateTime.ToTimestamp(Date) == dateTime.Timestamp(true)) {
		//		return true;
		//	}
		//}

		//// @todo: don't return false when task started before midnight but
		//// is still executing after midnight
		//if (Duration > 0) {
		//	if (currentTime > Time && currentTime < EndTime) {
		//		return true;
		//	}
		//}
		//else {
			if (DateTime.TwentyFour() >= Time) {
				return true;
			}
		//}

		return false;
	}
}
