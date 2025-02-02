using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

class ConsoleException : Exception
{
	public ConsoleException() {}

	public ConsoleException(string message)
		: base($"Error: {message}") {}

	public ConsoleException(string message, Exception inner)
		: base($"Error: {message}", inner) {}
}

public struct ConsoleCommandInfo
{
	public object ClassName;
	public MethodInfo MethodInfo;

	public ConsoleCommandInfo(object className, MethodInfo methodInfo)
	{
		ClassName = className;
		MethodInfo = methodInfo;
	}
}

public partial class Console : Resource
{
	public Inventory ActorInventory { get {
		return !Engine.IsEditorHint()
			? GD.Load<Inventory>(Resources.ActorInventory)
			: new();
	}}

	public Dictionary<string, ConsoleCommandInfo> Commands { get; private set; } = new();
	public bool IsOpen { get; set; } = false;

	public void ExecuteCommand(object controller, string methodName, string[] args)
	{
		if (methodName is null) {
			return;
		}

		//if (methodName == "add_item") {
		//	if (args[2] is null) {
		//		args[2] = "1";
		//	}

		//	if (args[1] is null) {
		//		throw new ConsoleException("item name missing");
		//	}

		//	if (args[0] is null) {
		//		throw new ConsoleException("item type missing");
		//	}
		//}

		var method = controller.GetType().GetMethod(methodName);
		var attribute = method?.GetCustomAttribute<ConsoleCommandAttribute>();
		var result = method?.Invoke(controller, args);
	}

	public void AddCommands(object controller)
	{
		MethodInfo[] methods = controller.GetType().GetMethods(
			BindingFlags.Instance |
			BindingFlags.Public |
			BindingFlags.NonPublic
		);

		foreach (MethodInfo method in methods) {
			var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
			if (attribute is not null) {
				Commands.Add(
					attribute.CmdName,
					new ConsoleCommandInfo(controller, method)
				);
			}
		}
	}
}
