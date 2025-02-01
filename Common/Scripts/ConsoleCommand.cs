using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public partial class ConsoleCommandAttribute : Attribute
{
	public string CmdName { get; }

	public ConsoleCommandAttribute(string cmdName)
	{
		CmdName = cmdName;
	}
}
