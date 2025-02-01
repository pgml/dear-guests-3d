using Godot;
using System;
using System.Linq;
using System.Reflection;

class ConsoleException : Exception
{
	public ConsoleException() {}

	public ConsoleException(string message)
		: base($"Error: {message}") {}

	public ConsoleException(string message, Exception inner)
		: base($"Error: {message}", inner) {}
}

public partial class Console : Control
{
	[Export]
	public TextEdit CmdOutput { get; set; }

	[Export]
	public LineEdit CmdInput { get; set; }

	public Inventory ActorInventory { get {
		return !Engine.IsEditorHint()
			? GD.Load<Inventory>(Resources.ActorInventory)
			: new();
	}}

	public bool IsOpen { get; set; } = false;

	private Tween _tween;

	public override void _Ready()
	{
		CmdInput.GrabFocus();
		CmdInput.TextSubmitted += _onTextSubmitted;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_console")) {
			_toggleConsole();
		}
	}

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

	private void _onTextSubmitted(string input)
	{
		string[] cmdInput = input.Split(" ");

		var method = cmdInput[0] switch {
			"add_item" => nameof(Inventory.AddItemByString),
			_ => null
		};

		if (method is null) {
			input = $"Error: command `{cmdInput[0]}` not found";
		}

		try {
			ExecuteCommand(ActorInventory, method, cmdInput.Skip(1).ToArray());
		}
		catch (ConsoleException e) {
			input = e.Message;
		}

		CmdOutput.Text += $"> {input}\n";
		CmdInput.Text = "";
		CmdInput.GrabFocus();
	}

	private async void _toggleConsole()
	{
		_tween = CreateTween();

		float posY = !IsOpen ? 0 : -Math.Abs(CustomMinimumSize.Y);

		_tween.TweenProperty(this, "position", new Vector2(
			Position.X,
			posY
		), 0.1);

		IsOpen = !IsOpen;
		CmdInput.Editable = IsOpen;

		if (!IsOpen) {
			CmdInput.Text = "";
		}

		await ToSignal(_tween, Tween.SignalName.Finished);
		CallDeferred("grab_focus");
		CmdInput.CallDeferred("grab_focus");
		_tween.IsQueuedForDeletion();
	}
}
