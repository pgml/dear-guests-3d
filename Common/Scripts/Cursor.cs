using Godot;

public partial class Cursor : Node3D
{
	[Export]
	public Texture2D Default;

	[Export]
	public Texture2D Hand;

	[Export]
	public Texture2D Grab;

	private CreatureData _actorData;
	//private AnimatedSprite3D _throw;
	//private ContainerResource _container;

	public override void _Ready()
	{
		_actorData = GD.Load<CreatureData>(Resources.ActorData);
		//_throw = GetNode<AnimatedSprite3D>("ThrowCursor");

		CursorDefault();
		//_container = Load<ContainerResource>(Resources.Container);
	}

	public override void _Process(double delta)
	{
		//if (_container.IsOpen) {
		//	CursorDefault();
		//}
		if (_actorData.IsPickingUpItem) {
			CursorGrab();
		}
		else if (_actorData.CanPickUp) {
			CursorHand();
		}
		//else if (_actorData.CanThrowItem) {
		//	CursorThrow();
		//}
		else {
			CursorDefault();
		}
	}

	public void CursorDefault()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Input.SetCustomMouseCursor(Default);
		_hideThrowCursor();
	}

	public void CursorHand()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Input.SetCustomMouseCursor(Hand);
		_hideThrowCursor();
	}

	public void CursorGrab()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Input.SetCustomMouseCursor(Grab);
		_hideThrowCursor();
	}

	public void CursorThrow()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		//_showThrowCursor();
		//_throw.GlobalPosition = GetGlobalMousePosition();
	}

	private void _showThrowCursor()
	{
		//_throw.Visible = true;
	}

	private void _hideThrowCursor()
	{
		//_throw.Visible = false;
	}
}
