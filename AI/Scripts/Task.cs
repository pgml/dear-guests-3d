using Godot;

public class Task
{
	public string Action { get; set; }
	public double Priority { get; set; }
	public long Time { get; set; }
	public TaskLocation Location { get; set; }

	/// <summary>
	/// Checks if the task can be executed
	/// according to task date, time and/or frequency
	/// </summary>
	public bool CanExecute {
		get => _canExecute();
	}

	private bool _canExecute()
	{
		//var dateTime = GD.Load<DateTime>(Resources.DateTime);
		//int currentTime = dateTime.Time(false, true).ToInt();

		////if (IsActive) {
		////	return true;
		////}

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
		//	if (currentTime > Time) {
		//		return true;
		//	}
		//}

		//return false;

		return true;
	}
}
