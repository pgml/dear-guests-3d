using Godot;
//using static Godot.GD;

public partial class SceneTransition : CanvasLayer
{
	private AnimationPlayer _animationPlayer;

	private bool _isPlaying = false;

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public void FadeIn()
	{
		_animationPlayer.Play("fade");
		_isPlaying = true;
	}

	public void FadeOut()
	{
		_animationPlayer.PlayBackwards("fade");
		_isPlaying = false;
	}

	public SignalAwaiter AnimationFinished()
	{
		return ToSignal(_animationPlayer, "animation_finished");
	}
}
