using Godot;

public static partial class Tools
{
	public static bool CheckAssigned(
		dynamic godotObject,
		string message = "",
		string className = ""
	)
	{
		if (godotObject is null) {
			if (className == "") {
				className = "DG";
			}
			GD.PrintErr($"{className}: {message}");
			return false;
		}
		return true;
	}
}
