using Godot;

public partial class UiLoading : Control
{
	[Export]
	public Label Label { get; set; }

	[Export]
	public AnimationPlayer AnimationPlayer { get; set; }

	private bool _isPlaying = false;

	public override void _Ready()
	{
		AnimationPlayer = GetNode<AnimationPlayer>("Background/AnimationPlayer");
	}

	public void Show(string message = "")
	{
		AnimationPlayer.Play("show");
		_isPlaying = true;
		UpdateLabel(message);
	}

	public void FadeIn(string message = "")
	{
		AnimationPlayer.Play("fade");
		_isPlaying = true;
		UpdateLabel(message);
	}

	public void FadeOut()
	{
		AnimationPlayer.PlayBackwards("fade");
		_isPlaying = false;
		UpdateLabel("");
	}

	public void UpdateLabel(string message)
	{
		Label.Text = message;
	}

	public SignalAwaiter AnimationFinished()
	{
		return ToSignal(AnimationPlayer, "animation_finished");
	}
}

