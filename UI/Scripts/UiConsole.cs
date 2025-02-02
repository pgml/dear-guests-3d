using Godot;
using System;
using System.Linq;

public partial class UiConsole : Control
{
	[Export]
	public TextEdit CmdOutput { get; set; }

	[Export]
	public LineEdit CmdInput { get; set; }

	private Console Console { get {
		return GD.Load<Console>(Resources.Console);
	}}

	public bool IsOpen { get; set; } = false;
	private Tween _tween;
	private int _currHistoryCommandIndex = -1;

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

		if (@event is InputEventKey e && @event.IsReleased()) {
			if (e.Keycode == Key.Up) {
				if (_currHistoryCommandIndex > 0) {
					_currHistoryCommandIndex--;
				}
				CmdInput.Text = Console.CommandHistory[_currHistoryCommandIndex];
			}
			if (e.Keycode == Key.Down) {
				if (_currHistoryCommandIndex < Console.CommandHistory.Count) {
					_currHistoryCommandIndex++;
				}
				CmdInput.Text = Console.CommandHistory[_currHistoryCommandIndex];
			}
		}
	}

	private void _onTextSubmitted(string input)
	{
		string[] cmdInput = input.Split(" ");
		string methodName = cmdInput[0];

		Console.CommandHistory.Add(input);
		_currHistoryCommandIndex++;

		if (!Console.Commands.ContainsKey(methodName)) {
			input = $"Error: command `{methodName}` not found";
		}
		else {
			var cmd = Console.Commands[methodName];

			try {
				Console.ExecuteCommand(
					cmd.ClassName,
					cmd.MethodInfo.Name,
					cmdInput.Skip(1).ToArray()
				);
			}
			catch (ConsoleException e) {
				input = e.Message;
			}
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

		Console.IsOpen = IsOpen;

		await ToSignal(_tween, Tween.SignalName.Finished);
		CallDeferred("grab_focus");
		CmdInput.CallDeferred("grab_focus");
		_tween.IsQueuedForDeletion();
	}
}
