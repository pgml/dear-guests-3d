public class Goal
{
	public string Name { get; private set; }
	public int Time { get; private set; }
	public float Priority { get; private set; }
	public Task Task { get; private set; }
	public bool IsActive { get; set; }

	public Goal(Task task)
	{
		Name = task.Action;
		Time = (int)task.Time;
		Priority = (float)task.Priority;
		Task = task;
		IsActive = false;
	}
}
