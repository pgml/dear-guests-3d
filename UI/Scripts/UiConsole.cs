using Godot;
using System;
using System.Linq;

public partial class UiConsole : UiControl
{
	[Export]
	public TextEdit CmdOutput { get; set; }

	[Export]
	public LineEdit CmdInput { get; set; }

	private Console Console { get {
		return GD.Load<Console>(Resources.Console);
	}}

	private Tween _tween;
	private int _currHistoryCommandIndex = -1;

	public override void _Ready()
	{
		base._Ready();
		CmdInput.TextSubmitted += _onTextSubmitted;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_console")) {
			_toggleConsole();
		}

		if (@event is InputEventKey e && @event.IsReleased() && IsOpen) {
			if (e.Keycode == Key.Up) {
				if (_currHistoryCommandIndex > 0) {
					_currHistoryCommandIndex--;
				}
				CmdInput.Text = Console.CommandHistory[_currHistoryCommandIndex];
				CmdInput.CaretColumn = CmdInput.Text.Length;
			}
			if (e.Keycode == Key.Down) {
				if (_currHistoryCommandIndex < Console.CommandHistory.Count) {
					_currHistoryCommandIndex++;
				}
				CmdInput.Text = Console.CommandHistory[_currHistoryCommandIndex];
				CmdInput.CaretColumn = CmdInput.Text.Length;
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
		ActorData().IsConsoleOpen = IsOpen;

		_grabFocus();

		await ToSignal(_tween, Tween.SignalName.Finished);
		_tween.IsQueuedForDeletion();
	}

	private async void _grabFocus()
	{
		await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
		CallDeferred("grab_focus");
		CmdInput.CallDeferred("grab_focus");
	}
}
