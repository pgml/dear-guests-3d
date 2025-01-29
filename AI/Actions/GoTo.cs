using Godot;

public partial class GoTo : AiAction
{
	public override bool Execute(ref Goal goal)
	{
		Task task = goal.Task;

		if (!task.CanExecute) {
			return false;
		}

		if (Agent.CurrentGoal is null) {
			Agent.TargetLocation = task.Location;
			Agent.SetTargetPosition();
		}

		Agent.CurrentGoal = goal;
		Agent.ActiveTask = task;

		if (NavigationServer3D.MapGetIterationId(Agent.GetNavigationMap()) == 0) {
			return false;
		}

		if (Agent.IsNavigationFinished()) {
			Agent.EndNavigation();
			return false;
		}

		Agent.FollowPath();

		return true;
	}
}
