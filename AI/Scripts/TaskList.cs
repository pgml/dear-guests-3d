using Godot;
using System.Collections.Generic;
using Tomlyn;
using Tomlyn.Model;

public struct TaskLocation
{
	public string Name;
	public string Type;

	public TaskLocation(string name, string type)
	{
		Name = name;
		Type = type;
	}
}

public class TaskList
{
	public List<Task> Fetch(string file)
	{
		FileAccess scheduleFile = FileAccess.Open(
			file.SimplifyPath(),
			FileAccess.ModeFlags.Read
		);

		TomlTable schedule = Toml.ToModel(scheduleFile.GetAsText());
		List<Task> tasks = new();

		if (!schedule.ContainsKey("task")) {
			return tasks;
		}

		object taskTables = schedule["task"];

		foreach (TomlTable table in (TomlTableArray)taskTables) {
			var task = new Task {
				Action = table["action"].ToString(),
				Time = System.Convert.ToInt32(table["time"]),
				Location = _taskLocation(table),
				Priority = (double)table["priority"]
			};

			//if (table.ContainsKey("duration")) {
			//	task.Duration = (long)table["duration"];
			//}

			//if (table.ContainsKey("recurring")) {
			//	task.Recurring = (bool)table["recurring"];
			//}

			//if (table.ContainsKey("on_arrival")) {
			//	task.OnArrival = table["on_arrival"].ToString();
			//}

			//if (table.ContainsKey("date")) {
			//	var dateSplit = table["date"].ToString().Split("-");
			//	dateSplit[0] = (dateSplit[0].ToInt() + 1969).ToString();
			//	var date = dateSplit.Join("-");
			//	task.Date = DateTime.IsoToDateTime(date);
			//}

			tasks.Add(task);
		}

		return tasks;
	}

	private TaskLocation _taskLocation(TomlTable table)
	{
		object locationTable = table["location"];

		var locationName = "";
		var locationType = "";

		if (table["location"] is TomlTable) {
			var locTable = (TomlTable)table["location"];
			if (locTable.ContainsKey("type")) {
				locationType = locTable["type"].ToString();
			}
			if (locTable.ContainsKey("name")) {
				locationName = locTable["name"].ToString();
			}
		}
		else {
			locationName = table["location"].ToString();
		}

		return new TaskLocation(locationName, locationType);
	}
}

