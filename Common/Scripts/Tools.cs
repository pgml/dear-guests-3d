using Godot;
using System;
using System.Reflection;

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

	#nullable enable
	public static T? GetCustomAttribute<T, TEnum>(TEnum enumValue)
		where T : Attribute
		where TEnum : Enum
	{
		FieldInfo? fieldInfo = typeof(TEnum).GetField(enumValue.ToString());
		return fieldInfo?.GetCustomAttribute<T>();
	}
	#nullable disable
}
